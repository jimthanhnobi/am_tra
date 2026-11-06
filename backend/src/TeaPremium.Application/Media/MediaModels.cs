using TeaPremium.Application.Catalog;

namespace TeaPremium.Application.Media;

public sealed record MediaUploadSettings(
    long MaximumFileSizeBytes,
    TimeSpan UploadUrlLifetime,
    int InspectionPrefixBytes)
{
    public static MediaUploadSettings Default { get; } = new(
        10 * 1024 * 1024,
        TimeSpan.FromMinutes(10),
        256 * 1024);
}

public sealed record CreateMediaUploadIntentCommand(
    string FileName,
    string ContentType,
    long FileSize);

public sealed record MediaUploadIntentDto(
    Guid UploadId,
    string PresignedUrl,
    string ObjectKey,
    DateTimeOffset ExpiresAt,
    IReadOnlyDictionary<string, string> RequiredHeaders);

public sealed record AdminMediaAssetDto(
    Guid Id,
    string FileName,
    string StorageKey,
    string MimeType,
    long ByteSize,
    int Width,
    int Height,
    string? Checksum,
    string? PublicUrl,
    DateTimeOffset CreatedAtUtc);

public sealed record AdminMediaListQuery(
    int Page = 1,
    int PageSize = 24,
    string? Search = null,
    string? ContentType = null);

public sealed record MediaCleanupResult(int DeletedObjects, int ExpiredSessions);

public sealed record PresignedObjectUpload(
    string Url,
    IReadOnlyDictionary<string, string> RequiredHeaders);

public sealed record StoredObjectMetadata(
    long ByteSize,
    string? ContentType,
    string? EntityTag);

public sealed class MediaStorageUnavailableException(string message) : Exception(message);

public sealed class ObjectStorageException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public interface IMediaService
{
    Task<PagedResult<AdminMediaAssetDto>> GetMediaAsync(
        AdminMediaListQuery query,
        CancellationToken cancellationToken = default);

    Task<MediaUploadIntentDto> CreateUploadIntentAsync(
        Guid userId,
        CreateMediaUploadIntentCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminMediaAssetDto> CompleteUploadAsync(
        Guid userId,
        Guid uploadId,
        CancellationToken cancellationToken = default);

    Task<MediaCleanupResult> CleanupExpiredUploadsAsync(
        CancellationToken cancellationToken = default);
}

public interface IObjectStorageService
{
    bool IsConfigured { get; }

    Task<PresignedObjectUpload> CreatePresignedUploadAsync(
        string objectKey,
        string contentType,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken);

    Task<StoredObjectMetadata?> GetMetadataAsync(
        string objectKey,
        CancellationToken cancellationToken);

    Task<byte[]> ReadPrefixAsync(
        string objectKey,
        int maximumBytes,
        CancellationToken cancellationToken);

    Task DeleteIfExistsAsync(string objectKey, CancellationToken cancellationToken);

    string? GetPublicUrl(string objectKey);
}
