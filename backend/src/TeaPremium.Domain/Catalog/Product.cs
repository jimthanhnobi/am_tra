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

    public bool IsSellable { get; private set; } = true;

    public void SetSellable(bool value)
    {
        IsSellable = value;
        Touch();
    }

    public void ReturnToDraft()
    {
        if (Status is not (PublicationStatus.InReview or PublicationStatus.Archived))
            throw new InvalidOperationException("Only InReview or Archived products can return to Draft.");
        Status = PublicationStatus.Draft;
        Touch();
    }

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

    public void Update(
        string slug,
        string name,
        string summary,
        string descriptionJson,
        string descriptionHtml,
        string productType,
        string specsJson,
        SeoMetadata seo,
        IReadOnlyCollection<Guid> categoryIds,
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
        ArgumentNullException.ThrowIfNull(categoryIds);

        if (capacityMl is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacityMl),
                capacityMl,
                "Capacity must be greater than zero when specified.");
        }

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

        var requestedCategoryIds = categoryIds.ToHashSet();
        foreach (var link in ProductCategories
                     .Where(link => !requestedCategoryIds.Contains(link.CategoryId))
                     .ToArray())
        {
            ProductCategories.Remove(link);
        }

        var existingCategoryIds = ProductCategories
            .Select(link => link.CategoryId)
            .ToHashSet();
        foreach (var categoryId in requestedCategoryIds.Except(existingCategoryIds))
        {
            ProductCategories.Add(new ProductCategory(Id, categoryId));
        }

        Touch();
    }

    public void MoveToInReview()
    {
        if (Status != PublicationStatus.Draft)
        {
            throw new InvalidOperationException("Only Draft products can move to InReview.");
        }

        Status = PublicationStatus.InReview;
        Touch();
    }

    public void Publish(DateTimeOffset publishedAtUtc)
    {
        if (Status != PublicationStatus.InReview)
        {
            throw new InvalidOperationException("Only InReview products can be published.");
        }

        Status = PublicationStatus.Published;
        PublishedAt = publishedAtUtc.ToUniversalTime();
        Touch(publishedAtUtc);
    }

    public void Archive(DateTimeOffset updatedAtUtc)
    {
        if (Status != PublicationStatus.Published)
        {
            throw new InvalidOperationException("Only Published products can be archived.");
        }

        Status = PublicationStatus.Archived;
        Touch(updatedAtUtc);
    }

    public void Touch(DateTimeOffset? updatedAtUtc = null)
    {
        UpdatedAtUtc = (updatedAtUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
    }
}
