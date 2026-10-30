using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TeaPremium.Application.Catalog;
using TeaPremium.Application.Media;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Common;
using TeaPremium.Domain.Media;
using TeaPremium.Infrastructure.Persistence;

namespace TeaPremium.Infrastructure.Catalog;

internal sealed class CatalogDataStore(
    AppDbContext dbContext,
    IObjectStorageService storage) : ICatalogDataStore
{
    public async Task<PagedResult<AdminProductListItem>> QueryAdminProductsAsync(
        AdminProductListQuery query,
        CancellationToken cancellationToken)
    {
        var products = dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            products = products.Where(product =>
                EF.Functions.ILike(product.Name, pattern) ||
                EF.Functions.ILike(product.Slug, pattern) ||
                product.Sku != null && EF.Functions.ILike(product.Sku, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            CatalogRules.TryParseStatus(query.Status, out var status))
        {
            products = products.Where(product => product.Status == status);
        }

        if (query.CategoryId is { } categoryId)
        {
            products = products.Where(product =>
                product.ProductCategories.Any(link => link.CategoryId == categoryId));
        }

        if (query.BrandId is { } brandId)
        {
            products = products.Where(product => product.BrandId == brandId);
        }

        var totalItems = await products.LongCountAsync(cancellationToken);
        var ordered = ApplyAdminSort(products, query.Sort);
        var rows = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(product => new
            {
                product.Id,
                product.Sku,
                product.Slug,
                product.Name,
                product.Summary,
                CoverImage = product.Media
                    .Where(media => media.IsCover)
                    .Select(media => new AdminProductThumbnailRow(
                        media.AssetId,
                        media.Asset.StorageKey,
                        media.Asset.Width,
                        media.Asset.Height,
                        media.AltText))
                    .SingleOrDefault(),
                product.Status,
                product.PrimaryCategoryId,
                PrimaryCategoryName = product.PrimaryCategory == null
                    ? null
                    : product.PrimaryCategory.Name,
                product.BrandId,
                BrandName = product.Brand == null ? null : product.Brand.Name,
                product.PublishedAt,
                product.UpdatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new AdminProductListItem(
                row.Id,
                row.Sku,
                row.Slug,
                row.Name,
                row.Summary,
                row.CoverImage is null
                    ? null
                    : new AdminProductThumbnailDto(
                        row.CoverImage.AssetId,
                        storage.GetPublicUrl(row.CoverImage.StorageKey),
                        row.CoverImage.Width,
                        row.CoverImage.Height,
                        row.CoverImage.AltText),
                row.Status.ToString(),
                row.PrimaryCategoryId,
                row.PrimaryCategoryName,
                row.BrandId,
                row.BrandName,
                row.PublishedAt,
                row.UpdatedAtUtc))
            .ToArray();

        return Page(items, query.Page, query.PageSize, totalItems);
    }

    public async Task<AdminProductDetail?> QueryAdminProductAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.ProductCategories)
                .ThenInclude(link => link.Category)
            .Include(item => item.Media)
                .ThenInclude(media => media.Asset)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return product is null ? null : MapAdminProduct(product);
    }

    public async Task<Product?> LoadProductAsync(
        Guid id,
        bool includeCategories,
        bool includeMedia,
        CancellationToken cancellationToken)
    {
        IQueryable<Product> query = dbContext.Products;

        if (includeCategories)
        {
            query = query.Include(product => product.ProductCategories);
        }

        if (includeMedia)
        {
            query = query.Include(product => product.Media);
        }

        return await query.SingleOrDefaultAsync(product => product.Id == id, cancellationToken);
    }

    public Task<bool> ProductSlugExistsAsync(
        string slug,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        dbContext.Products.AnyAsync(
            product => product.Slug == slug &&
                       (!excludingId.HasValue || product.Id != excludingId.Value),
            cancellationToken);

    public Task<bool> ProductSkuExistsAsync(
        string sku,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        dbContext.Products.AnyAsync(
            product => product.Sku == sku &&
                       (!excludingId.HasValue || product.Id != excludingId.Value),
            cancellationToken);

    public async Task<HashSet<Guid>> GetExistingCategoryIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var values = await dbContext.Categories
            .AsNoTracking()
            .Where(category => ids.Contains(category.Id))
            .Select(category => category.Id)
            .ToListAsync(cancellationToken);
        return values.ToHashSet();
    }

    public Task<bool> BrandExistsAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Brands.AsNoTracking().AnyAsync(brand => brand.Id == id, cancellationToken);

    public Task<bool> MediaAssetExistsAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.MediaAssets.AsNoTracking().AnyAsync(asset => asset.Id == id, cancellationToken);

    public Task<bool> ProductHasAssetAsync(
        Guid productId,
        Guid assetId,
        CancellationToken cancellationToken) =>
        dbContext.ProductMedia.AsNoTracking().AnyAsync(
            media => media.ProductId == productId && media.AssetId == assetId,
            cancellationToken);

    public void AddProduct(Product product) => dbContext.Products.Add(product);

    public async Task<IReadOnlyList<AdminCategoryNode>> QueryAdminCategoriesAsync(
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryRow(
                category.Id,
                category.ParentId,
                category.Slug,
                category.Name,
                category.SortOrder,
                category.Status))
            .ToListAsync(cancellationToken);

        return BuildAdminTree(rows);
    }

    public async Task<AdminCategoryDetail?> QueryAdminCategoryAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return category is null
            ? null
            : new AdminCategoryDetail(
                category.Id,
                category.ParentId,
                category.Slug,
                category.Name,
                category.DescriptionHtml,
                category.SortOrder,
                category.Status.ToString(),
                MapSeo(category.Seo),
                category.CreatedAtUtc,
                category.UpdatedAtUtc);
    }

    public Task<Category?> LoadCategoryAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Categories.SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

    public Task<bool> CategorySlugExistsAsync(
        string slug,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        dbContext.Categories.AnyAsync(
            category => category.Slug == slug &&
                        (!excludingId.HasValue || category.Id != excludingId.Value),
            cancellationToken);

    public async Task<bool> CategoryWouldCreateCycleAsync(
        Guid categoryId,
        Guid parentId,
        CancellationToken cancellationToken)
    {
        var parents = await dbContext.Categories
            .AsNoTracking()
            .Select(category => new { category.Id, category.ParentId })
            .ToDictionaryAsync(item => item.Id, item => item.ParentId, cancellationToken);

        var visited = new HashSet<Guid>();
        Guid? current = parentId;
        while (current is { } currentId && visited.Add(currentId))
        {
            if (currentId == categoryId)
            {
                return true;
            }

            current = parents.GetValueOrDefault(currentId);
        }

        return current is not null;
    }

    public Task<bool> CategoryIsPublishedAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Categories.AsNoTracking().AnyAsync(
            category => category.Id == id && category.Status == PublicationStatus.Published,
            cancellationToken);

    public Task<bool> CategoryIsUsedByPublishedProductsAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        dbContext.Products.AsNoTracking().AnyAsync(
            product => product.Status == PublicationStatus.Published &&
                       (product.PrimaryCategoryId == id ||
                        product.ProductCategories.Any(link => link.CategoryId == id)),
            cancellationToken);

    public void AddCategory(Category category) => dbContext.Categories.Add(category);

    public async Task<IReadOnlyList<AdminBrandDto>> QueryAdminBrandsAsync(
        CancellationToken cancellationToken) =>
        await dbContext.Brands
            .AsNoTracking()
            .OrderBy(brand => brand.Name)
            .Select(brand => new AdminBrandDto(brand.Id, brand.Name, brand.Slug, brand.Origin))
            .ToListAsync(cancellationToken);

    public Task<AdminBrandDto?> QueryAdminBrandAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        dbContext.Brands
            .AsNoTracking()
            .Where(brand => brand.Id == id)
            .Select(brand => new AdminBrandDto(brand.Id, brand.Name, brand.Slug, brand.Origin))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Brand?> LoadBrandAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Brands.SingleOrDefaultAsync(brand => brand.Id == id, cancellationToken);

    public Task<bool> BrandSlugExistsAsync(
        string slug,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        dbContext.Brands.AnyAsync(
            brand => brand.Slug == slug &&
                     (!excludingId.HasValue || brand.Id != excludingId.Value),
            cancellationToken);

    public void AddBrand(Brand brand) => dbContext.Brands.Add(brand);

    public void AddProductMedia(ProductMedia media) => dbContext.ProductMedia.Add(media);

    public void RemoveProductMedia(ProductMedia media) => dbContext.ProductMedia.Remove(media);

    public async Task<PagedResult<PublicProductListItem>> QueryPublicProductsAsync(
        PublicProductListQuery query,
        CancellationToken cancellationToken)
    {
        var products = PublishedProducts();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            products = products.Where(product =>
                EF.Functions.ILike(product.Name, pattern) ||
                EF.Functions.ILike(product.Slug, pattern) ||
                product.Sku != null && EF.Functions.ILike(product.Sku, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var categorySlug = query.Category.Trim();
            products = products.Where(product =>
                product.ProductCategories.Any(link =>
                    link.Category.Slug == categorySlug &&
                    link.Category.Status == PublicationStatus.Published));
        }

        var totalItems = await products.LongCountAsync(cancellationToken);
        var ordered = ApplyPublicSort(products, query.Sort);
        var rows = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(product => new PublicProductListItem(
                product.Id,
                product.Slug,
                product.Name,
                product.Summary,
                product.Media
                    .Where(media => media.IsCover)
                    .Select(media => new PublicMediaDto(
                        media.AssetId,
                        media.Asset.StorageKey,
                        null,
                        media.Asset.MimeType,
                        media.Asset.Width,
                        media.Asset.Height,
                        media.AltText,
                        media.SortOrder,
                        media.IsCover))
                    .SingleOrDefault(),
                product.PrimaryCategory == null
                    ? null
                    : new PublicCategorySummary(
                        product.PrimaryCategory.Id,
                        product.PrimaryCategory.Slug,
                        product.PrimaryCategory.Name),
                product.Brand == null
                    ? null
                    : new PublicBrandDto(
                        product.Brand.Id,
                        product.Brand.Slug,
                        product.Brand.Name,
                        product.Brand.Origin)))
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(item => item.CoverImage is null
                ? item
                : item with
                {
                    CoverImage = item.CoverImage with
                    {
                        PublicUrl = storage.GetPublicUrl(item.CoverImage.StorageKey),
                    },
                })
            .ToArray();

        return Page(items, query.Page, query.PageSize, totalItems);
    }

    public async Task<PublicProductDetail?> QueryPublicProductAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var product = await PublishedProducts()
            .AsSplitQuery()
            .Include(item => item.PrimaryCategory)
            .Include(item => item.Brand)
            .Include(item => item.ProductCategories)
                .ThenInclude(link => link.Category)
            .Include(item => item.Media)
                .ThenInclude(media => media.Asset)
            .SingleOrDefaultAsync(item => item.Slug == slug, cancellationToken);

        if (product?.PrimaryCategory is null || product.PublishedAt is null)
        {
            return null;
        }

        var categories = product.ProductCategories
            .Where(link => link.Category.Status == PublicationStatus.Published)
            .OrderBy(link => link.Category.SortOrder)
            .ThenBy(link => link.Category.Name)
            .Select(link => new PublicCategorySummary(
                link.Category.Id,
                link.Category.Slug,
                link.Category.Name))
            .ToArray();
        var media = product.Media
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .Select(MapPublicMedia)
            .ToArray();

        return new PublicProductDetail(
            product.Id,
            product.Sku,
            product.Slug,
            product.Name,
            product.Summary,
            ParseJson(product.DescriptionJson),
            product.DescriptionHtml,
            product.ProductType,
            product.MaterialText,
            product.CapacityMl,
            ParseJson(product.SpecsJson),
            product.PublishedAt.Value,
            MapSeo(product.Seo),
            new PublicCategorySummary(
                product.PrimaryCategory.Id,
                product.PrimaryCategory.Slug,
                product.PrimaryCategory.Name),
            categories,
            product.Brand is null
                ? null
                : new PublicBrandDto(
                    product.Brand.Id,
                    product.Brand.Slug,
                    product.Brand.Name,
                    product.Brand.Origin),
            media);
    }

    public async Task<IReadOnlyList<PublicCategoryNode>> QueryPublicCategoriesAsync(
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.Status == PublicationStatus.Published)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryRow(
                category.Id,
                category.ParentId,
                category.Slug,
                category.Name,
                category.SortOrder,
                category.Status))
            .ToListAsync(cancellationToken);

        return BuildPublicTree(rows);
    }

    public async Task<PublicCategoryDetail?> QueryPublicCategoryAsync(
        string slug,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Slug == slug && item.Status == PublicationStatus.Published,
                cancellationToken);
        if (category is null)
        {
            return null;
        }

        var products = await QueryPublicProductsAsync(
            new PublicProductListQuery(page, pageSize, Category: slug),
            cancellationToken);

        return new PublicCategoryDetail(
            category.Id,
            category.Slug,
            category.Name,
            category.DescriptionHtml,
            MapSeo(category.Seo),
            products);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new CatalogConflictException(
                "The catalog record was changed by another request. Reload it and try again.");
        }
        catch (DbUpdateException exception)
            when (exception.GetBaseException() is PostgresException postgresException)
        {
            throw MapDatabaseConflict(postgresException);
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (dbContext.Database.CurrentTransaction is not null)
        {
            await operation(cancellationToken);
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    private IQueryable<Product> PublishedProducts() =>
        dbContext.Products
            .AsNoTracking()
            .Where(product =>
                product.Status == PublicationStatus.Published &&
                product.PublishedAt != null &&
                product.PrimaryCategory != null &&
                product.PrimaryCategory.Status == PublicationStatus.Published);

    private static IOrderedQueryable<Product> ApplyAdminSort(
        IQueryable<Product> products,
        string sort) =>
        sort.ToLowerInvariant() switch
        {
            "created_desc" => products
                .OrderByDescending(product => product.CreatedAtUtc)
                .ThenBy(product => product.Id),
            "name_asc" => products
                .OrderBy(product => product.Name)
                .ThenBy(product => product.Id),
            "name_desc" => products
                .OrderByDescending(product => product.Name)
                .ThenBy(product => product.Id),
            "status_asc" => products
                .OrderBy(product => product.Status)
                .ThenByDescending(product => product.UpdatedAtUtc)
                .ThenBy(product => product.Id),
            "published_desc" => products
                .OrderByDescending(product => product.PublishedAt)
                .ThenBy(product => product.Id),
            _ => products
                .OrderByDescending(product => product.UpdatedAtUtc)
                .ThenBy(product => product.Id),
        };

    private static IOrderedQueryable<Product> ApplyPublicSort(
        IQueryable<Product> products,
        string sort) =>
        sort.ToLowerInvariant() switch
        {
            "name_asc" => products
                .OrderBy(product => product.Name)
                .ThenBy(product => product.Id),
            "name_desc" => products
                .OrderByDescending(product => product.Name)
                .ThenBy(product => product.Id),
            _ => products
                .OrderByDescending(product => product.PublishedAt)
                .ThenBy(product => product.Id),
        };

    private AdminProductDetail MapAdminProduct(Product product) =>
        new(
            product.Id,
            product.Sku,
            product.Slug,
            product.Name,
            product.Summary,
            ParseJson(product.DescriptionJson),
            product.DescriptionHtml,
            product.Status.ToString(),
            product.PrimaryCategoryId,
            product.BrandId,
            product.ProductType,
            product.MaterialText,
            product.CapacityMl,
            ParseJson(product.SpecsJson),
            product.PublishedAt,
            MapSeo(product.Seo),
            product.ProductCategories
                .OrderBy(link => link.Category.SortOrder)
                .ThenBy(link => link.Category.Name)
                .Select(link => new AdminCategorySummary(
                    link.Category.Id,
                    link.Category.Slug,
                    link.Category.Name,
                    link.Category.Status.ToString()))
                .ToArray(),
            product.Media
                .OrderBy(media => media.SortOrder)
                .ThenBy(media => media.Id)
                .Select(media => new AdminProductMediaDto(
                    media.Id,
                    media.AssetId,
                    media.Asset.StorageKey,
                    storage.GetPublicUrl(media.Asset.StorageKey),
                    media.Asset.MimeType,
                    media.Asset.Width,
                    media.Asset.Height,
                    media.AltText,
                    media.SortOrder,
                    media.IsCover))
                .ToArray(),
            product.CreatedAtUtc,
            product.UpdatedAtUtc);

    private PublicMediaDto MapPublicMedia(ProductMedia media) =>
        new(
            media.AssetId,
            media.Asset.StorageKey,
            storage.GetPublicUrl(media.Asset.StorageKey),
            media.Asset.MimeType,
            media.Asset.Width,
            media.Asset.Height,
            media.AltText,
            media.SortOrder,
            media.IsCover);

    private static SeoDto MapSeo(SeoMetadata seo) =>
        new(
            seo.SeoTitle,
            seo.MetaDescription,
            seo.CanonicalOverride,
            seo.RobotsIndex,
            seo.OgTitle,
            seo.OgDescription,
            seo.OgImageAssetId);

    private static JsonElement ParseJson(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    private static PagedResult<T> Page<T>(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        long totalItems) =>
        new(
            items,
            page,
            pageSize,
            totalItems,
            totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize));

    private static List<AdminCategoryNode> BuildAdminTree(IReadOnlyList<CategoryRow> rows)
    {
        var knownIds = rows.Select(row => row.Id).ToHashSet();
        var children = rows
            .Where(row => row.ParentId is not null && knownIds.Contains(row.ParentId.Value))
            .GroupBy(row => row.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var built = new HashSet<Guid>();

        AdminCategoryNode Build(CategoryRow row, HashSet<Guid> path)
        {
            built.Add(row.Id);
            if (!path.Add(row.Id))
            {
                return new AdminCategoryNode(
                    row.Id,
                    row.ParentId,
                    row.Slug,
                    row.Name,
                    row.SortOrder,
                    row.Status.ToString(),
                    []);
            }

            var nodes = children.GetValueOrDefault(row.Id, [])
                .Where(child => !path.Contains(child.Id))
                .OrderBy(child => child.SortOrder)
                .ThenBy(child => child.Name)
                .Select(child => Build(child, new HashSet<Guid>(path)))
                .ToArray();
            return new AdminCategoryNode(
                row.Id,
                row.ParentId,
                row.Slug,
                row.Name,
                row.SortOrder,
                row.Status.ToString(),
                nodes);
        }

        var result = rows
            .Where(row => row.ParentId is null || !knownIds.Contains(row.ParentId.Value))
            .OrderBy(row => row.SortOrder)
            .ThenBy(row => row.Name)
            .Select(row => Build(row, []))
            .ToList();

        foreach (var remaining in rows.Where(row => !built.Contains(row.Id)))
        {
            result.Add(Build(remaining, []));
        }

        return result;
    }

    private static PublicCategoryNode[] BuildPublicTree(IReadOnlyList<CategoryRow> rows)
    {
        var knownIds = rows.Select(row => row.Id).ToHashSet();
        var children = rows
            .Where(row => row.ParentId is not null && knownIds.Contains(row.ParentId.Value))
            .GroupBy(row => row.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.ToArray());

        PublicCategoryNode Build(CategoryRow row) =>
            new(
                row.Id,
                row.Slug,
                row.Name,
                row.SortOrder,
                children.GetValueOrDefault(row.Id, [])
                    .OrderBy(child => child.SortOrder)
                    .ThenBy(child => child.Name)
                    .Select(Build)
                    .ToArray());

        return rows
            .Where(row => row.ParentId is null || !knownIds.Contains(row.ParentId.Value))
            .OrderBy(row => row.SortOrder)
            .ThenBy(row => row.Name)
            .Select(Build)
            .ToArray();
    }

    private static CatalogConflictException MapDatabaseConflict(PostgresException exception) =>
        exception.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => MapUniqueConflict(exception.ConstraintName),
            PostgresErrorCodes.ForeignKeyViolation => new CatalogConflictException(
                "A referenced catalog record changed while the request was being processed."),
            _ => new CatalogConflictException(
                "The catalog change conflicted with a database constraint."),
        };

    private static CatalogConflictException MapUniqueConflict(string? constraintName) =>
        constraintName switch
        {
            "ux_products_slug" => new CatalogConflictException("The product slug is already in use."),
            "ux_products_sku_not_null" => new CatalogConflictException("The product SKU is already in use."),
            "ux_categories_slug" => new CatalogConflictException("The category slug is already in use."),
            "ux_brands_slug" => new CatalogConflictException("The brand slug is already in use."),
            "ux_product_media_one_cover_per_product" => new CatalogConflictException(
                "The product already has a cover image."),
            _ => new CatalogConflictException("A unique catalog value is already in use."),
        };

    private sealed record CategoryRow(
        Guid Id,
        Guid? ParentId,
        string Slug,
        string Name,
        int SortOrder,
        PublicationStatus Status);

    private sealed record AdminProductThumbnailRow(
        Guid AssetId,
        string StorageKey,
        int Width,
        int Height,
        string AltText);
}
