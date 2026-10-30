using TeaPremium.Domain.Common;

namespace TeaPremium.Domain.Catalog;

public sealed class Category
{
    private Category()
    {
    }

    public Category(
        string slug,
        string name,
        string descriptionHtml,
        SeoMetadata seo,
        Guid? parentId = null,
        int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(descriptionHtml);
        ArgumentNullException.ThrowIfNull(seo);

        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                sortOrder,
                "Sort order cannot be negative.");
        }

        Id = Guid.NewGuid();
        ParentId = parentId;
        Slug = slug;
        Name = name;
        DescriptionHtml = descriptionHtml;
        SortOrder = sortOrder;
        Status = PublicationStatus.Draft;
        Seo = seo;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid? ParentId { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string DescriptionHtml { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public PublicationStatus Status { get; private set; } = PublicationStatus.Draft;

    public SeoMetadata Seo { get; private set; } = new();

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Category? Parent { get; private set; }

    public ICollection<Category> Children { get; } = new List<Category>();

    public ICollection<Product> PrimaryProducts { get; } = new List<Product>();

    public ICollection<ProductCategory> ProductCategories { get; } = new List<ProductCategory>();

    public void Update(
        string slug,
        string name,
        string descriptionHtml,
        SeoMetadata seo,
        Guid? parentId,
        int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(descriptionHtml);
        ArgumentNullException.ThrowIfNull(seo);

        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                sortOrder,
                "Sort order cannot be negative.");
        }

        ParentId = parentId;
        Slug = slug;
        Name = name;
        DescriptionHtml = descriptionHtml;
        SortOrder = sortOrder;
        Seo = seo;
        Touch();
    }

    public void MoveToInReview()
    {
        if (Status != PublicationStatus.Draft)
        {
            throw new InvalidOperationException("Only Draft categories can move to InReview.");
        }

        Status = PublicationStatus.InReview;
        Touch();
    }

    public void Publish()
    {
        if (Status != PublicationStatus.InReview)
        {
            throw new InvalidOperationException("Only InReview categories can be published.");
        }

        Status = PublicationStatus.Published;
        Touch();
    }

    public void Archive()
    {
        if (Status != PublicationStatus.Published)
        {
            throw new InvalidOperationException("Only Published categories can be archived.");
        }

        Status = PublicationStatus.Archived;
        Touch();
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
