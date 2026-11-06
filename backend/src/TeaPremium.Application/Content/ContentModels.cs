using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Common;
using TeaPremium.Domain.Content;

namespace TeaPremium.Application.Content;

public sealed record ArticleSeoInput(string? SeoTitle, string? MetaDescription,
    string? CanonicalOverride, bool RobotsIndex, string? OgTitle, string? OgDescription, Guid? OgImageAssetId)
{
    public SeoMetadata ToMetadata() => new(SeoTitle, MetaDescription, CanonicalOverride,
        RobotsIndex, OgTitle, OgDescription, OgImageAssetId);
}

public sealed record ArticleInput(string Slug, string Title, string Excerpt, string ContentJson,
    Guid? CoverAssetId, IReadOnlyList<Guid> CategoryIds, ArticleSeoInput Seo);
public sealed record ArticleCategoryInput(string Slug, string Name);
public sealed record ArticleCategoryDto(Guid Id, string Slug, string Name);
public sealed record ArticlePage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems, int TotalPages);
public sealed record ArticleSummary(Guid Id, string Slug, string Title, string Excerpt,
    string Status, DateTimeOffset? PublishedAt, DateTimeOffset UpdatedAtUtc,
    string? CoverUrl, int? CoverWidth, int? CoverHeight);
public sealed record ArticleDetail(Guid Id, string Slug, string Title, string Excerpt,
    string ContentHtml, string? ContentJson, string Status,
    DateTimeOffset? PublishedAt, DateTimeOffset UpdatedAtUtc,
    ArticleSeoInput Seo, IReadOnlyList<ArticleCategoryDto> Categories,
    string? CoverUrl, int? CoverWidth, int? CoverHeight, Guid? CoverAssetId,
    string? OgImageUrl, int? OgImageWidth, int? OgImageHeight);

public interface IArticleStore
{
    Task<Article?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<Article?> FindBySlugAsync(string slug, bool publicOnly, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, Guid? exceptId, CancellationToken cancellationToken);
    Task<bool> AssetsAndCategoriesExistAsync(Guid? coverId, Guid? ogImageId, IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken);
    Task SaveAsync(Article article, CancellationToken cancellationToken);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
    Task<ArticleDetail> DetailAsync(Article article, bool includeDraft, CancellationToken cancellationToken);
    Task<ArticlePage<ArticleSummary>> ListAsync(bool publicOnly, int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<ArticleCategoryDto>> CategoriesAsync(CancellationToken cancellationToken);
    Task<ArticleCategory?> FindCategoryAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CategorySlugExistsAsync(string slug, Guid? exceptId, CancellationToken cancellationToken);
    Task SaveCategoryAsync(ArticleCategory category, CancellationToken cancellationToken);
}
