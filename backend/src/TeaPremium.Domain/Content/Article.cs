using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Common;

namespace TeaPremium.Domain.Content;

public sealed class Article
{
    private Article() { }

    public Article(string slug, string title, string excerpt, string contentJson, string contentHtml,
        Guid authorId, Guid? coverAssetId, SeoMetadata seo)
    {
        Id = Guid.NewGuid();
        Slug = slug;
        Title = title;
        Excerpt = excerpt;
        ContentJson = contentJson;
        ContentHtml = contentHtml;
        AuthorId = authorId;
        CoverAssetId = coverAssetId;
        Seo = seo;
        Status = PublicationStatus.Draft;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Excerpt { get; private set; } = string.Empty;
    public string ContentJson { get; private set; } = "{}";
    public string ContentHtml { get; private set; } = string.Empty;
    public PublicationStatus Status { get; private set; }
    public Guid AuthorId { get; private set; }
    public Guid? CoverAssetId { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public SeoMetadata Seo { get; private set; } = new(null, null);
    public ICollection<ArticleCategoryLink> Categories { get; } = new List<ArticleCategoryLink>();

    public void Update(string slug, string title, string excerpt, string contentJson, string contentHtml,
        Guid? coverAssetId, SeoMetadata seo, IReadOnlyCollection<Guid> categoryIds)
    {
        Slug = slug;
        Title = title;
        Excerpt = excerpt;
        ContentJson = contentJson;
        ContentHtml = contentHtml;
        CoverAssetId = coverAssetId;
        Seo = seo;
        var requested = categoryIds.ToHashSet();
        foreach (var link in Categories.Where(link => !requested.Contains(link.CategoryId)).ToArray())
            Categories.Remove(link);
        foreach (var id in requested.Except(Categories.Select(link => link.CategoryId)))
            Categories.Add(new ArticleCategoryLink(Id, id));
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SubmitForReview()
    {
        if (Status != PublicationStatus.Draft)
            throw new InvalidOperationException("Only Draft articles can be submitted.");
        Status = PublicationStatus.InReview;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Publish(DateTimeOffset now)
    {
        if (Status != PublicationStatus.InReview)
            throw new InvalidOperationException("Only InReview articles can be published.");
        Status = PublicationStatus.Published;
        PublishedAt = UpdatedAtUtc = now.ToUniversalTime();
    }

    public void Archive(DateTimeOffset now)
    {
        if (Status != PublicationStatus.Published)
            throw new InvalidOperationException("Only Published articles can be archived.");
        Status = PublicationStatus.Archived;
        UpdatedAtUtc = now.ToUniversalTime();
    }
}
