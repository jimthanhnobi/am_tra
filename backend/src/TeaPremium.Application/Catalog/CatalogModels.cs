using System.Text.Json;

namespace TeaPremium.Application.Catalog;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalItems,
    int TotalPages);

public sealed record SeoInput(
    string? SeoTitle,
    string? MetaDescription,
    string? CanonicalOverride = null,
    bool RobotsIndex = true,
    string? OgTitle = null,
    string? OgDescription = null,
    Guid? OgImageAssetId = null);

public sealed record SeoDto(
    string? SeoTitle,
    string? MetaDescription,
    string? CanonicalOverride,
    bool RobotsIndex,
    string? OgTitle,
    string? OgDescription,
    Guid? OgImageAssetId);

public sealed record CreateProductCommand(
    string? Sku,
    string Slug,
    string Name,
    string Summary,
    JsonElement Description,
    string DescriptionHtml,
    string ProductType,
    string? MaterialText,
    int? CapacityMl,
    JsonElement Specifications,
    Guid? PrimaryCategoryId,
    Guid? BrandId,
    IReadOnlyCollection<Guid>? CategoryIds,
    SeoInput? Seo);

public sealed record UpdateProductCommand(
    string? Sku,
    string Slug,
    string Name,
    string Summary,
    JsonElement Description,
    string DescriptionHtml,
    string ProductType,
    string? MaterialText,
    int? CapacityMl,
    JsonElement Specifications,
    Guid? PrimaryCategoryId,
    Guid? BrandId,
    IReadOnlyCollection<Guid>? CategoryIds,
    SeoInput? Seo,
    string? Status = null);

public sealed record AdminProductListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Status = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    string Sort = "updated_desc");

public sealed record AdminProductListItem(
    Guid Id,
    string? Sku,
    string Slug,
    string Name,
    string Summary,
    AdminProductThumbnailDto? CoverImage,
    string Status,
    Guid? PrimaryCategoryId,
    string? PrimaryCategoryName,
    Guid? BrandId,
    string? BrandName,
    DateTimeOffset? PublishedAt,
    DateTimeOffset UpdatedAtUtc);

public sealed record AdminProductThumbnailDto(
    Guid AssetId,
    string? PublicUrl,
    int Width,
    int Height,
    string AltText);

public sealed record AdminProductDetail(
    Guid Id,
    string? Sku,
    string Slug,
    string Name,
    string Summary,
    JsonElement Description,
    string DescriptionHtml,
    string Status,
    Guid? PrimaryCategoryId,
    Guid? BrandId,
    string ProductType,
    string? MaterialText,
    int? CapacityMl,
    JsonElement Specifications,
    DateTimeOffset? PublishedAt,
    SeoDto Seo,
    IReadOnlyList<AdminCategorySummary> Categories,
    IReadOnlyList<AdminProductMediaDto> Media,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record AdminCategorySummary(Guid Id, string Slug, string Name, string Status);

public sealed record AdminProductMediaDto(
    Guid Id,
    Guid AssetId,
    string StorageKey,
    string? PublicUrl,
    string MimeType,
    int Width,
    int Height,
    string AltText,
    int SortOrder,
    bool IsCover);

public sealed record CreateCategoryCommand(
    Guid? ParentId,
    string Slug,
    string Name,
    string DescriptionHtml,
    int SortOrder,
    SeoInput? Seo);

public sealed record UpdateCategoryCommand(
    Guid? ParentId,
    string Slug,
    string Name,
    string DescriptionHtml,
    int SortOrder,
    SeoInput? Seo,
    string? Status = null);

public sealed record AdminCategoryNode(
    Guid Id,
    Guid? ParentId,
    string Slug,
    string Name,
    int SortOrder,
    string Status,
    IReadOnlyList<AdminCategoryNode> Children);

public sealed record AdminCategoryDetail(
    Guid Id,
    Guid? ParentId,
    string Slug,
    string Name,
    string DescriptionHtml,
    int SortOrder,
    string Status,
    SeoDto Seo,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateBrandCommand(string Name, string Slug, string? Origin);

public sealed record UpdateBrandCommand(string Name, string Slug, string? Origin);

public sealed record AdminBrandDto(Guid Id, string Name, string Slug, string? Origin);

public sealed record AttachProductMediaCommand(
    Guid AssetId,
    string AltText,
    int SortOrder,
    bool IsCover = false);

public sealed record UpdateProductMediaCommand(string AltText, int SortOrder);

public sealed record PublicProductListQuery(
    int Page = 1,
    int PageSize = 12,
    string? Search = null,
    string? Category = null,
    string Sort = "latest");

public sealed record PublicProductListItem(
    Guid Id,
    string Slug,
    string Name,
    string Summary,
    PublicMediaDto? CoverImage,
    PublicCategorySummary? PrimaryCategory,
    PublicBrandDto? Brand);

public sealed record PublicProductDetail(
    Guid Id,
    string? Sku,
    string Slug,
    string Name,
    string Summary,
    JsonElement Description,
    string DescriptionHtml,
    string ProductType,
    string? MaterialText,
    int? CapacityMl,
    JsonElement Specifications,
    DateTimeOffset PublishedAt,
    SeoDto Seo,
    PublicCategorySummary PrimaryCategory,
    IReadOnlyList<PublicCategorySummary> Categories,
    PublicBrandDto? Brand,
    IReadOnlyList<PublicMediaDto> Media);

public sealed record PublicMediaDto(
    Guid AssetId,
    string StorageKey,
    string? PublicUrl,
    string MimeType,
    int Width,
    int Height,
    string AltText,
    int SortOrder,
    bool IsCover);

public sealed record PublicCategorySummary(Guid Id, string Slug, string Name);

public sealed record PublicBrandDto(Guid Id, string Slug, string Name, string? Origin);

public sealed record PublicCategoryNode(
    Guid Id,
    string Slug,
    string Name,
    int SortOrder,
    IReadOnlyList<PublicCategoryNode> Children);

public sealed record PublicCategoryDetail(
    Guid Id,
    string Slug,
    string Name,
    string DescriptionHtml,
    SeoDto Seo,
    PagedResult<PublicProductListItem> Products);
