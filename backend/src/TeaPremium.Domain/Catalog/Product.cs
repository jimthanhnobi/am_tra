using TeaPremium.Domain.Common;
using TeaPremium.Domain.Media;

namespace TeaPremium.Domain.Catalog;

public sealed class Product
{
    private Product()
    {
    }

    public Product(
        string slug,
        string name,
        string summary,
        string descriptionJson,
        string descriptionHtml,
        string productType,
        string specsJson,
        SeoMetadata seo,
        string? sku = null,
        Guid? primaryCategoryId = null,
        Guid? brandId = null,
        string? materialText = null,
        int? capacityMl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(descriptionHtml);
        ArgumentException.ThrowIfNullOrWhiteSpace(productType);
        ArgumentNullException.ThrowIfNull(seo);

        if (capacityMl is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacityMl),
                capacityMl,
                "Capacity must be greater than zero when specified.");
        }

        Id = Guid.NewGuid();
        Slug = slug;
        Name = name;
        Summary = summary;
        DescriptionJson = JsonObjectValidator.EnsureObject(descriptionJson, nameof(descriptionJson));
        DescriptionHtml = descriptionHtml;
        ProductType = productType;
        SpecsJson = JsonObjectValidator.EnsureObject(specsJson, nameof(specsJson));
        Seo = seo;
        Sku = sku;
        PrimaryCategoryId = primaryCategoryId;
        BrandId = brandId;
        MaterialText = materialText;
        CapacityMl = capacityMl;
        Status = PublicationStatus.Draft;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string? Sku { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Summary { get; private set; } = string.Empty;

    public string DescriptionJson { get; private set; } = "{}";

    public string DescriptionHtml { get; private set; } = string.Empty;

    public PublicationStatus Status { get; private set; } = PublicationStatus.Draft;

    public Guid? PrimaryCategoryId { get; private set; }

    public Guid? BrandId { get; private set; }

    public string ProductType { get; private set; } = string.Empty;

    public string? MaterialText { get; private set; }

    public int? CapacityMl { get; private set; }

    public string SpecsJson { get; private set; } = "{}";

    public DateTimeOffset? PublishedAt { get; private set; }

    public SeoMetadata Seo { get; private set; } = new();

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Category? PrimaryCategory { get; private set; }

    public Brand? Brand { get; private set; }

    public ICollection<ProductCategory> ProductCategories { get; } = new List<ProductCategory>();

    public ICollection<ProductMedia> Media { get; } = new List<ProductMedia>();
}
