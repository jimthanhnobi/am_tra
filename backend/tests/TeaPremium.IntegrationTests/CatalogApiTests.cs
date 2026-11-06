using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeaPremium.Api.Contracts.Authentication;
using TeaPremium.Application.Catalog;
using TeaPremium.Application.Security;
using TeaPremium.Application.Seo;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Media;
using TeaPremium.Infrastructure.Identity;
using TeaPremium.Infrastructure.Persistence;

namespace TeaPremium.IntegrationTests;

[Collection(PostgreSqlTestGroup.Name)]
public sealed class CatalogApiTests : IAsyncLifetime, IDisposable
{
    private const string Password = "Integration1!Password";

    private readonly string _adminEmail = $"catalog-admin-{Guid.NewGuid():N}@integration.test";
    private readonly string _editorEmail = $"catalog-editor-{Guid.NewGuid():N}@integration.test";
    private readonly string _readerEmail = $"catalog-reader-{Guid.NewGuid():N}@integration.test";
    private CatalogApiFactory _factory = null!;

    public CatalogApiTests(PostgreSqlFixture database)
    {
        ArgumentNullException.ThrowIfNull(database);
    }

    public async Task InitializeAsync()
    {
        _factory = new CatalogApiFactory(PostgreSqlFixture.GetRequiredConnectionString());
        await CreateUserAsync(_adminEmail, AdminRoles.Admin);
        await CreateUserAsync(_editorEmail, AdminRoles.Editor);
        await CreateUserAsync(_readerEmail, permission: Permissions.CatalogRead);
    }

    public async Task DisposeAsync()
    {
        await DeleteUserIfPresentAsync(_adminEmail);
        await DeleteUserIfPresentAsync(_editorEmail);
        await DeleteUserIfPresentAsync(_readerEmail);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task CreateProductPersistsRelationshipsAndRejectsDuplicateIdentifiers()
    {
        using var client = await CreateAuthenticatedClientAsync(_adminEmail);
        var suffix = Guid.NewGuid().ToString("N");
        var category = await CreateCategoryAsync(client, $"category-{suffix}");

        var product = await CreateProductAsync(
            client,
            $"product-{suffix}",
            $"SKU-{suffix}",
            category.Id);

        Assert.Equal(category.Id, product.PrimaryCategoryId);
        Assert.Contains(product.Categories, item => item.Id == category.Id);
        Assert.Equal("Draft", product.Status);

        var secondProduct = await CreateProductAsync(
            client,
            $"product-second-{suffix}",
            $"SKU-SECOND-{suffix}",
            category.Id);
        var firstPage = await client.GetFromJsonAsync<PagedResult<AdminProductListItem>>(
            $"/api/v1/admin/products?page=1&pageSize=1&categoryId={category.Id}&sort=name_asc");
        var secondPage = await client.GetFromJsonAsync<PagedResult<AdminProductListItem>>(
            $"/api/v1/admin/products?page=2&pageSize=1&categoryId={category.Id}&sort=name_asc");
        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);
        Assert.Equal(2, firstPage.TotalItems);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Single(firstPage.Items);
        Assert.Single(secondPage.Items);
        Assert.NotEqual(firstPage.Items[0].Id, secondPage.Items[0].Id);
        Assert.Contains(secondProduct.Id, firstPage.Items.Concat(secondPage.Items).Select(item => item.Id));

        using var duplicateSlug = await PostJsonAsync(
            client,
            "/api/v1/admin/products",
            CreateProductCommandFor(
                product.Slug,
                $"SKU-DIFFERENT-{suffix}",
                category.Id));
        Assert.Equal(HttpStatusCode.Conflict, duplicateSlug.StatusCode);

        using var duplicateSku = await PostJsonAsync(
            client,
            "/api/v1/admin/products",
            CreateProductCommandFor(
                $"different-{suffix}",
                product.Sku,
                category.Id));
        Assert.Equal(HttpStatusCode.Conflict, duplicateSku.StatusCode);

        using var tooLargePage = await client.GetAsync(
            "/api/v1/admin/products?page=1&pageSize=49");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooLargePage.StatusCode);
    }

    [Fact]
    public async Task InvalidPrimaryCategoryReturnsValidationProblemWithoutPartialProduct()
    {
        using var client = await CreateAuthenticatedClientAsync(_adminEmail);
        var suffix = Guid.NewGuid().ToString("N");
        var attached = await CreateCategoryAsync(client, $"attached-{suffix}");
        var primary = await CreateCategoryAsync(client, $"primary-{suffix}");
        var command = CreateProductCommandFor(
            $"invalid-primary-{suffix}",
            $"INVALID-{suffix}",
            attached.Id) with
        {
            PrimaryCategoryId = primary.Id,
        };

        using var response = await PostJsonAsync(
            client,
            "/api/v1/admin/products",
            command);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await dbContext.Products.AnyAsync(item => item.Slug == command.Slug));
    }

    [Fact]
    public async Task CategoryCycleIsRejectedAndOriginalHierarchyIsPreserved()
    {
        using var client = await CreateAuthenticatedClientAsync(_adminEmail);
        var suffix = Guid.NewGuid().ToString("N");
        var parent = await CreateCategoryAsync(client, $"parent-{suffix}");
        var child = await CreateCategoryAsync(client, $"child-{suffix}", parent.Id);

        var update = ToUpdate(parent, parentId: child.Id);
        using var response = await PatchJsonAsync(
            client,
            $"/api/v1/admin/categories/{parent.Id}",
            update);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persistedParentId = await dbContext.Categories
            .Where(item => item.Id == parent.Id)
            .Select(item => item.ParentId)
            .SingleAsync();
        Assert.Null(persistedParentId);
    }

    [Fact]
    public async Task PublishRequiresCoverAndPublicApiExposesOnlyPublishedSafeDto()
    {
        using var client = await CreateAuthenticatedClientAsync(_adminEmail);
        var suffix = Guid.NewGuid().ToString("N");
        var category = await CreateCategoryAsync(client, $"published-category-{suffix}");
        category = await MoveCategoryAsync(client, category, "InReview");
        category = await MoveCategoryAsync(client, category, "Published");

        var product = await CreateProductAsync(
            client,
            $"published-product-{suffix}",
            $"PUBLISHED-{suffix}",
            category.Id);
        product = await MoveProductToInReviewAsync(client, product);

        using var missingCover = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/admin/products/{product.Id}/publish");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, missingCover.StatusCode);

        var assetId = await CreateMediaAssetAsync($"catalog-tests/{suffix}/cover.webp");
        using var attachResponse = await PostJsonAsync(
            client,
            $"/api/v1/admin/products/{product.Id}/media",
            new AttachProductMediaCommand(assetId, "Ấm Tử Sa cao cấp", 0, true));
        Assert.Equal(HttpStatusCode.Created, attachResponse.StatusCode);

        using var publishResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/admin/products/{product.Id}/publish");
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);

        var specifications = JsonSerializer.SerializeToElement(new
        {
            capacityMl = 180,
            price = "PRIVATE_PRICE",
            adminNotes = "PRIVATE_ADMIN",
            details = new[] { new { color = "Clay", wholesalePrice = "PRIVATE_WHOLESALE" } },
        });
        using var specsUpdate = await PatchJsonAsync(client, $"/api/v1/admin/products/{product.Id}",
            new UpdateProductCommand(product.Sku, product.Slug, product.Name, product.Summary,
                product.Description, product.DescriptionHtml, product.ProductType, product.MaterialText,
                product.CapacityMl, specifications, product.PrimaryCategoryId, product.BrandId,
                product.Categories.Select(item => item.Id).ToArray(), ToSeoInput(product.Seo)));
        Assert.Equal(HttpStatusCode.OK, specsUpdate.StatusCode);
        var adminAfterUpdate = await specsUpdate.Content.ReadFromJsonAsync<AdminProductDetail>();
        Assert.NotNull(adminAfterUpdate);
        Assert.Equal("PRIVATE_PRICE", adminAfterUpdate.Specifications.GetProperty("price").GetString());

        using var publicDetailResponse = await client.GetAsync(
            $"/api/v1/public/products/{product.Slug}");
        Assert.Equal(HttpStatusCode.OK, publicDetailResponse.StatusCode);
        using var publicDocument = JsonDocument.Parse(
            await publicDetailResponse.Content.ReadAsStringAsync());
        Assert.Equal(product.Slug, publicDocument.RootElement.GetProperty("slug").GetString());
        AssertNoCommerceProperties(publicDocument.RootElement);
        var publicSpecs = publicDocument.RootElement.GetProperty("specifications");
        Assert.Equal(180, publicSpecs.GetProperty("capacityMl").GetInt32());
        Assert.Equal("Clay", publicSpecs.GetProperty("details")[0].GetProperty("color").GetString());
        Assert.DoesNotContain("PRIVATE_", publicDocument.RootElement.GetRawText());

        using var publicListResponse = await client.GetAsync(
            $"/api/v1/public/products?category={category.Slug}&pageSize=48");
        Assert.Equal(HttpStatusCode.OK, publicListResponse.StatusCode);
        using var listDocument = JsonDocument.Parse(
            await publicListResponse.Content.ReadAsStringAsync());
        Assert.Contains(
            listDocument.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("slug").GetString() == product.Slug);
        AssertNoCommerceProperties(listDocument.RootElement);

        var draft = await CreateProductAsync(
            client,
            $"draft-product-{suffix}",
            $"DRAFT-{suffix}",
            category.Id);
        using var hiddenDraft = await client.GetAsync($"/api/v1/public/products/{draft.Slug}");
        Assert.Equal(HttpStatusCode.NotFound, hiddenDraft.StatusCode);

        using var archive = await SendWithCsrfAsync(client, HttpMethod.Post,
            $"/api/v1/admin/products/{product.Id}/archive");
        Assert.Equal(HttpStatusCode.OK, archive.StatusCode);
        using var hiddenArchived = await client.GetAsync($"/api/v1/public/products/{product.Slug}");
        Assert.Equal(HttpStatusCode.NotFound, hiddenArchived.StatusCode);
        var remaining = await client.GetFromJsonAsync<PagedResult<PublicProductListItem>>(
            $"/api/v1/public/products?search={suffix}");
        Assert.NotNull(remaining);
        Assert.Empty(remaining.Items);
    }

    [Fact]
    public async Task AdminCatalogEndpointsEnforceAuthenticationAndPermissions()
    {
        using var anonymous = _factory.CreateCatalogClient();
        using var anonymousResponse = await anonymous.GetAsync("/api/v1/admin/products");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using var reader = await CreateAuthenticatedClientAsync(_readerEmail);
        using var readResponse = await reader.GetAsync("/api/v1/admin/products");
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        using var forbiddenWrite = await PostJsonAsync(
            reader,
            "/api/v1/admin/brands",
            new CreateBrandCommand("Reader cannot write", $"reader-{Guid.NewGuid():N}", null));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenWrite.StatusCode);

        using var admin = await CreateAuthenticatedClientAsync(_adminEmail);
        using var missingCsrf = await admin.PostAsJsonAsync(
            "/api/v1/admin/brands",
            new CreateBrandCommand("Missing CSRF", $"missing-csrf-{Guid.NewGuid():N}", null));
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        var suffix = Guid.NewGuid().ToString("N");
        var category = await CreateCategoryAsync(admin, $"editor-category-{suffix}");
        var product = await CreateProductAsync(
            admin,
            $"editor-product-{suffix}",
            $"EDITOR-{suffix}",
            category.Id);
        product = await MoveProductToInReviewAsync(admin, product);

        using var editor = await CreateAuthenticatedClientAsync(_editorEmail);
        using var forbiddenPublish = await SendWithCsrfAsync(
            editor,
            HttpMethod.Post,
            $"/api/v1/admin/products/{product.Id}/publish");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenPublish.StatusCode);
    }

    [Fact]
    public async Task PublishedNoindexCatalogRemainsPublicButReportsRobotsFlag()
    {
        using var client = await CreateAuthenticatedClientAsync(_adminEmail);
        var suffix = Guid.NewGuid().ToString("N");
        var category = await CreateCategoryAsync(client, $"indexable-{suffix}");
        category = await MoveCategoryAsync(client, category, "InReview");
        category = await MoveCategoryAsync(client, category, "Published");
        var product = await CreateProductAsync(client, $"visibility-{suffix}", null, category.Id);
        product = await MoveProductToInReviewAsync(client, product);
        var assetId = await CreateMediaAssetAsync($"visibility/{suffix}.webp");
        using var attached = await PostJsonAsync(client, $"/api/v1/admin/products/{product.Id}/media",
            new AttachProductMediaCommand(assetId, "Ảnh sản phẩm", 0, true));
        Assert.Equal(HttpStatusCode.Created, attached.StatusCode);
        using var published = await SendWithCsrfAsync(client, HttpMethod.Post, $"/api/v1/admin/products/{product.Id}/publish");
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);

        var update = new UpdateProductCommand(product.Sku, product.Slug, product.Name, product.Summary,
            product.Description, product.DescriptionHtml, product.ProductType, product.MaterialText,
            product.CapacityMl, product.Specifications, product.PrimaryCategoryId, product.BrandId,
            product.Categories.Select(item => item.Id).ToArray(), ToSeoInput(product.Seo) with { RobotsIndex = false });
        using var noindexUpdate = await PatchJsonAsync(client, $"/api/v1/admin/products/{product.Id}", update);
        Assert.Equal(HttpStatusCode.OK, noindexUpdate.StatusCode);
        using var noindexProduct = await client.GetAsync($"/api/v1/public/products/{product.Slug}");
        Assert.Equal(HttpStatusCode.OK, noindexProduct.StatusCode);
        using var productDocument = JsonDocument.Parse(await noindexProduct.Content.ReadAsStringAsync());
        Assert.False(productDocument.RootElement.GetProperty("seo").GetProperty("robotsIndex").GetBoolean());
        var results = await client.GetFromJsonAsync<PagedResult<PublicProductListItem>>($"/api/v1/public/products?search={suffix}");
        Assert.NotNull(results);
        Assert.Contains(results.Items, item => item.Slug == product.Slug);

        using var visibleUpdate = await PatchJsonAsync(client, $"/api/v1/admin/products/{product.Id}",
            update with { Seo = ToSeoInput(product.Seo) });
        Assert.Equal(HttpStatusCode.OK, visibleUpdate.StatusCode);
        using var noindexCategoryUpdate = await PatchJsonAsync(client, $"/api/v1/admin/categories/{category.Id}",
            ToUpdate(category) with { Seo = ToSeoInput(category.Seo) with { RobotsIndex = false } });
        Assert.Equal(HttpStatusCode.OK, noindexCategoryUpdate.StatusCode);
        using var noindexCategory = await client.GetAsync($"/api/v1/public/categories/{category.Slug}");
        Assert.Equal(HttpStatusCode.OK, noindexCategory.StatusCode);
        using var categoryDocument = JsonDocument.Parse(await noindexCategory.Content.ReadAsStringAsync());
        Assert.False(categoryDocument.RootElement.GetProperty("seo").GetProperty("robotsIndex").GetBoolean());
        Assert.Contains(categoryDocument.RootElement.GetProperty("products").GetProperty("items").EnumerateArray(),
            item => item.GetProperty("slug").GetString() == product.Slug);
        using var productInNoindexCategory = await client.GetAsync($"/api/v1/public/products/{product.Slug}");
        Assert.Equal(HttpStatusCode.OK, productInNoindexCategory.StatusCode);
        var categories = await client.GetFromJsonAsync<PublicCategoryNode[]>("/api/v1/public/categories");
        Assert.NotNull(categories);
        Assert.Contains(categories, item => item.Slug == category.Slug);
    }

    [Fact]
    public async Task CatalogTransactionRollsBackWhenOperationFails()
    {
        var slug = $"rollback-{Guid.NewGuid():N}";
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<ICatalogDataStore>();
            await Assert.ThrowsAsync<ExpectedRollbackException>(() =>
                store.ExecuteInTransactionAsync(
                    async cancellationToken =>
                    {
                        store.AddBrand(new Brand("Rollback brand", slug, null));
                        await store.SaveChangesAsync(cancellationToken);
                        throw new ExpectedRollbackException();
                    },
                    CancellationToken.None));
        }

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await dbContext.Brands.AnyAsync(item => item.Slug == slug));
    }

    [Fact]
    public async Task PublishedCatalogSlugChangesCreateDirectRedirectsAndReserveOldPaths()
    {
        using var client = await CreateAuthenticatedClientAsync(_adminEmail);
        var suffix = Guid.NewGuid().ToString("N");
        var category = await CreateCategoryAsync(client, $"redirect-category-{suffix}");
        category = await MoveCategoryAsync(client, category, "InReview");
        category = await MoveCategoryAsync(client, category, "Published");
        var firstCategorySlug = category.Slug;
        var nextCategorySlug = $"next-category-{suffix}";
        using (var renameCategory = await PatchJsonAsync(client, $"/api/v1/admin/categories/{category.Id}",
            ToUpdate(category) with { Slug = nextCategorySlug }))
        {
            Assert.Equal(HttpStatusCode.OK, renameCategory.StatusCode);
            category = Assert.IsType<AdminCategoryDetail>(await renameCategory.Content.ReadFromJsonAsync<AdminCategoryDetail>());
        }
        using (var redirect = await client.GetAsync($"/api/v1/public/seo-redirects?sourcePath=/danh-muc/{firstCategorySlug}"))
        {
            Assert.Equal(HttpStatusCode.OK, redirect.StatusCode);
            Assert.Contains($"/danh-muc/{nextCategorySlug}", await redirect.Content.ReadAsStringAsync());
        }
        using (var detail = await client.GetAsync($"/api/v1/public/categories/{nextCategorySlug}"))
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using (var duplicate = await PostJsonAsync(client, "/api/v1/admin/categories",
            new CreateCategoryCommand(null, firstCategorySlug, "Reserved path", "<p>Test</p>", 0, ValidSeo(suffix))))
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var product = await CreateProductAsync(client, $"redirect-product-{suffix}", null, category.Id);
        product = await MoveProductToInReviewAsync(client, product);
        var assetId = await CreateMediaAssetAsync($"redirect-tests/{suffix}/cover.webp");
        using (var attach = await PostJsonAsync(client, $"/api/v1/admin/products/{product.Id}/media",
            new AttachProductMediaCommand(assetId, "Cover", 0, true)))
            Assert.Equal(HttpStatusCode.Created, attach.StatusCode);
        using (var publish = await SendWithCsrfAsync(client, HttpMethod.Post,
            $"/api/v1/admin/products/{product.Id}/publish"))
            Assert.Equal(HttpStatusCode.OK, publish.StatusCode);

        var firstProductSlug = product.Slug;
        var secondProductSlug = $"second-product-{suffix}";
        var thirdProductSlug = $"third-product-{suffix}";
        var update = new UpdateProductCommand(product.Sku, secondProductSlug, product.Name,
            product.Summary, product.Description, product.DescriptionHtml, product.ProductType,
            product.MaterialText, product.CapacityMl, product.Specifications,
            product.PrimaryCategoryId, product.BrandId, product.Categories.Select(x => x.Id).ToArray(),
            ToSeoInput(product.Seo));
        using (var staleCanonical = await PatchJsonAsync(client, $"/api/v1/admin/products/{product.Id}",
            update with { Seo = ToSeoInput(product.Seo) with
                { CanonicalOverride = $"https://catalog.test/san-pham/{firstProductSlug}" } }))
            Assert.Equal(HttpStatusCode.UnprocessableEntity, staleCanonical.StatusCode);
        using (var rename = await PatchJsonAsync(client, $"/api/v1/admin/products/{product.Id}", update))
            Assert.Equal(HttpStatusCode.OK, rename.StatusCode);
        using (var renameAgain = await PatchJsonAsync(client, $"/api/v1/admin/products/{product.Id}",
            update with { Slug = thirdProductSlug }))
            Assert.Equal(HttpStatusCode.OK, renameAgain.StatusCode);
        foreach (var oldSlug in new[] { firstProductSlug, secondProductSlug })
        {
            using var redirect = await client.GetAsync($"/api/v1/public/seo-redirects?sourcePath=/san-pham/{oldSlug}");
            Assert.Equal(HttpStatusCode.OK, redirect.StatusCode);
            Assert.Contains($"/san-pham/{thirdProductSlug}", await redirect.Content.ReadAsStringAsync());
        }
        using (var detail = await client.GetAsync($"/api/v1/public/products/{thirdProductSlug}"))
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using (var loop = await PatchJsonAsync(client, $"/api/v1/admin/products/{product.Id}",
            update with { Slug = firstProductSlug }))
            Assert.Equal(HttpStatusCode.Conflict, loop.StatusCode);
        using (var afterRejectedRename = await client.GetAsync($"/api/v1/public/products/{thirdProductSlug}"))
            Assert.Equal(HttpStatusCode.OK, afterRejectedRename.StatusCode);

        // A product may be reopened for editing after archival. Its former public URL must still
        // join the direct redirect chain when the slug changes before the next publication.
        using (var archive = await SendWithCsrfAsync(client, HttpMethod.Post,
            $"/api/v1/admin/products/{product.Id}/archive"))
            Assert.Equal(HttpStatusCode.OK, archive.StatusCode);
        var fourthProductSlug = $"reopened-product-{suffix}";
        AdminProductDetail reopened;
        using (var reopen = await PatchJsonAsync(client, $"/api/v1/admin/products/{product.Id}",
            update with { Slug = fourthProductSlug, Status = "Draft" }))
        {
            Assert.Equal(HttpStatusCode.OK, reopen.StatusCode);
            reopened = Assert.IsType<AdminProductDetail>(
                await reopen.Content.ReadFromJsonAsync<AdminProductDetail>());
        }
        reopened = await MoveProductToInReviewAsync(client, reopened);
        using (var republish = await SendWithCsrfAsync(client, HttpMethod.Post,
            $"/api/v1/admin/products/{product.Id}/publish"))
            Assert.Equal(HttpStatusCode.OK, republish.StatusCode);
        foreach (var oldSlug in new[] { firstProductSlug, secondProductSlug, thirdProductSlug })
        {
            using var redirect = await client.GetAsync($"/api/v1/public/seo-redirects?sourcePath=/san-pham/{oldSlug}");
            Assert.Equal(HttpStatusCode.OK, redirect.StatusCode);
            Assert.Contains($"/san-pham/{fourthProductSlug}", await redirect.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task RedirectWriteRollsBackWithCatalogTransaction()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var source = $"/san-pham/rollback-old-{suffix}";
        var target = $"/san-pham/rollback-new-{suffix}";
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<ICatalogDataStore>();
            var redirects = scope.ServiceProvider.GetRequiredService<ISeoRedirectStore>();
            await Assert.ThrowsAsync<ExpectedRollbackException>(() => store.ExecuteInTransactionAsync(
                async cancellationToken =>
                {
                    await redirects.RecordSlugChangeAsync(source, target, cancellationToken);
                    await store.SaveChangesAsync(cancellationToken);
                    throw new ExpectedRollbackException();
                }, CancellationToken.None));
        }
        await using var checkScope = _factory.Services.CreateAsyncScope();
        var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.SeoRedirects.AnyAsync(x => x.SourcePath == source));
    }

    [Fact]
    public async Task DevelopmentOpenApiContainsCatalogRoutes()
    {
        using var client = _factory.CreateCatalogClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        string[] expectedPaths =
        [
            "/api/v1/admin/products",
            "/api/v1/admin/products/{id}",
            "/api/v1/admin/products/{id}/publish",
            "/api/v1/admin/products/{id}/archive",
            "/api/v1/admin/products/{id}/media",
            "/api/v1/admin/products/{id}/media/{mediaId}",
            "/api/v1/admin/products/{id}/media/{mediaId}/cover",
            "/api/v1/admin/categories",
            "/api/v1/admin/categories/{id}",
            "/api/v1/admin/brands",
            "/api/v1/admin/brands/{id}",
            "/api/v1/public/products",
            "/api/v1/public/products/{slug}",
            "/api/v1/public/categories",
            "/api/v1/public/categories/{slug}",
        ];

        foreach (var expectedPath in expectedPaths)
        {
            Assert.True(paths.TryGetProperty(expectedPath, out _), expectedPath);
        }
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = _factory.CreateCatalogClient();
        var csrf = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest(email, Password)),
        };
        request.Headers.Add(csrf.HeaderName, csrf.RequestToken);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    private static async Task<AdminCategoryDetail> CreateCategoryAsync(
        HttpClient client,
        string slug,
        Guid? parentId = null)
    {
        using var response = await PostJsonAsync(
            client,
            "/api/v1/admin/categories",
            new CreateCategoryCommand(
                parentId,
                slug,
                $"Category {slug}",
                "<p>Nội dung danh mục</p>",
                0,
                ValidSeo(slug)));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<AdminCategoryDetail>(
            await response.Content.ReadFromJsonAsync<AdminCategoryDetail>());
    }

    private static async Task<AdminCategoryDetail> MoveCategoryAsync(
        HttpClient client,
        AdminCategoryDetail category,
        string status)
    {
        using var response = await PatchJsonAsync(
            client,
            $"/api/v1/admin/categories/{category.Id}",
            ToUpdate(category, status: status));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<AdminCategoryDetail>(
            await response.Content.ReadFromJsonAsync<AdminCategoryDetail>());
    }

    private static async Task<AdminProductDetail> CreateProductAsync(
        HttpClient client,
        string slug,
        string? sku,
        Guid categoryId)
    {
        using var response = await PostJsonAsync(
            client,
            "/api/v1/admin/products",
            CreateProductCommandFor(slug, sku, categoryId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<AdminProductDetail>(
            await response.Content.ReadFromJsonAsync<AdminProductDetail>());
    }

    private static async Task<AdminProductDetail> MoveProductToInReviewAsync(
        HttpClient client,
        AdminProductDetail product)
    {
        var update = new UpdateProductCommand(
            product.Sku,
            product.Slug,
            product.Name,
            product.Summary,
            product.Description,
            product.DescriptionHtml,
            product.ProductType,
            product.MaterialText,
            product.CapacityMl,
            product.Specifications,
            product.PrimaryCategoryId,
            product.BrandId,
            product.Categories.Select(item => item.Id).ToArray(),
            ToSeoInput(product.Seo),
            "InReview");
        using var response = await PatchJsonAsync(
            client,
            $"/api/v1/admin/products/{product.Id}",
            update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<AdminProductDetail>(
            await response.Content.ReadFromJsonAsync<AdminProductDetail>());
    }

    private async Task<Guid> CreateMediaAssetAsync(string storageKey)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var asset = new MediaAsset(storageKey, "image/webp", 2_048, 1200, 900);
        dbContext.MediaAssets.Add(asset);
        await dbContext.SaveChangesAsync();
        return asset.Id;
    }

    private static CreateProductCommand CreateProductCommandFor(
        string slug,
        string? sku,
        Guid categoryId) =>
        new(
            sku,
            slug,
            $"Product {slug}",
            "Một sản phẩm trà cụ cao cấp có nội dung mô tả đầy đủ.",
            JsonSerializer.SerializeToElement(new { type = "doc", version = 1 }),
            "<p>Mô tả sản phẩm an toàn và đầy đủ.</p>",
            "Teapot",
            "Clay",
            180,
            JsonSerializer.SerializeToElement(new { capacityMl = 180 }),
            categoryId,
            null,
            [categoryId],
            ValidSeo(slug));

    private static SeoInput ValidSeo(string suffix) =>
        new(
            $"SEO title {suffix}",
            $"Meta description for {suffix}",
            RobotsIndex: true,
            OgTitle: $"OG {suffix}",
            OgDescription: $"OG description {suffix}");

    private static SeoInput ToSeoInput(SeoDto seo) =>
        new(
            seo.SeoTitle,
            seo.MetaDescription,
            seo.CanonicalOverride,
            seo.RobotsIndex,
            seo.OgTitle,
            seo.OgDescription,
            seo.OgImageAssetId);

    private static UpdateCategoryCommand ToUpdate(
        AdminCategoryDetail category,
        Guid? parentId = null,
        string? status = null) =>
        new(
            parentId,
            category.Slug,
            category.Name,
            category.DescriptionHtml,
            category.SortOrder,
            ToSeoInput(category.Seo),
            status);

    private static async Task<HttpResponseMessage> PostJsonAsync<T>(
        HttpClient client,
        string uri,
        T body) =>
        await SendJsonWithCsrfAsync(client, HttpMethod.Post, uri, body);

    private static async Task<HttpResponseMessage> PatchJsonAsync<T>(
        HttpClient client,
        string uri,
        T body) =>
        await SendJsonWithCsrfAsync(client, HttpMethod.Patch, uri, body);

    private static async Task<HttpResponseMessage> SendJsonWithCsrfAsync<T>(
        HttpClient client,
        HttpMethod method,
        string uri,
        T body)
    {
        var csrf = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(method, uri)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add(csrf.HeaderName, csrf.RequestToken);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendWithCsrfAsync(
        HttpClient client,
        HttpMethod method,
        string uri)
    {
        var csrf = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Add(csrf.HeaderName, csrf.RequestToken);
        return await client.SendAsync(request);
    }

    private static async Task<AntiforgeryTokenResponse> GetAntiforgeryTokenAsync(
        HttpClient client)
    {
        var csrf = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/v1/admin/auth/csrf");
        return Assert.IsType<AntiforgeryTokenResponse>(csrf);
    }

    private static void AssertNoCommerceProperties(JsonElement element)
    {
        string[] forbidden = ["price", "cart", "order", "checkout", "payment", "inventory"];

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                Assert.DoesNotContain(
                    property.Name,
                    forbidden,
                    StringComparer.OrdinalIgnoreCase);
                AssertNoCommerceProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                AssertNoCommerceProperties(item);
            }
        }
    }

    private async Task CreateUserAsync(
        string email,
        string? role = null,
        string? permission = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            LockoutEnabled = true,
        };

        var createResult = await userManager.CreateAsync(user, Password);
        Assert.True(
            createResult.Succeeded,
            string.Join(", ", createResult.Errors.Select(error => error.Code)));

        if (role is not null)
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            Assert.True(
                roleResult.Succeeded,
                string.Join(", ", roleResult.Errors.Select(error => error.Code)));
        }

        if (permission is not null)
        {
            var claimResult = await userManager.AddClaimAsync(
                user,
                new Claim(CustomClaimTypes.Permission, permission));
            Assert.True(
                claimResult.Succeeded,
                string.Join(", ", claimResult.Errors.Select(error => error.Code)));
        }
    }

    private async Task DeleteUserIfPresentAsync(string email)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            await userManager.DeleteAsync(user);
        }
    }

    private sealed class CatalogApiFactory(string connectionString)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Postgres", connectionString);
        }

        public HttpClient CreateCatalogClient() => CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = true,
            });
    }

    private sealed class ExpectedRollbackException : Exception;
}
