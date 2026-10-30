using Microsoft.EntityFrameworkCore;
using Npgsql;
using TeaPremium.Application.Catalog;
using TeaPremium.Application.Media;
using TeaPremium.Domain.Media;
using TeaPremium.Infrastructure.Persistence;

namespace TeaPremium.Infrastructure.Media;

internal sealed class MediaDataStore(
    AppDbContext dbContext,
    IObjectStorageService storage) : IMediaDataStore
{
    public async Task<PagedResult<AdminMediaAssetDto>> QueryMediaAsync(
        AdminMediaListQuery query,
        CancellationToken cancellationToken)
    {
        var assets = dbContext.MediaAssets.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            assets = assets.Where(asset =>
                EF.Functions.ILike(asset.StorageKey, pattern) ||
                dbContext.MediaUploadSessions.Any(upload =>
                    upload.AssetId == asset.Id &&
                    EF.Functions.ILike(upload.OriginalFileName, pattern)));
        }

        if (!string.IsNullOrWhiteSpace(query.ContentType))
        {
            assets = assets.Where(asset => asset.MimeType == query.ContentType);
        }

        var totalItems = await assets.LongCountAsync(cancellationToken);
        var rows = await assets
            .OrderByDescending(asset => asset.CreatedAtUtc)
            .ThenBy(asset => asset.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(asset => new MediaAssetRow(
                asset.Id,
                dbContext.MediaUploadSessions
                    .Where(upload => upload.AssetId == asset.Id)
                    .Select(upload => upload.OriginalFileName)
                    .SingleOrDefault(),
                asset.StorageKey,
                asset.MimeType,
                asset.ByteSize,
                asset.Width,
                asset.Height,
                asset.Checksum,
                asset.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Page(rows, query.Page, query.PageSize, totalItems);
    }

    public async Task<AdminMediaAssetDto?> QueryMediaAssetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.MediaAssets
            .AsNoTracking()
            .Where(asset => asset.Id == id)
            .Select(asset => new MediaAssetRow(
                asset.Id,
                dbContext.MediaUploadSessions
                    .Where(upload => upload.AssetId == asset.Id)
                    .Select(upload => upload.OriginalFileName)
                    .SingleOrDefault(),
                asset.StorageKey,
                asset.MimeType,
                asset.ByteSize,
                asset.Width,
                asset.Height,
                asset.Checksum,
                asset.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return row is null ? null : Map(row);
    }

    public Task<MediaUploadSession?> LoadUploadAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        dbContext.MediaUploadSessions.SingleOrDefaultAsync(
            upload => upload.Id == id,
            cancellationToken);

    public async Task<IReadOnlyList<MediaUploadSession>> LoadExpiredPendingUploadsAsync(
        DateTimeOffset nowUtc,
        int maximumCount,
        CancellationToken cancellationToken) =>
        await dbContext.MediaUploadSessions
            .Where(upload =>
                upload.Status == MediaUploadStatus.Pending &&
                upload.ExpiresAtUtc <= nowUtc)
            .OrderBy(upload => upload.ExpiresAtUtc)
            .Take(maximumCount)
            .ToListAsync(cancellationToken);

    public void AddUpload(MediaUploadSession upload) =>
        dbContext.MediaUploadSessions.Add(upload);

    public void AddAsset(MediaAsset asset) => dbContext.MediaAssets.Add(asset);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.GetBaseException() is PostgresException postgresException)
        {
            throw postgresException.SqlState == PostgresErrorCodes.UniqueViolation
                ? new CatalogConflictException("This upload or media object has already been completed.")
                : new CatalogConflictException("The media change conflicted with a database constraint.");
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    private PagedResult<AdminMediaAssetDto> Page(
        IReadOnlyList<MediaAssetRow> rows,
        int page,
        int pageSize,
        long totalItems) =>
        new(
            rows.Select(Map).ToArray(),
            page,
            pageSize,
            totalItems,
            totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize));

    private AdminMediaAssetDto Map(MediaAssetRow row) =>
        new(
            row.Id,
            row.FileName ?? Path.GetFileName(row.StorageKey),
            row.StorageKey,
            row.MimeType,
            row.ByteSize,
            row.Width,
            row.Height,
            row.Checksum,
            storage.GetPublicUrl(row.StorageKey),
            row.CreatedAtUtc);

    private sealed record MediaAssetRow(
        Guid Id,
        string? FileName,
        string StorageKey,
        string MimeType,
        long ByteSize,
        int Width,
        int Height,
        string? Checksum,
        DateTimeOffset CreatedAtUtc);
}
