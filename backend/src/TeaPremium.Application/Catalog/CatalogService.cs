using Ganss.Xss;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Common;
using TeaPremium.Domain.Media;
using TeaPremium.Domain.Seo;
using TeaPremium.Application.Seo;

namespace TeaPremium.Application.Catalog;

internal sealed class CatalogService(
    ICatalogDataStore store,
    ISeoRedirectStore redirects,
    HtmlSanitizer htmlSanitizer,
    TimeProvider timeProvider) : ICatalogService
{
    private static readonly string[] AdminSorts =
        ["updated_desc", "created_desc", "name_asc", "name_desc", "status_asc", "published_desc"];

    private static readonly string[] PublicSorts = ["latest", "name_asc", "name_desc"];

    public Task<PagedResult<AdminProductListItem>> GetAdminProductsAsync(
        AdminProductListQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(query.Page, query.PageSize);
        ValidateSort(query.Sort, AdminSorts);
        ValidateOptionalStatus(query.Status, "status");

        return store.QueryAdminProductsAsync(query, cancellationToken);
    }

    public async Task<AdminProductDetail> GetAdminProductAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await store.QueryAdminProductAsync(id, cancellationToken) ??
        throw new CatalogNotFoundException("Product", id);

    public async Task<AdminProductDetail> CreateProductAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalized = NormalizeProduct(command);
        ValidateProduct(normalized);
        await ValidateProductReferencesAsync(normalized, cancellationToken);
        await EnsureProductUniqueAsync(normalized.Slug, normalized.Sku, null, cancellationToken);
        if (await redirects.SourceExistsAsync(SeoRedirect.ContentPath("san-pham", normalized.Slug), cancellationToken))
            throw new CatalogConflictException("Product slug is reserved by a redirect.");

        var product = new Product(
            normalized.Slug,
            normalized.Name,
            normalized.Summary,
            normalized.DescriptionJson,
            normalized.DescriptionHtml,
            normalized.ProductType,
            normalized.SpecificationsJson,
            ToSeoMetadata(normalized.Seo),
            normalized.Sku,
            normalized.PrimaryCategoryId,
            normalized.BrandId,
            normalized.MaterialText,
            normalized.CapacityMl);
        product.SetSellable(command.IsSellable);

        foreach (var categoryId in normalized.CategoryIds)
        {
            product.ProductCategories.Add(new ProductCategory(product.Id, categoryId));
        }

        await store.ExecuteInTransactionAsync(
            async token =>
            {
                await redirects.EnsurePathAvailableAsync(
                    SeoRedirect.ContentPath("san-pham", normalized.Slug), token);
                store.AddProduct(product);
                await store.SaveChangesAsync(token);
            },
            cancellationToken);

        return await GetAdminProductAsync(product.Id, cancellationToken);
    }

    public async Task<AdminProductDetail> UpdateProductAsync(
        Guid id,
        UpdateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await store.LoadProductAsync(
                id,
                includeCategories: true,
                includeMedia: true,
                cancellationToken) ??
            throw new CatalogNotFoundException("Product", id);

        var normalized = NormalizeProduct(command);
        ValidateProduct(normalized);

        var previousSlug = product.Slug;
        var slugChanged = !string.Equals(previousSlug, normalized.Slug, StringComparison.Ordinal);
        var hasBeenPublished = product.PublishedAt.HasValue;
        if (slugChanged && hasBeenPublished &&
            SeoRedirect.CanonicalPointsToSource(normalized.Seo.CanonicalOverride,
                SeoRedirect.ContentPath("san-pham", previousSlug)))
            throw new CatalogValidationException(new Dictionary<string, string[]>
            { ["seo.canonicalOverride"] = ["Clear the old canonical URL before changing a Published slug."] });
        if (slugChanged && await redirects.SourceExistsAsync(
                SeoRedirect.ContentPath("san-pham", normalized.Slug), cancellationToken))
            throw new CatalogConflictException("Product slug is reserved by a redirect.");

        await ValidateProductReferencesAsync(normalized, cancellationToken);
        await EnsureProductUniqueAsync(normalized.Slug, normalized.Sku, id, cancellationToken);

        var requestedStatus = ParseRequestedStatus(command.Status, product.Status);
        ValidateProductStatusTransition(product.Status, requestedStatus);

        if (product.Status == PublicationStatus.Published)
        {
            await ValidatePublishRulesAsync(
                normalized,
                product.Media,
                cancellationToken);
        }

        await store.ExecuteInTransactionAsync(
            async token =>
            {
                if (slugChanged && hasBeenPublished)
                    await redirects.RecordSlugChangeAsync(
                        SeoRedirect.ContentPath("san-pham", previousSlug),
                        SeoRedirect.ContentPath("san-pham", normalized.Slug), token);
                else if (slugChanged)
                    await redirects.EnsurePathAvailableAsync(
                        SeoRedirect.ContentPath("san-pham", normalized.Slug), token);
                product.Update(
                    normalized.Slug,
                    normalized.Name,
                    normalized.Summary,
                    normalized.DescriptionJson,
                    normalized.DescriptionHtml,
                    normalized.ProductType,
                    normalized.SpecificationsJson,
                    ToSeoMetadata(normalized.Seo),
                    normalized.CategoryIds,
                    normalized.Sku,
                    normalized.PrimaryCategoryId,
                    normalized.BrandId,
                    normalized.MaterialText,
                    normalized.CapacityMl);

                if (requestedStatus == PublicationStatus.InReview &&
                    product.Status == PublicationStatus.Draft)
                {
                    product.MoveToInReview();
                }
                else if (requestedStatus == PublicationStatus.Draft && product.Status != PublicationStatus.Draft)
                    product.ReturnToDraft();
                if (command.IsSellable.HasValue) product.SetSellable(command.IsSellable.Value);

                await store.SaveChangesAsync(token);
            },
            cancellationToken);

        return await GetAdminProductAsync(id, cancellationToken);
    }

    public async Task<AdminProductDetail> PublishProductAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await store.LoadProductAsync(
                id,
                includeCategories: true,
                includeMedia: true,
                cancellationToken) ??
            throw new CatalogNotFoundException("Product", id);

        if (product.Status != PublicationStatus.InReview)
        {
            throw new CatalogConflictException("Only an InReview product can be published.");
        }

        var normalized = ProductInput.From(product);
        ValidateProduct(normalized);
        await EnsureProductUniqueAsync(product.Slug, product.Sku, id, cancellationToken);
        await ValidatePublishRulesAsync(normalized, product.Media, cancellationToken);

        await store.ExecuteInTransactionAsync(
            async token =>
            {
                product.Publish(timeProvider.GetUtcNow());
                await store.SaveChangesAsync(token);
            },
            cancellationToken);

        return await GetAdminProductAsync(id, cancellationToken);
    }

    public async Task<AdminProductDetail> ArchiveProductAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await store.LoadProductAsync(
                id,
                includeCategories: false,
                includeMedia: false,
                cancellationToken) ??
            throw new CatalogNotFoundException("Product", id);

        if (product.Status != PublicationStatus.Published)
        {
            throw new CatalogConflictException("Only a Published product can be archived.");
        }

        await store.ExecuteInTransactionAsync(
            async token =>
            {
                product.Archive(timeProvider.GetUtcNow());
                await store.SaveChangesAsync(token);
            },
            cancellationToken);

        return await GetAdminProductAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<AdminCategoryNode>> GetAdminCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        store.QueryAdminCategoriesAsync(cancellationToken);

    public async Task<AdminCategoryDetail> GetAdminCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await store.QueryAdminCategoryAsync(id, cancellationToken) ??
        throw new CatalogNotFoundException("Category", id);

    public async Task<AdminCategoryDetail> CreateCategoryAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalized = NormalizeCategory(command);
        ValidateCategory(normalized);
        await ValidateCategoryParentAsync(null, normalized.ParentId, cancellationToken);
        await ValidateSeoAssetAsync(normalized.Seo, cancellationToken);

        if (await store.CategorySlugExistsAsync(normalized.Slug, null, cancellationToken))
        {
            throw new CatalogConflictException($"Category slug '{normalized.Slug}' is already in use.");
        }
        if (await redirects.SourceExistsAsync(SeoRedirect.ContentPath("danh-muc", normalized.Slug), cancellationToken))
            throw new CatalogConflictException("Category slug is reserved by a redirect.");

        var category = new Category(
            normalized.Slug,
            normalized.Name,
            normalized.DescriptionHtml,
            ToSeoMetadata(normalized.Seo),
            normalized.ParentId,
            normalized.SortOrder);

        await store.ExecuteInTransactionAsync(
            async token =>
            {
                await redirects.EnsurePathAvailableAsync(
                    SeoRedirect.ContentPath("danh-muc", normalized.Slug), token);
                store.AddCategory(category);
                await store.SaveChangesAsync(token);
            },
            cancellationToken);

        return await GetAdminCategoryAsync(category.Id, cancellationToken);
    }

    public async Task<AdminCategoryDetail> UpdateCategoryAsync(
        Guid id,
        UpdateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = await store.LoadCategoryAsync(id, cancellationToken) ??
            throw new CatalogNotFoundException("Category", id);
        var normalized = NormalizeCategory(command);
        ValidateCategory(normalized);

        var previousSlug = category.Slug;
        var slugChanged = !string.Equals(previousSlug, normalized.Slug, StringComparison.Ordinal);
        if (slugChanged && category.Status == PublicationStatus.Published &&
            SeoRedirect.CanonicalPointsToSource(normalized.Seo.CanonicalOverride,
                SeoRedirect.ContentPath("danh-muc", previousSlug)))
            throw new CatalogValidationException(new Dictionary<string, string[]>
            { ["seo.canonicalOverride"] = ["Clear the old canonical URL before changing a Published slug."] });
        if (slugChanged && await redirects.SourceExistsAsync(
                SeoRedirect.ContentPath("danh-muc", normalized.Slug), cancellationToken))
            throw new CatalogConflictException("Category slug is reserved by a redirect.");

        await ValidateCategoryParentAsync(id, normalized.ParentId, cancellationToken);
        await ValidateSeoAssetAsync(normalized.Seo, cancellationToken);

        if (await store.CategorySlugExistsAsync(normalized.Slug, id, cancellationToken))
        {
            throw new CatalogConflictException($"Category slug '{normalized.Slug}' is already in use.");
        }

        var requestedStatus = ParseRequestedStatus(command.Status, category.Status);
        ValidateCategoryStatusTransition(category.Status, requestedStatus);

        if (requestedStatus == PublicationStatus.Published ||
            category.Status == PublicationStatus.Published)
        {
            await ValidateCategoryPublishRulesAsync(normalized, cancellationToken);
        }

        if (requestedStatus == PublicationStatus.Archived &&
            await store.CategoryIsUsedByPublishedProductsAsync(id, cancellationToken))
        {
            throw new CatalogValidationException(new Dictionary<string, string[]>
            {
                ["status"] = ["A category used by Published products cannot be archived."],
            });
        }

        await store.ExecuteInTransactionAsync(
            async token =>
            {
                if (slugChanged && category.Status == PublicationStatus.Published)
                    await redirects.RecordSlugChangeAsync(
                        SeoRedirect.ContentPath("danh-muc", previousSlug),
                        SeoRedirect.ContentPath("danh-muc", normalized.Slug), token);
                else if (slugChanged)
                    await redirects.EnsurePathAvailableAsync(
                        SeoRedirect.ContentPath("danh-muc", normalized.Slug), token);
                category.Update(
                    normalized.Slug,
                    normalized.Name,
                    normalized.DescriptionHtml,
                    ToSeoMetadata(normalized.Seo),
                    normalized.ParentId,
                    normalized.SortOrder);

                if (requestedStatus != category.Status)
                {
                    ApplyCategoryStatus(category, requestedStatus);
                }

                await store.SaveChangesAsync(token);
            },
            cancellationToken);

        return await GetAdminCategoryAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<AdminBrandDto>> GetAdminBrandsAsync(
        CancellationToken cancellationToken = default) =>
        store.QueryAdminBrandsAsync(cancellationToken);

    public async Task<AdminBrandDto> GetAdminBrandAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await store.QueryAdminBrandAsync(id, cancellationToken) ??
        throw new CatalogNotFoundException("Brand", id);

    public async Task<AdminBrandDto> CreateBrandAsync(
        CreateBrandCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalized = NormalizeBrand(command.Name, command.Slug, command.Origin);
        ValidateBrand(normalized);

        if (await store.BrandSlugExistsAsync(normalized.Slug, null, cancellationToken))
        {
            throw new CatalogConflictException($"Brand slug '{normalized.Slug}' is already in use.");
        }

        var brand = new Brand(normalized.Name, normalized.Slug, normalized.Origin);
        await store.ExecuteInTransactionAsync(
            async token =>
            {
                store.AddBrand(brand);
                await store.SaveChangesAsync(token);
            },
            cancellationToken);

        return await GetAdminBrandAsync(brand.Id, cancellationToken);
    }

    public async Task<AdminBrandDto> UpdateBrandAsync(
        Guid id,
        UpdateBrandCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var brand = await store.LoadBrandAsync(id, cancellationToken) ??
            throw new CatalogNotFoundException("Brand", id);
        var normalized = NormalizeBrand(command.Name, command.Slug, command.Origin);
        ValidateBrand(normalized);

        if (await store.BrandSlugExistsAsync(normalized.Slug, id, cancellationToken))
        {
            throw new CatalogConflictException($"Brand slug '{normalized.Slug}' is already in use.");
        }

        brand.Update(normalized.Name, normalized.Slug, normalized.Origin);
        await store.SaveChangesAsync(cancellationToken);
        return await GetAdminBrandAsync(id, cancellationToken);
    }

    public async Task<AdminProductMediaDto> AttachProductMediaAsync(
        Guid productId,
        AttachProductMediaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await LoadProductWithMediaAsync(productId, cancellationToken);
        ValidateMedia(command.AltText, command.SortOrder, command.IsCover && product.Status == PublicationStatus.Published);

        if (!await store.MediaAssetExistsAsync(command.AssetId, cancellationToken))
        {
            throw new CatalogValidationException(new Dictionary<string, string[]>
            {
                ["assetId"] = ["The selected MediaAsset does not exist."],
            });
        }

        if (await store.ProductHasAssetAsync(productId, command.AssetId, cancellationToken))
        {
            throw new CatalogConflictException("This MediaAsset is already attached to the product.");
        }

        var media = new ProductMedia(
            productId,
            command.AssetId,
            (command.AltText ?? string.Empty).Trim(),
            command.SortOrder,
            command.IsCover);

        await store.ExecuteInTransactionAsync(
            async token =>
            {
                if (command.IsCover)
                {
                    foreach (var currentCover in product.Media.Where(item => item.IsCover))
                    {
                        currentCover.SetCover(false);
                    }

                    await store.SaveChangesAsync(token);
                }

                store.AddProductMedia(media);
                product.Touch();
                await store.SaveChangesAsync(token);
            },
            cancellationToken);

        var updated = await store.QueryAdminProductAsync(productId, cancellationToken) ??
            throw new CatalogNotFoundException("Product", productId);
        return updated.Media.Single(item => item.Id == media.Id);
    }

    public async Task<AdminProductMediaDto> UpdateProductMediaAsync(
        Guid productId,
        Guid mediaId,
        UpdateProductMediaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await LoadProductWithMediaAsync(productId, cancellationToken);
        var media = product.Media.SingleOrDefault(item => item.Id == mediaId) ??
            throw new CatalogNotFoundException("Product media", mediaId);

        ValidateMedia(
            command.AltText,
            command.SortOrder,
            media.IsCover && product.Status == PublicationStatus.Published);

        media.Update((command.AltText ?? string.Empty).Trim(), command.SortOrder);
        product.Touch();
        await store.SaveChangesAsync(cancellationToken);

        var updated = await store.QueryAdminProductAsync(productId, cancellationToken) ??
            throw new CatalogNotFoundException("Product", productId);
        return updated.Media.Single(item => item.Id == mediaId);
    }

    public async Task RemoveProductMediaAsync(
        Guid productId,
        Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        var product = await LoadProductWithMediaAsync(productId, cancellationToken);
        var media = product.Media.SingleOrDefault(item => item.Id == mediaId) ??
            throw new CatalogNotFoundException("Product media", mediaId);

        if (product.Status == PublicationStatus.Published && media.IsCover)
        {
            throw new CatalogValidationException(new Dictionary<string, string[]>
            {
                ["mediaId"] = ["The cover of a Published product cannot be removed. Select another cover first."],
            });
        }

        store.RemoveProductMedia(media);
        product.Touch();
        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminProductMediaDto> SetProductCoverAsync(
        Guid productId,
        Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        var product = await LoadProductWithMediaAsync(productId, cancellationToken);
        var media = product.Media.SingleOrDefault(item => item.Id == mediaId) ??
            throw new CatalogNotFoundException("Product media", mediaId);

        if (product.Status == PublicationStatus.Published && string.IsNullOrWhiteSpace(media.AltText))
        {
            throw new CatalogValidationException(new Dictionary<string, string[]>
            {
                ["altText"] = ["The cover image of a Published product requires ALT text."],
            });
        }

        await store.ExecuteInTransactionAsync(
            async token =>
            {
                foreach (var currentCover in product.Media.Where(item => item.IsCover))
                {
                    currentCover.SetCover(false);
                }

                await store.SaveChangesAsync(token);
                media.SetCover(true);
                product.Touch();
                await store.SaveChangesAsync(token);
            },
            cancellationToken);

        var updated = await store.QueryAdminProductAsync(productId, cancellationToken) ??
            throw new CatalogNotFoundException("Product", productId);
        return updated.Media.Single(item => item.Id == mediaId);
    }

    public Task<PagedResult<PublicProductListItem>> GetPublicProductsAsync(
        PublicProductListQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(query.Page, query.PageSize);
        ValidateSort(query.Sort, PublicSorts);
        return store.QueryPublicProductsAsync(query, cancellationToken);
    }

    public async Task<PublicProductDetail> GetPublicProductAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (!CatalogRules.IsValidSlug(slug))
        {
            throw new CatalogNotFoundException("Published product", slug);
        }

        return await store.QueryPublicProductAsync(slug, cancellationToken) ??
            throw new CatalogNotFoundException("Published product", slug);
    }

    public Task<IReadOnlyList<PublicCategoryNode>> GetPublicCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        store.QueryPublicCategoriesAsync(cancellationToken);

    public async Task<PublicCategoryDetail> GetPublicCategoryAsync(
        string slug,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(page, pageSize);
        if (!CatalogRules.IsValidSlug(slug))
        {
            throw new CatalogNotFoundException("Published category", slug);
        }

        return await store.QueryPublicCategoryAsync(slug, page, pageSize, cancellationToken) ??
            throw new CatalogNotFoundException("Published category", slug);
    }

    private ProductInput NormalizeProduct(CreateProductCommand command) =>
        ProductInput.Create(
            command.Sku,
            command.Slug,
            command.Name,
            command.Summary,
            command.Description,
            htmlSanitizer.Sanitize(command.DescriptionHtml ?? string.Empty),
            command.ProductType,
            command.MaterialText,
            command.CapacityMl,
            command.Specifications,
            command.PrimaryCategoryId,
            command.BrandId,
            command.CategoryIds,
            command.Seo);

    private ProductInput NormalizeProduct(UpdateProductCommand command) =>
        ProductInput.Create(
            command.Sku,
            command.Slug,
            command.Name,
            command.Summary,
            command.Description,
            htmlSanitizer.Sanitize(command.DescriptionHtml ?? string.Empty),
            command.ProductType,
            command.MaterialText,
            command.CapacityMl,
            command.Specifications,
            command.PrimaryCategoryId,
            command.BrandId,
            command.CategoryIds,
            command.Seo);

    private static void ValidateProduct(ProductInput input)
    {
        var validation = new CatalogValidationBag();

        if (!CatalogRules.IsValidSlug(input.Slug))
        {
            validation.Add("slug", "Use a lowercase ASCII kebab-case slug with at most 200 characters.");
        }

        validation.Require("name", input.Name, 300);
        validation.Require("productType", input.ProductType, 100);
        validation.OptionalMaximum("sku", input.Sku, 100);
        validation.OptionalMaximum("summary", input.Summary, 1000);
        validation.OptionalMaximum("materialText", input.MaterialText, 300);

        if (!CatalogRules.IsJsonObject(input.Description))
        {
            validation.Add("description", "Description must be a JSON object.");
        }

        if (!CatalogRules.IsJsonObject(input.Specifications))
        {
            validation.Add("specifications", "Specifications must be a JSON object.");
        }

        if (input.CapacityMl is <= 0)
        {
            validation.Add("capacityMl", "Capacity must be greater than zero when specified.");
        }

        if (input.PrimaryCategoryId is not null &&
            !input.CategoryIds.Contains(input.PrimaryCategoryId.Value))
        {
            validation.Add(
                "primaryCategoryId",
                "The primary category must also be present in categoryIds.");
        }

        ValidateSeo(input.Seo, validation);
        validation.ThrowIfInvalid();
    }

    private async Task ValidateProductReferencesAsync(
        ProductInput input,
        CancellationToken cancellationToken)
    {
        var validation = new CatalogValidationBag();
        var existingCategories = await store.GetExistingCategoryIdsAsync(
            input.CategoryIds,
            cancellationToken);

        foreach (var missingId in input.CategoryIds.Except(existingCategories))
        {
            validation.Add("categoryIds", $"Category '{missingId}' does not exist.");
        }

        if (input.BrandId is { } brandId &&
            !await store.BrandExistsAsync(brandId, cancellationToken))
        {
            validation.Add("brandId", "The selected brand does not exist.");
        }

        if (input.Seo.OgImageAssetId is { } assetId &&
            !await store.MediaAssetExistsAsync(assetId, cancellationToken))
        {
            validation.Add("seo.ogImageAssetId", "The selected Open Graph image does not exist.");
        }

        validation.ThrowIfInvalid();
    }

    private async Task EnsureProductUniqueAsync(
        string slug,
        string? sku,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        if (await store.ProductSlugExistsAsync(slug, excludingId, cancellationToken))
        {
            throw new CatalogConflictException($"Product slug '{slug}' is already in use.");
        }

        if (sku is not null &&
            await store.ProductSkuExistsAsync(sku, excludingId, cancellationToken))
        {
            throw new CatalogConflictException($"Product SKU '{sku}' is already in use.");
        }
    }

    private async Task ValidatePublishRulesAsync(
        ProductInput input,
        IEnumerable<ProductMedia> media,
        CancellationToken cancellationToken)
    {
        var validation = new CatalogValidationBag();
        validation.Require("name", input.Name, 300);
        validation.Require("summary", input.Summary, 1000);
        validation.Require("descriptionHtml", input.DescriptionHtml, int.MaxValue);

        if (input.Description.ValueKind != System.Text.Json.JsonValueKind.Object ||
            !input.Description.EnumerateObject().Any())
        {
            validation.Add("description", "Published products require a non-empty description object.");
        }

        if (input.PrimaryCategoryId is null)
        {
            validation.Add("primaryCategoryId", "Published products require a primary category.");
        }
        else if (!input.CategoryIds.Contains(input.PrimaryCategoryId.Value))
        {
            validation.Add("primaryCategoryId", "The primary category must be attached to the product.");
        }
        else if (!await store.CategoryIsPublishedAsync(input.PrimaryCategoryId.Value, cancellationToken))
        {
            validation.Add("primaryCategoryId", "The primary category must be Published first.");
        }

        var cover = media.SingleOrDefault(item => item.IsCover);
        if (cover is null)
        {
            validation.Add("media", "Published products require a cover image.");
        }
        else if (string.IsNullOrWhiteSpace(cover.AltText))
        {
            validation.Add("media", "The cover image requires ALT text.");
        }

        validation.Require("seo.seoTitle", input.Seo.SeoTitle, 200);
        validation.Require("seo.metaDescription", input.Seo.MetaDescription, 500);
        validation.ThrowIfInvalid();
    }

    private static void ValidateProductStatusTransition(
        PublicationStatus current,
        PublicationStatus requested)
    {
        if (requested == current ||
            current == PublicationStatus.Draft && requested == PublicationStatus.InReview ||
            current is PublicationStatus.InReview or PublicationStatus.Archived && requested == PublicationStatus.Draft)
        {
            return;
        }

        throw new CatalogConflictException(
            "PATCH supports Draft to InReview, or InReview/Archived back to Draft. Use publish/archive for public transitions.");
    }

    private CategoryInput NormalizeCategory(CreateCategoryCommand command) =>
        CategoryInput.Create(
            command.ParentId,
            command.Slug,
            command.Name,
            htmlSanitizer.Sanitize(command.DescriptionHtml ?? string.Empty),
            command.SortOrder,
            command.Seo);

    private CategoryInput NormalizeCategory(UpdateCategoryCommand command) =>
        CategoryInput.Create(
            command.ParentId,
            command.Slug,
            command.Name,
            htmlSanitizer.Sanitize(command.DescriptionHtml ?? string.Empty),
            command.SortOrder,
            command.Seo);

    private static void ValidateCategory(CategoryInput input)
    {
        var validation = new CatalogValidationBag();
        if (!CatalogRules.IsValidSlug(input.Slug))
        {
            validation.Add("slug", "Use a lowercase ASCII kebab-case slug with at most 200 characters.");
        }

        validation.Require("name", input.Name, 300);
        if (input.SortOrder < 0)
        {
            validation.Add("sortOrder", "Sort order cannot be negative.");
        }

        ValidateSeo(input.Seo, validation);
        validation.ThrowIfInvalid();
    }

    private async Task ValidateCategoryParentAsync(
        Guid? categoryId,
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        if (parentId is null)
        {
            return;
        }

        var validation = new CatalogValidationBag();
        if (categoryId == parentId)
        {
            validation.Add("parentId", "A category cannot be its own parent.");
        }
        else
        {
            var existing = await store.GetExistingCategoryIdsAsync([parentId.Value], cancellationToken);
            if (!existing.Contains(parentId.Value))
            {
                validation.Add("parentId", "The selected parent category does not exist.");
            }
            else if (categoryId is { } id &&
                     await store.CategoryWouldCreateCycleAsync(id, parentId.Value, cancellationToken))
            {
                validation.Add("parentId", "The selected parent would create a circular category hierarchy.");
            }
        }

        validation.ThrowIfInvalid();
    }

    private async Task ValidateCategoryPublishRulesAsync(
        CategoryInput input,
        CancellationToken cancellationToken)
    {
        var validation = new CatalogValidationBag();
        validation.Require("descriptionHtml", input.DescriptionHtml, int.MaxValue);
        validation.Require("seo.seoTitle", input.Seo.SeoTitle, 200);
        validation.Require("seo.metaDescription", input.Seo.MetaDescription, 500);

        if (input.ParentId is { } parentId &&
            !await store.CategoryIsPublishedAsync(parentId, cancellationToken))
        {
            validation.Add("parentId", "A parent category must be Published first.");
        }

        validation.ThrowIfInvalid();
    }

    private static void ValidateCategoryStatusTransition(
        PublicationStatus current,
        PublicationStatus requested)
    {
        var valid = requested == current ||
                    current == PublicationStatus.Draft && requested == PublicationStatus.InReview ||
                    current == PublicationStatus.InReview && requested == PublicationStatus.Published ||
                    current == PublicationStatus.Published && requested == PublicationStatus.Archived;

        if (!valid)
        {
            throw new CatalogConflictException(
                "Category status must follow Draft -> InReview -> Published -> Archived.");
        }
    }

    private static void ApplyCategoryStatus(Category category, PublicationStatus status)
    {
        if (status == PublicationStatus.InReview)
        {
            category.MoveToInReview();
        }
        else if (status == PublicationStatus.Published)
        {
            category.Publish();
        }
        else if (status == PublicationStatus.Archived)
        {
            category.Archive();
        }
    }

    private async Task ValidateSeoAssetAsync(SeoInput seo, CancellationToken cancellationToken)
    {
        if (seo.OgImageAssetId is { } assetId &&
            !await store.MediaAssetExistsAsync(assetId, cancellationToken))
        {
            throw new CatalogValidationException(new Dictionary<string, string[]>
            {
                ["seo.ogImageAssetId"] = ["The selected Open Graph image does not exist."],
            });
        }
    }

    private static void ValidateSeo(SeoInput seo, CatalogValidationBag validation)
    {
        validation.OptionalMaximum("seo.seoTitle", seo.SeoTitle, 200);
        validation.OptionalMaximum("seo.metaDescription", seo.MetaDescription, 500);
        validation.OptionalMaximum("seo.canonicalOverride", seo.CanonicalOverride, 2048);
        validation.OptionalMaximum("seo.ogTitle", seo.OgTitle, 200);
        validation.OptionalMaximum("seo.ogDescription", seo.OgDescription, 500);

        if (!string.IsNullOrWhiteSpace(seo.CanonicalOverride) &&
            (!Uri.TryCreate(seo.CanonicalOverride, UriKind.Absolute, out var canonical) ||
             canonical.Scheme is not ("http" or "https")))
        {
            validation.Add("seo.canonicalOverride", "Canonical override must be an absolute HTTP(S) URL.");
        }
    }

    private static BrandInput NormalizeBrand(string name, string slug, string? origin) =>
        new(name?.Trim() ?? string.Empty, slug?.Trim() ?? string.Empty, NullIfWhiteSpace(origin));

    private static void ValidateBrand(BrandInput input)
    {
        var validation = new CatalogValidationBag();
        validation.Require("name", input.Name, 300);
        if (!CatalogRules.IsValidSlug(input.Slug))
        {
            validation.Add("slug", "Use a lowercase ASCII kebab-case slug with at most 200 characters.");
        }

        validation.OptionalMaximum("origin", input.Origin, 300);
        validation.ThrowIfInvalid();
    }

    private static void ValidateMedia(
        string? altText,
        int sortOrder,
        bool requireAltText)
    {
        var validation = new CatalogValidationBag();
        validation.OptionalMaximum("altText", altText, 500);
        if (requireAltText && string.IsNullOrWhiteSpace(altText))
        {
            validation.Add("altText", "The cover image of a Published product requires ALT text.");
        }

        if (sortOrder < 0)
        {
            validation.Add("sortOrder", "Sort order cannot be negative.");
        }

        validation.ThrowIfInvalid();
    }

    private async Task<Product> LoadProductWithMediaAsync(
        Guid productId,
        CancellationToken cancellationToken) =>
        await store.LoadProductAsync(
            productId,
            includeCategories: false,
            includeMedia: true,
            cancellationToken) ??
        throw new CatalogNotFoundException("Product", productId);

    private static void ValidatePage(int page, int pageSize)
    {
        var validation = new CatalogValidationBag();
        if (page < 1)
        {
            validation.Add("page", "Page must be greater than or equal to 1.");
        }

        if (pageSize is < 1 or > CatalogRules.MaximumPageSize)
        {
            validation.Add(
                "pageSize",
                $"Page size must be between 1 and {CatalogRules.MaximumPageSize}.");
        }

        validation.ThrowIfInvalid();
    }

    private static void ValidateSort(string sort, IReadOnlyCollection<string> allowed)
    {
        if (allowed.Contains(sort, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        throw new CatalogValidationException(new Dictionary<string, string[]>
        {
            ["sort"] = [$"Sort must be one of: {string.Join(", ", allowed)}."],
        });
    }

    private static void ValidateOptionalStatus(string? status, string field)
    {
        if (status is null || CatalogRules.TryParseStatus(status, out _))
        {
            return;
        }

        throw new CatalogValidationException(new Dictionary<string, string[]>
        {
            [field] = ["Status must be Draft, InReview, Published, or Archived."],
        });
    }

    private static PublicationStatus ParseRequestedStatus(
        string? value,
        PublicationStatus current)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return current;
        }

        if (CatalogRules.TryParseStatus(value, out var parsed))
        {
            return parsed;
        }

        throw new CatalogValidationException(new Dictionary<string, string[]>
        {
            ["status"] = ["Status must be Draft, InReview, Published, or Archived."],
        });
    }

    private static SeoMetadata ToSeoMetadata(SeoInput input) =>
        new(
            NullIfWhiteSpace(input.SeoTitle),
            NullIfWhiteSpace(input.MetaDescription),
            NullIfWhiteSpace(input.CanonicalOverride),
            input.RobotsIndex,
            NullIfWhiteSpace(input.OgTitle),
            NullIfWhiteSpace(input.OgDescription),
            input.OgImageAssetId);

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record ProductInput(
        string? Sku,
        string Slug,
        string Name,
        string Summary,
        System.Text.Json.JsonElement Description,
        string DescriptionJson,
        string DescriptionHtml,
        string ProductType,
        string? MaterialText,
        int? CapacityMl,
        System.Text.Json.JsonElement Specifications,
        string SpecificationsJson,
        Guid? PrimaryCategoryId,
        Guid? BrandId,
        IReadOnlyCollection<Guid> CategoryIds,
        SeoInput Seo)
    {
        public static ProductInput Create(
            string? sku,
            string slug,
            string name,
            string summary,
            System.Text.Json.JsonElement description,
            string descriptionHtml,
            string productType,
            string? materialText,
            int? capacityMl,
            System.Text.Json.JsonElement specifications,
            Guid? primaryCategoryId,
            Guid? brandId,
            IReadOnlyCollection<Guid>? categoryIds,
            SeoInput? seo) =>
            new(
                NullIfWhiteSpace(sku),
                slug?.Trim() ?? string.Empty,
                name?.Trim() ?? string.Empty,
                summary?.Trim() ?? string.Empty,
                description,
                description.ValueKind == System.Text.Json.JsonValueKind.Undefined
                    ? string.Empty
                    : description.GetRawText(),
                descriptionHtml.Trim(),
                productType?.Trim() ?? string.Empty,
                NullIfWhiteSpace(materialText),
                capacityMl,
                specifications,
                specifications.ValueKind == System.Text.Json.JsonValueKind.Undefined
                    ? string.Empty
                    : specifications.GetRawText(),
                primaryCategoryId,
                brandId,
                (categoryIds ?? []).Distinct().ToArray(),
                seo ?? new SeoInput(null, null));

        public static ProductInput From(Product product) =>
            Create(
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
                product.PrimaryCategoryId,
                product.BrandId,
                product.ProductCategories.Select(link => link.CategoryId).ToArray(),
                new SeoInput(
                    product.Seo.SeoTitle,
                    product.Seo.MetaDescription,
                    product.Seo.CanonicalOverride,
                    product.Seo.RobotsIndex,
                    product.Seo.OgTitle,
                    product.Seo.OgDescription,
                    product.Seo.OgImageAssetId));

        private static System.Text.Json.JsonElement ParseJson(string json) =>
            System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
    }

    private sealed record CategoryInput(
        Guid? ParentId,
        string Slug,
        string Name,
        string DescriptionHtml,
        int SortOrder,
        SeoInput Seo)
    {
        public static CategoryInput Create(
            Guid? parentId,
            string slug,
            string name,
            string descriptionHtml,
            int sortOrder,
            SeoInput? seo) =>
            new(
                parentId,
                slug?.Trim() ?? string.Empty,
                name?.Trim() ?? string.Empty,
                descriptionHtml.Trim(),
                sortOrder,
                seo ?? new SeoInput(null, null));
    }

    private sealed record BrandInput(string Name, string Slug, string? Origin);
}
