using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TeaPremium.Application.Content;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Content;
using TeaPremium.Infrastructure.Persistence;
using TeaPremium.Infrastructure.Media;

namespace TeaPremium.Infrastructure.Content;

internal sealed class ArticleStore(AppDbContext db, IConfiguration configuration) : IArticleStore
{
    public Task<Article?> FindAsync(Guid id, CancellationToken ct) =>
        db.Articles.Include(x => x.Categories).FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Article?> FindBySlugAsync(string slug, bool publicOnly, CancellationToken ct) =>
        db.Articles.AsNoTracking().Include(x => x.Categories).FirstOrDefaultAsync(x => x.Slug == slug &&
            (!publicOnly || x.Status == PublicationStatus.Published), ct);

    public Task<bool> SlugExistsAsync(string slug, Guid? exceptId, CancellationToken ct) =>
        db.Articles.AnyAsync(x => x.Slug == slug && (exceptId == null || x.Id != exceptId), ct);

    public async Task<bool> AssetsAndCategoriesExistAsync(Guid? coverId, Guid? ogImageId,
        IReadOnlyCollection<Guid> categoryIds, CancellationToken ct)
    {
        var ids = categoryIds.Distinct().ToArray();
        if (await db.ArticleCategories.CountAsync(x => ids.Contains(x.Id), ct) != ids.Length) return false;
        var assetIds = new[] { coverId, ogImageId }.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        return await db.MediaAssets.CountAsync(x => assetIds.Contains(x.Id), ct) == assetIds.Length;
    }

    public async Task SaveAsync(Article article, CancellationToken ct)
    {
        if (db.Entry(article).State == EntityState.Detached) db.Articles.Add(article);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "23505" })
        { throw new ContentConflictException("Article slug already exists."); }
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task<ArticleDetail> DetailAsync(Article article, bool includeDraft, CancellationToken ct)
    {
        var categoryIds = article.Categories.Select(x => x.CategoryId).ToArray();
        var categories = await db.ArticleCategories.AsNoTracking()
            .Where(x => categoryIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .Select(x => new ArticleCategoryDto(x.Id, x.Slug, x.Name)).ToListAsync(ct);
        var cover = article.CoverAssetId is Guid id
            ? await db.MediaAssets.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new { x.StorageKey, x.Width, x.Height }).FirstOrDefaultAsync(ct)
            : null;
        var seo = article.Seo;
        var ogImage = seo.OgImageAssetId is Guid ogId
            ? await db.MediaAssets.AsNoTracking().Where(x => x.Id == ogId)
                .Select(x => new { x.StorageKey, x.Width, x.Height }).FirstOrDefaultAsync(ct)
            : null;
        return new ArticleDetail(article.Id, article.Slug, article.Title, article.Excerpt,
            article.ContentHtml, includeDraft ? article.ContentJson : null, article.Status.ToString(),
            article.PublishedAt, article.UpdatedAtUtc,
            new ArticleSeoInput(seo.SeoTitle, seo.MetaDescription, seo.CanonicalOverride,
                seo.RobotsIndex, seo.OgTitle, seo.OgDescription, seo.OgImageAssetId), categories,
            CoverUrl(cover?.StorageKey), cover?.Width, cover?.Height, article.CoverAssetId,
            CoverUrl(ogImage?.StorageKey), ogImage?.Width, ogImage?.Height);
    }

    public async Task<ArticlePage<ArticleSummary>> ListAsync(bool publicOnly, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Articles.AsNoTracking().AsQueryable();
        if (publicOnly) query = query.Where(x => x.Status == PublicationStatus.Published);
        var total = await query.CountAsync(ct);
        var articles = await query.OrderByDescending(x => publicOnly ? x.PublishedAt : x.UpdatedAtUtc)
            .ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var coverIds = articles.Where(x => x.CoverAssetId.HasValue).Select(x => x.CoverAssetId!.Value).ToArray();
        var covers = await db.MediaAssets.AsNoTracking().Where(x => coverIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
        var items = articles.Select(x =>
        {
            var cover = x.CoverAssetId is Guid id && covers.TryGetValue(id, out var asset) ? asset : null;
            return new ArticleSummary(x.Id, x.Slug, x.Title, x.Excerpt, x.Status.ToString(), x.PublishedAt,
                x.UpdatedAtUtc, CoverUrl(cover?.StorageKey), cover?.Width, cover?.Height);
        }).ToArray();
        return new ArticlePage<ArticleSummary>(items, page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<IReadOnlyList<ArticleCategoryDto>> CategoriesAsync(CancellationToken ct) =>
        await db.ArticleCategories.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new ArticleCategoryDto(x.Id, x.Slug, x.Name)).ToListAsync(ct);
    public Task<ArticleCategory?> FindCategoryAsync(Guid id, CancellationToken ct) =>
        db.ArticleCategories.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> CategorySlugExistsAsync(string slug, Guid? exceptId, CancellationToken ct) =>
        db.ArticleCategories.AnyAsync(x => x.Slug == slug && (exceptId == null || x.Id != exceptId), ct);
    public async Task SaveCategoryAsync(ArticleCategory category, CancellationToken ct)
    {
        if (db.Entry(category).State == EntityState.Detached) db.ArticleCategories.Add(category);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "23505" })
        { throw new ContentConflictException("Article category slug already exists."); }
    }

    private string? CoverUrl(string? key)
    {
        if (key is not null && DemoMediaPath.Resolve(key) is { } bundledUrl) return bundledUrl;
        var baseUrl = configuration["R2_PUBLIC_BASE_URL"]?.TrimEnd('/');
        return key is null || string.IsNullOrWhiteSpace(baseUrl) ? null :
            $"{baseUrl}/{string.Join('/', key.Split('/').Select(Uri.EscapeDataString))}";
    }
}
