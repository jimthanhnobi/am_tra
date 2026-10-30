namespace TeaPremium.Application.Catalog;

public interface ICatalogService
{
    Task<PagedResult<AdminProductListItem>> GetAdminProductsAsync(
        AdminProductListQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminProductDetail> GetAdminProductAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AdminProductDetail> CreateProductAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminProductDetail> UpdateProductAsync(
        Guid id,
        UpdateProductCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminProductDetail> PublishProductAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AdminProductDetail> ArchiveProductAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminCategoryNode>> GetAdminCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<AdminCategoryDetail> GetAdminCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryDetail> CreateCategoryAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryDetail> UpdateCategoryAsync(
        Guid id,
        UpdateCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminBrandDto>> GetAdminBrandsAsync(
        CancellationToken cancellationToken = default);

    Task<AdminBrandDto> GetAdminBrandAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AdminBrandDto> CreateBrandAsync(
        CreateBrandCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminBrandDto> UpdateBrandAsync(
        Guid id,
        UpdateBrandCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminProductMediaDto> AttachProductMediaAsync(
        Guid productId,
        AttachProductMediaCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminProductMediaDto> UpdateProductMediaAsync(
        Guid productId,
        Guid mediaId,
        UpdateProductMediaCommand command,
        CancellationToken cancellationToken = default);

    Task RemoveProductMediaAsync(
        Guid productId,
        Guid mediaId,
        CancellationToken cancellationToken = default);

    Task<AdminProductMediaDto> SetProductCoverAsync(
        Guid productId,
        Guid mediaId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PublicProductListItem>> GetPublicProductsAsync(
        PublicProductListQuery query,
        CancellationToken cancellationToken = default);

    Task<PublicProductDetail> GetPublicProductAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublicCategoryNode>> GetPublicCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<PublicCategoryDetail> GetPublicCategoryAsync(
        string slug,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
