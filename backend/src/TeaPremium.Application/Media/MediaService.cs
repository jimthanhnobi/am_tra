using TeaPremium.Application.Catalog;
using TeaPremium.Domain.Media;

namespace TeaPremium.Application.Media;

internal sealed class MediaService(
    IMediaDataStore store,
    IObjectStorageService storage,
    MediaUploadSettings settings,
    TimeProvider timeProvider) : IMediaService
{
    private static readonly Dictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
        };

    public Task<PagedResult<AdminMediaAssetDto>> GetMediaAsync(
        AdminMediaListQuery query,
        CancellationToken cancellationToken = default)
    {
        var validation = new CatalogValidationBag();
        if (query.Page < 1)
        {
            validation.Add("page", "Page must be greater than or equal to 1.");
        }

        if (query.PageSize is < 1 or > CatalogRules.MaximumPageSize)
        {
            validation.Add(
                "pageSize",
                $"Page size must be between 1 and {CatalogRules.MaximumPageSize}.");
        }

        if (query.ContentType is not null && !AllowedContentTypes.ContainsKey(query.ContentType))
        {
            validation.Add("contentType", "Content type must be image/jpeg, image/png, or image/webp.");
        }

        validation.ThrowIfInvalid();
        return store.QueryMediaAsync(query, cancellationToken);
    }

    public async Task<MediaUploadIntentDto> CreateUploadIntentAsync(
        Guid userId,
        CreateMediaUploadIntentCommand command,
        CancellationToken cancellationToken = default)
    {
        EnsureStorageConfigured();
        ArgumentNullException.ThrowIfNull(command);

        var contentType = NormalizeContentType(command.ContentType);
        var validation = new CatalogValidationBag();
        if (userId == Guid.Empty)
        {
            validation.Add("user", "An authenticated user is required.");
        }

        if (string.IsNullOrWhiteSpace(command.FileName))
        {
            validation.Add("fileName", "File name is required.");
        }

        if (!AllowedContentTypes.TryGetValue(contentType, out var extension))
        {
            validation.Add("contentType", "Only JPEG, PNG, and WebP images are allowed.");
        }

        if (command.FileSize <= 0 || command.FileSize > settings.MaximumFileSizeBytes)
        {
            validation.Add(
                "fileSize",
                $"File size must be between 1 byte and {settings.MaximumFileSizeBytes} bytes.");
        }

        validation.ThrowIfInvalid();

        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(settings.UploadUrlLifetime);
        var objectKey = $"catalog/{now:yyyy/MM}/{Guid.NewGuid():N}{extension}";
        var displayName = NormalizeDisplayFileName(command.FileName, extension!);
        var session = new MediaUploadSession(
            objectKey,
            displayName,
            contentType,
            command.FileSize,
            userId,
            expiresAt,
            now);
        var upload = await storage.CreatePresignedUploadAsync(
            objectKey,
            contentType,
            expiresAt,
            cancellationToken);

        store.AddUpload(session);
        await store.SaveChangesAsync(cancellationToken);

        return new MediaUploadIntentDto(
            session.Id,
            upload.Url,
            objectKey,
            expiresAt,
            upload.RequiredHeaders);
    }

    public async Task<AdminMediaAssetDto> CompleteUploadAsync(
        Guid userId,
        Guid uploadId,
        CancellationToken cancellationToken = default)
    {
        EnsureStorageConfigured();
        var upload = await store.LoadUploadAsync(uploadId, cancellationToken) ??
            throw new CatalogNotFoundException("Media upload", uploadId);

        if (upload.CreatedByUserId != userId)
        {
            throw new CatalogNotFoundException("Media upload", uploadId);
        }

        if (upload.Status != MediaUploadStatus.Pending)
        {
            throw new CatalogConflictException("This media upload has already been completed or expired.");
        }

        var now = timeProvider.GetUtcNow();
        if (upload.IsExpired(now))
        {
            await storage.DeleteIfExistsAsync(upload.ObjectKey, cancellationToken);
            upload.Expire();
            await store.SaveChangesAsync(cancellationToken);
            throw Validation("uploadId", "The upload intent has expired. Request a new upload URL.");
        }

        StoredObjectMetadata? metadata;
        byte[] prefix;
        try
        {
            metadata = await storage.GetMetadataAsync(upload.ObjectKey, cancellationToken);
            if (metadata is null)
            {
                throw Validation("uploadId", "The uploaded object was not found in storage.");
            }

            prefix = await storage.ReadPrefixAsync(
                upload.ObjectKey,
                settings.InspectionPrefixBytes,
                cancellationToken);
        }
        catch (ObjectStorageException)
        {
            throw;
        }

        var errors = new CatalogValidationBag();
        if (metadata.ByteSize != upload.ExpectedByteSize)
        {
            errors.Add("fileSize", "The uploaded object size does not match the upload intent.");
        }

        var storedContentType = NormalizeContentType(metadata.ContentType);
        if (!string.Equals(storedContentType, upload.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("contentType", "The uploaded object content type does not match the upload intent.");
        }

        if (!ImageMetadataInspector.TryInspect(prefix, out var inspected) || inspected is null)
        {
            errors.Add("file", "The uploaded object is not a supported JPEG, PNG, or WebP image.");
        }
        else if (!string.Equals(inspected.MimeType, upload.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("file", "The image signature does not match the declared content type.");
        }

        errors.ThrowIfInvalid();

        var checksum = NormalizeEntityTag(metadata.EntityTag);
        var asset = new MediaAsset(
            upload.ObjectKey,
            upload.ContentType,
            metadata.ByteSize,
            inspected!.Width,
            inspected.Height,
            checksum);

        await store.ExecuteInTransactionAsync(
            async token =>
            {
                store.AddAsset(asset);
                upload.Complete(asset.Id, now);
                await store.SaveChangesAsync(token);
            },
            cancellationToken);

        return await store.QueryMediaAssetAsync(asset.Id, cancellationToken) ??
            throw new CatalogNotFoundException("Media asset", asset.Id);
    }

    public async Task<MediaCleanupResult> CleanupExpiredUploadsAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureStorageConfigured();
        var uploads = await store.LoadExpiredPendingUploadsAsync(
            timeProvider.GetUtcNow(),
            maximumCount: 100,
            cancellationToken);
        var deleted = 0;

        foreach (var upload in uploads)
        {
            await storage.DeleteIfExistsAsync(upload.ObjectKey, cancellationToken);
            upload.Expire();
            deleted++;
        }

        if (uploads.Count > 0)
        {
            await store.SaveChangesAsync(cancellationToken);
        }

        return new MediaCleanupResult(deleted, uploads.Count);
    }

    private void EnsureStorageConfigured()
    {
        if (!storage.IsConfigured)
        {
            throw new MediaStorageUnavailableException(
                "Object storage is not configured for this environment.");
        }
    }

    private static string NormalizeContentType(string? value) =>
        value?.Split(';', 2)[0].Trim().ToLowerInvariant() ?? string.Empty;

    private static string NormalizeDisplayFileName(string value, string fallbackExtension)
    {
        var name = Path.GetFileName(value.Replace('\\', '/'));
        var safe = new string(name.Where(character => !char.IsControl(character)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(safe))
        {
            safe = $"image{fallbackExtension}";
        }

        return safe.Length <= 255 ? safe : safe[..255];
    }

    private static string? NormalizeEntityTag(string? value)
    {
        var checksum = value?.Trim().Trim('"');
        return string.IsNullOrWhiteSpace(checksum)
            ? null
            : checksum.Length <= 128 ? checksum : checksum[..128];
    }

    private static CatalogValidationException Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
