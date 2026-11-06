using TeaPremium.Domain.Catalog;

namespace TeaPremium.Domain.Media;

public sealed class MediaAsset
{
    private MediaAsset()
    {
    }

    public MediaAsset(
        string storageKey,
        string mimeType,
        long byteSize,
        int width,
        int height,
        string? checksum = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        if (byteSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(byteSize),
                byteSize,
                "Byte size must be greater than zero.");
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                width,
                "Width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height),
                height,
                "Height must be greater than zero.");
        }

        Id = Guid.NewGuid();
        StorageKey = storageKey;
        MimeType = mimeType;
        ByteSize = byteSize;
        Width = width;
        Height = height;
        Checksum = checksum;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string StorageKey { get; private set; } = string.Empty;

    public string MimeType { get; private set; } = string.Empty;

    public long ByteSize { get; private set; }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public string? Checksum { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ICollection<ProductMedia> ProductMedia { get; } = new List<ProductMedia>();
}
