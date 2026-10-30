using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Media;

namespace TeaPremium.Application.Catalog;

public interface ICatalogDataStore
{
    Task<PagedResult<AdminProductListItem>> QueryAdminProductsAsync(
        AdminProductListQuery query,
        CancellationToken cancellationToken);

    Task<AdminProductDetail?> QueryAdminProductAsync(Guid id, CancellationToken cancellationToken);

    Task<Product?> LoadProductAsync(
        Guid id,
        bool includeCategories,
        bool includeMedia,
        CancellationToken cancellationToken);

    Task<bool> ProductSlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);

    Task<bool> ProductSkuExistsAsync(string sku, Guid? excludingId, CancellationToken cancellationToken);

    Task<HashSet<Guid>> GetExistingCategoryIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    Task<bool> BrandExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> MediaAssetExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ProductHasAssetAsync(Guid productId, Guid assetId, CancellationToken cancellationToken);

    void AddProduct(Product product);

    Task<IReadOnlyList<AdminCategoryNode>> QueryAdminCategoriesAsync(CancellationToken cancellationToken);

    Task<AdminCategoryDetail?> QueryAdminCategoryAsync(Guid id, CancellationToken cancellationToken);

    Task<Category?> LoadCategoryAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> CategorySlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);

    Task<bool> CategoryWouldCreateCycleAsync(
        Guid categoryId,
        Guid parentId,
        CancellationToken cancellationToken);

    Task<bool> CategoryIsPublishedAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> CategoryIsUsedByPublishedProductsAsync(Guid id, CancellationToken cancellationToken);

    void AddCategory(Category category);

    Task<IReadOnlyList<AdminBrandDto>> QueryAdminBrandsAsync(CancellationToken cancellationToken);

    Task<AdminBrandDto?> QueryAdminBrandAsync(Guid id, CancellationToken cancellationToken);

    Task<Brand?> LoadBrandAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> BrandSlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);

    void AddBrand(Brand brand);

    void AddProductMedia(ProductMedia media);

    void RemoveProductMedia(ProductMedia media);

    Task<PagedResult<PublicProductListItem>> QueryPublicProductsAsync(
        PublicProductListQuery query,
        CancellationToken cancellationToken);

    Task<PublicProductDetail?> QueryPublicProductAsync(string slug, CancellationToken cancellationToken);

    Task<IReadOnlyList<PublicCategoryNode>> QueryPublicCategoriesAsync(CancellationToken cancellationToken);

    Task<PublicCategoryDetail?> QueryPublicCategoryAsync(
        string slug,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);
}
