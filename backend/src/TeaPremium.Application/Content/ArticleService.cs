using System.Text.RegularExpressions;
using System.Net;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Content;
using TeaPremium.Domain.Seo;
using TeaPremium.Application.Seo;

namespace TeaPremium.Application.Content;

public sealed partial class ArticleService(IArticleStore store, ISeoRedirectStore redirects, ArticleContentRenderer renderer, TimeProvider clock)
{
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagPattern();

    public Task<ArticlePage<ArticleSummary>> ListAsync(bool publicOnly, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1 || pageSize is < 1 or > 48) throw new ContentValidationException("Invalid pagination.");
        return store.ListAsync(publicOnly, page, pageSize, ct);
    }

    public async Task<ArticleDetail> GetAsync(Guid id, CancellationToken ct)
    {
        var article = await store.FindAsync(id, ct) ?? throw new ContentNotFoundException("Article not found.");
        return await store.DetailAsync(article, true, ct);
    }

    public async Task<ArticleDetail> GetPublicAsync(string slug, CancellationToken ct)
    {
        if (!ValidSlug(slug)) throw new ContentNotFoundException("Article not found.");
        var article = await store.FindBySlugAsync(slug, true, ct) ?? throw new ContentNotFoundException("Article not found.");
        return await store.DetailAsync(article, false, ct);
    }

    public async Task<ArticleDetail> CreateAsync(ArticleInput input, Guid authorId, CancellationToken ct)
    {
        Validate(input);
        if (await store.SlugExistsAsync(input.Slug, null, ct)) throw new ContentConflictException("Article slug already exists.");
        if (await redirects.SourceExistsAsync(SeoRedirect.ContentPath("kien-thuc", input.Slug), ct))
            throw new ContentConflictException("Article slug is reserved by a redirect.");
        await ValidateReferencesAsync(input, ct);
        var html = renderer.Render(input.ContentJson);
        var article = new Article(input.Slug, input.Title.Trim(), input.Excerpt.Trim(), input.ContentJson,
            html, authorId, input.CoverAssetId, input.Seo.ToMetadata());
        article.Update(input.Slug, input.Title.Trim(), input.Excerpt.Trim(), input.ContentJson,
            html, input.CoverAssetId, input.Seo.ToMetadata(), input.CategoryIds);
        await store.ExecuteInTransactionAsync(async token =>
        {
            await redirects.EnsurePathAvailableAsync(SeoRedirect.ContentPath("kien-thuc", input.Slug), token);
            await store.SaveAsync(article, token);
        }, ct);
        return await store.DetailAsync(article, true, ct);
    }

    public async Task<ArticleDetail> UpdateAsync(Guid id, ArticleInput input, CancellationToken ct)
    {
        Validate(input);
        var article = await store.FindAsync(id, ct) ?? throw new ContentNotFoundException("Article not found.");
        var previousSlug = article.Slug;
        var slugChanged = previousSlug != input.Slug;
        if (slugChanged && article.Status == PublicationStatus.Published &&
            SeoRedirect.CanonicalPointsToSource(input.Seo.CanonicalOverride,
                SeoRedirect.ContentPath("kien-thuc", previousSlug)))
            throw new ContentValidationException("Clear the old canonical URL before changing a Published slug.");
        if (await store.SlugExistsAsync(input.Slug, id, ct)) throw new ContentConflictException("Article slug already exists.");
        if (slugChanged && await redirects.SourceExistsAsync(SeoRedirect.ContentPath("kien-thuc", input.Slug), ct))
            throw new ContentConflictException("Article slug is reserved by a redirect.");
        await ValidateReferencesAsync(input, ct);
        var html = renderer.Render(input.ContentJson);
        await store.ExecuteInTransactionAsync(async token =>
        {
            if (slugChanged && article.Status == PublicationStatus.Published)
                await redirects.RecordSlugChangeAsync(
                    SeoRedirect.ContentPath("kien-thuc", previousSlug),
                    SeoRedirect.ContentPath("kien-thuc", input.Slug), token);
            else if (slugChanged)
                await redirects.EnsurePathAvailableAsync(
                    SeoRedirect.ContentPath("kien-thuc", input.Slug), token);
            article.Update(input.Slug, input.Title.Trim(), input.Excerpt.Trim(), input.ContentJson,
                html, input.CoverAssetId, input.Seo.ToMetadata(), input.CategoryIds);
            await store.SaveAsync(article, token);
        }, ct);
        return await store.DetailAsync(article, true, ct);
    }

    public async Task<ArticleDetail> TransitionAsync(Guid id, string transition, CancellationToken ct)
    {
        var article = await store.FindAsync(id, ct) ?? throw new ContentNotFoundException("Article not found.");
        try
        {
            switch (transition)
            {
                case "review": article.SubmitForReview(); break;
                case "publish":
                    if (string.IsNullOrWhiteSpace(WebUtility.HtmlDecode(HtmlTagPattern().Replace(article.ContentHtml, ""))) ||
                        string.IsNullOrWhiteSpace(article.Seo.SeoTitle) ||
                        string.IsNullOrWhiteSpace(article.Seo.MetaDescription))
                        throw new ContentValidationException("Article content and SEO title/description are required to publish.");
                    article.Publish(clock.GetUtcNow()); break;
                case "archive": article.Archive(clock.GetUtcNow()); break;
                default: throw new ContentValidationException("Unknown article transition.");
            }
        }
        catch (InvalidOperationException error) { throw new ContentConflictException(error.Message); }
        await store.SaveAsync(article, ct);
        return await store.DetailAsync(article, true, ct);
    }

    public Task<IReadOnlyList<ArticleCategoryDto>> CategoriesAsync(CancellationToken ct) => store.CategoriesAsync(ct);

    public async Task<ArticleCategoryDto> SaveCategoryAsync(Guid? id, ArticleCategoryInput input, CancellationToken ct)
    {
        if (!ValidSlug(input.Slug) || string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 300)
            throw new ContentValidationException("Invalid article category.");
        if (await store.CategorySlugExistsAsync(input.Slug, id, ct))
            throw new ContentConflictException("Article category slug already exists.");
        var category = id is null ? new ArticleCategory(input.Slug, input.Name.Trim()) :
            await store.FindCategoryAsync(id.Value, ct) ?? throw new ContentNotFoundException("Article category not found.");
        category.Update(input.Slug, input.Name.Trim());
        await store.SaveCategoryAsync(category, ct);
        return new ArticleCategoryDto(category.Id, category.Slug, category.Name);
    }

    private static bool ValidSlug(string? value) => value is { Length: > 0 and <= 200 } && SlugPattern().IsMatch(value);
    private static void Validate(ArticleInput input)
    {
        if (!ValidSlug(input.Slug) || string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 300 ||
            input.Excerpt is null || input.Excerpt.Length > 1000 || input.CategoryIds is null ||
            input.CategoryIds.Count > 20 || input.Seo is null ||
            input.Seo.SeoTitle?.Length > 200 || input.Seo.MetaDescription?.Length > 500 ||
            input.Seo.OgTitle?.Length > 200 || input.Seo.OgDescription?.Length > 500)
            throw new ContentValidationException("Article fields are invalid.");
        if (input.Seo.CanonicalOverride is { Length: > 0 } canonical &&
            (!Uri.TryCreate(canonical, UriKind.Absolute, out var uri) || uri.Scheme != "https"))
            throw new ContentValidationException("Canonical URL must be HTTPS.");
    }

    private async Task ValidateReferencesAsync(ArticleInput input, CancellationToken ct)
    {
        if (!await store.AssetsAndCategoriesExistAsync(input.CoverAssetId, input.Seo.OgImageAssetId,
                input.CategoryIds.Distinct().ToArray(), ct))
            throw new ContentValidationException("Article category or media asset does not exist.");
    }
}
