namespace TeaPremium.Domain.Media;

public enum MediaUploadStatus
{
    Pending,
    Completed,
    Expired,
}

public sealed class MediaUploadSession
{
    private MediaUploadSession()
    {
    }

    public MediaUploadSession(
        string objectKey,
        string originalFileName,
        string contentType,
        long expectedByteSize,
        Guid createdByUserId,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedByteSize);

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("A user ID is required.", nameof(createdByUserId));
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException("Expiry must be after creation.", nameof(expiresAtUtc));
        }

        Id = Guid.NewGuid();
        ObjectKey = objectKey;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        ExpectedByteSize = expectedByteSize;
        CreatedByUserId = createdByUserId;
        ExpiresAtUtc = expiresAtUtc.ToUniversalTime();
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        Status = MediaUploadStatus.Pending;
    }

    public Guid Id { get; private set; }

    public string ObjectKey { get; private set; } = string.Empty;

    public string OriginalFileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long ExpectedByteSize { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public MediaUploadStatus Status { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public Guid? AssetId { get; private set; }

    public MediaAsset? Asset { get; private set; }

    public bool IsExpired(DateTimeOffset nowUtc) => nowUtc.ToUniversalTime() >= ExpiresAtUtc;

    public void Complete(Guid assetId, DateTimeOffset completedAtUtc)
    {
        if (Status != MediaUploadStatus.Pending)
        {
            throw new InvalidOperationException("Only a Pending upload can be completed.");
        }

        if (assetId == Guid.Empty)
        {
            throw new ArgumentException("An asset ID is required.", nameof(assetId));
        }

        Status = MediaUploadStatus.Completed;
        AssetId = assetId;
        CompletedAtUtc = completedAtUtc.ToUniversalTime();
    }

    public void Expire()
    {
        if (Status == MediaUploadStatus.Pending)
        {
            Status = MediaUploadStatus.Expired;
        }
    }
}
