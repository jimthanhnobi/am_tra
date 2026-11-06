using TeaPremium.Application.Catalog;
using TeaPremium.Domain.Media;

namespace TeaPremium.Application.Media;

public interface IMediaDataStore
{
    Task<PagedResult<AdminMediaAssetDto>> QueryMediaAsync(
        AdminMediaListQuery query,
        CancellationToken cancellationToken);

    Task<AdminMediaAssetDto?> QueryMediaAssetAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<MediaUploadSession?> LoadUploadAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MediaUploadSession>> LoadExpiredPendingUploadsAsync(
        DateTimeOffset nowUtc,
        int maximumCount,
        CancellationToken cancellationToken);

    void AddUpload(MediaUploadSession upload);

    void AddAsset(MediaAsset asset);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);
}
