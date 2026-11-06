using TeaPremium.Application.Media;

namespace TeaPremium.Infrastructure.Media;

internal sealed class UnavailableObjectStorageService : IObjectStorageService
{
    public bool IsConfigured => false;

    public Task<PresignedObjectUpload> CreatePresignedUploadAsync(
        string objectKey,
        string contentType,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken) =>
        Task.FromException<PresignedObjectUpload>(NotConfigured());

    public Task<StoredObjectMetadata?> GetMetadataAsync(
        string objectKey,
        CancellationToken cancellationToken) =>
        Task.FromException<StoredObjectMetadata?>(NotConfigured());

    public Task<byte[]> ReadPrefixAsync(
        string objectKey,
        int maximumBytes,
        CancellationToken cancellationToken) =>
        Task.FromException<byte[]>(NotConfigured());

    public Task DeleteIfExistsAsync(
        string objectKey,
        CancellationToken cancellationToken) =>
        Task.FromException(NotConfigured());

    public string? GetPublicUrl(string objectKey) => DemoMediaPath.Resolve(objectKey);

    private static MediaStorageUnavailableException NotConfigured() =>
        new("Object storage is not configured for this environment.");
}
