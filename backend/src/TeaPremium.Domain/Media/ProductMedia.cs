using TeaPremium.Domain.Catalog;

namespace TeaPremium.Domain.Media;

public sealed class ProductMedia
{
    private ProductMedia()
    {
    }

    public ProductMedia(
        Guid productId,
        Guid assetId,
        string altText,
        int sortOrder,
        bool isCover)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID is required.", nameof(productId));
        }

        if (assetId == Guid.Empty)
        {
            throw new ArgumentException("Asset ID is required.", nameof(assetId));
        }

        ArgumentNullException.ThrowIfNull(altText);

        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                sortOrder,
                "Sort order cannot be negative.");
        }

        Id = Guid.NewGuid();
        ProductId = productId;
        AssetId = assetId;
        AltText = altText;
        SortOrder = sortOrder;
        IsCover = isCover;
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public Guid AssetId { get; private set; }

    public string AltText { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public bool IsCover { get; private set; }

    public Product Product { get; private set; } = null!;

    public MediaAsset Asset { get; private set; } = null!;
}
