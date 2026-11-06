using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeaPremium.Api.Contracts.Authentication;
using TeaPremium.Application.Security;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Common;
using TeaPremium.Infrastructure.Identity;
using TeaPremium.Infrastructure.Persistence;

namespace TeaPremium.IntegrationTests;

[Collection(PostgreSqlTestGroup.Name)]
public sealed class ContentAndLeadsApiTests : IAsyncLifetime, IDisposable
{
    private const string Password = "Integration1!Password";
    private readonly string _adminEmail = $"content-admin-{Guid.NewGuid():N}@integration.test";
    private readonly string _editorEmail = $"content-editor-{Guid.NewGuid():N}@integration.test";
    private ApiFactory _factory = null!;

    public ContentAndLeadsApiTests(PostgreSqlFixture database) => ArgumentNullException.ThrowIfNull(database);

    public async Task InitializeAsync()
    {
        _factory = new ApiFactory(PostgreSqlFixture.GetRequiredConnectionString());
        await CreateUserAsync(_adminEmail, AdminRoles.Admin);
        await CreateUserAsync(_editorEmail, AdminRoles.Editor);
    }
    public Task DisposeAsync() => Task.CompletedTask;
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task SiteSettingsRequirePermissionAndExposeOnlyPublicFields()
    {
        using var anonymous = _factory.CreateApiClient();
        using var editor = await LoginAsync(_editorEmail);
        using var admin = await LoginAsync(_adminEmail);
        using (var denied = await anonymous.GetAsync("/api/v1/admin/settings"))
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        using (var forbidden = await editor.GetAsync("/api/v1/admin/settings"))
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        using (var forbiddenWrite = await SendAdminAsync(editor, HttpMethod.Put,
            "/api/v1/admin/settings", new { siteName = "Không có quyền" }))
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenWrite.StatusCode);

        var name = $"Tea test {Guid.NewGuid():N}";
        using (var invalid = await SendAdminAsync(admin, HttpMethod.Put,
            "/api/v1/admin/settings", new { siteName = name, zaloUrl = "https://example.com/redirect" }))
            Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        using (var saved = await SendAdminAsync(admin, HttpMethod.Put,
            "/api/v1/admin/settings", new
            {
                siteName = name, siteDescription = "Mô tả thử nghiệm", brandTagline = "Trà cụ",
                hotline = "0912345678", zaloUrl = "https://zalo.me/0912345678",
                facebookUrl = "https://facebook.com/example", email = "shop@integration.test",
                publicWebsiteUrl = "https://tea.integration.test",
                defaultSeoTitle = "Tea test SEO", defaultMetaDescription = "Mô tả SEO",
            }))
            Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        using var publicResponse = await anonymous.GetAsync("/api/v1/public/settings");
        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        var body = await publicResponse.Content.ReadAsStringAsync();
        Assert.Contains(name, body);
        Assert.DoesNotContain("updatedByUserId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accessKey", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ArticleCrudPreviewVisibilityAndPublishPermission()
    {
        using var admin = await LoginAsync(_adminEmail);
        using var editor = await LoginAsync(_editorEmail);
        var slug = $"test-article-{Guid.NewGuid():N}";
        using var categoryResponse = await SendAdminAsync(admin, HttpMethod.Post,
            "/api/v1/admin/article-categories", new { slug = $"knowledge-{Guid.NewGuid():N}", name = "Danh mục thử" });
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        using var categoryJson = JsonDocument.Parse(await categoryResponse.Content.ReadAsStringAsync());
        var categoryId = categoryJson.RootElement.GetProperty("id").GetGuid();
        var input = new
        {
            slug, title = "Bài viết kiểm thử", excerpt = "Nội dung kiểm thử không có tuyên bố sản phẩm.",
            contentJson = """
                {"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"<script>alert(1)</script>"}]}]}
                """,
            coverAssetId = (Guid?)null, categoryIds = new[] { categoryId },
            seo = new { seoTitle = "Bài viết kiểm thử", metaDescription = "Nội dung kiểm thử.",
                canonicalOverride = (string?)null, robotsIndex = true, ogTitle = (string?)null,
                ogDescription = (string?)null, ogImageAssetId = (Guid?)null },
        };
        using var create = await SendAdminAsync(admin, HttpMethod.Post, "/api/v1/admin/articles", input);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("Draft", created.RootElement.GetProperty("status").GetString());
        Assert.DoesNotContain("<script", created.RootElement.GetProperty("contentHtml").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(categoryId, created.RootElement.GetProperty("categories")[0].GetProperty("id").GetGuid());
        using var update = await SendAdminAsync(admin, HttpMethod.Patch, $"/api/v1/admin/articles/{id}",
            new { input.slug, title = "Bài viết đã sửa", input.excerpt, input.contentJson,
                input.coverAssetId, input.categoryIds, input.seo });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Contains("Bài viết đã sửa", await update.Content.ReadAsStringAsync());

        using var publicClient = _factory.CreateApiClient();
        using var before = await publicClient.GetAsync($"/api/v1/public/articles/{slug}");
        Assert.Equal(HttpStatusCode.NotFound, before.StatusCode);
        using var preview = await admin.GetAsync($"/api/v1/admin/articles/{id}/preview");
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        Assert.Equal("noindex, nofollow", preview.Headers.GetValues("X-Robots-Tag").Single());
        using var review = await SendAdminAsync(admin, HttpMethod.Post, $"/api/v1/admin/articles/{id}/review");
        Assert.Equal(HttpStatusCode.OK, review.StatusCode);
        using var forbidden = await SendAdminAsync(editor, HttpMethod.Post, $"/api/v1/admin/articles/{id}/publish");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        using var publish = await SendAdminAsync(admin, HttpMethod.Post, $"/api/v1/admin/articles/{id}/publish");
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
        using var visible = await publicClient.GetAsync($"/api/v1/public/articles/{slug}");
        Assert.Equal(HttpStatusCode.OK, visible.StatusCode);
        var body = await visible.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<script", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contentJson", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authorId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("coverAssetId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"id\":", body, StringComparison.OrdinalIgnoreCase);
        var changedSlug = $"changed-{slug}";
        using var updateSlug = await SendAdminAsync(admin, HttpMethod.Patch, $"/api/v1/admin/articles/{id}",
            new { input.title, input.excerpt, slug = changedSlug, input.contentJson,
                input.coverAssetId, input.categoryIds, input.seo });
        Assert.Equal(HttpStatusCode.OK, updateSlug.StatusCode);
        using var oldRedirect = await publicClient.GetAsync($"/api/v1/public/seo-redirects?sourcePath=/kien-thuc/{slug}");
        Assert.Equal(HttpStatusCode.OK, oldRedirect.StatusCode);
        Assert.Contains($"/kien-thuc/{changedSlug}", await oldRedirect.Content.ReadAsStringAsync());
        using var newArticle = await publicClient.GetAsync($"/api/v1/public/articles/{changedSlug}");
        Assert.Equal(HttpStatusCode.OK, newArticle.StatusCode);
        using var duplicateSource = await SendAdminAsync(admin, HttpMethod.Post, "/api/v1/admin/articles",
            new { input.title, input.excerpt, input.slug, input.contentJson,
                input.coverAssetId, input.categoryIds, input.seo });
        Assert.Equal(HttpStatusCode.Conflict, duplicateSource.StatusCode);
        using var noindexUpdate = await SendAdminAsync(admin, HttpMethod.Patch,
            $"/api/v1/admin/articles/{id}", new
            {
                slug = changedSlug, title = "Bài viết đã sửa", input.excerpt, input.contentJson,
                input.coverAssetId, input.categoryIds,
                seo = new { input.seo.seoTitle, input.seo.metaDescription,
                    input.seo.canonicalOverride, robotsIndex = false, input.seo.ogTitle,
                    input.seo.ogDescription, input.seo.ogImageAssetId },
            });
        Assert.Equal(HttpStatusCode.OK, noindexUpdate.StatusCode);
        using var noindexArticle = await publicClient.GetAsync($"/api/v1/public/articles/{changedSlug}");
        Assert.Equal(HttpStatusCode.OK, noindexArticle.StatusCode);
        using var noindexDocument = JsonDocument.Parse(await noindexArticle.Content.ReadAsStringAsync());
        Assert.False(noindexDocument.RootElement.GetProperty("seo").GetProperty("robotsIndex").GetBoolean());
        using var publicList = JsonDocument.Parse(await publicClient.GetStringAsync("/api/v1/public/articles"));
        Assert.Contains(publicList.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("slug").GetString() == changedSlug);
        using var archive = await SendAdminAsync(admin, HttpMethod.Post, $"/api/v1/admin/articles/{id}/archive");
        Assert.Equal(HttpStatusCode.OK, archive.StatusCode);
        using var after = await publicClient.GetAsync($"/api/v1/public/articles/{changedSlug}");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
        using var archivedRedirect = await publicClient.GetAsync($"/api/v1/public/seo-redirects?sourcePath=/kien-thuc/{slug}");
        Assert.Equal(HttpStatusCode.NotFound, archivedRedirect.StatusCode);
    }

    [Fact]
    public async Task LeadRequiresConsentHoneypotDoesNotPersistAndEventsDeduplicate()
    {
        using var client = _factory.CreateApiClient();
        using var admin = await LoginAsync(_adminEmail);
        var before = await admin.GetFromJsonAsync<LeadCount>("/api/v1/admin/leads");
        var input = new { name = "Khách thử", phone = "0912345678", email = (string?)null,
            message = "Xin tư vấn sản phẩm.", productId = (Guid?)null, sourcePage = "/lien-he",
            channel = "web_form", utm = new { source = "integration-test" }, consent = true, website = "" };
        using var invalid = await client.PostAsJsonAsync("/api/v1/public/leads", new { input.name, input.phone,
            input.email, input.message, input.productId, input.sourcePage, input.channel, input.utm,
            consent = false, input.website });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        using var invalidEmail = await client.PostAsJsonAsync("/api/v1/public/leads", new { input.name,
            phone = (string?)null, email = "invalid", input.message, input.productId, input.sourcePage,
            input.channel, input.utm, input.consent, input.website });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidEmail.StatusCode);
        using var invalidUtm = await client.PostAsJsonAsync("/api/v1/public/leads", new { input.name,
            input.phone, input.email, input.message, input.productId, input.sourcePage,
            input.channel, utm = new { source = new string('x', 201) }, input.consent, input.website });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidUtm.StatusCode);
        using var spam = await client.PostAsJsonAsync("/api/v1/public/leads", new { input.name, input.phone,
            input.email, input.message, input.productId, input.sourcePage, input.channel, input.utm,
            input.consent, website = "bot-filled" });
        Assert.Equal(HttpStatusCode.Accepted, spam.StatusCode);
        using var accepted = await client.PostAsJsonAsync("/api/v1/public/leads", input);
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        Assert.Equal(string.Empty, await accepted.Content.ReadAsStringAsync());
        var after = await admin.GetFromJsonAsync<LeadCount>("/api/v1/admin/leads");
        Assert.Equal(before!.TotalItems + 1, after!.TotalItems);
        using var leadPage = JsonDocument.Parse(await admin.GetStringAsync("/api/v1/admin/leads"));
        var leadId = leadPage.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();
        using var statusChange = await SendAdminAsync(admin, HttpMethod.Patch,
            $"/api/v1/admin/leads/{leadId}/status", new { status = "Contacted" });
        Assert.Equal(HttpStatusCode.OK, statusChange.StatusCode);
        Assert.Contains("\"status\":\"Contacted\"", await statusChange.Content.ReadAsStringAsync());
        var eventId = Guid.NewGuid();
        var click = new { clientEventId = eventId, eventType = "contact_click", channel = "zalo",
            productId = (Guid?)null, pagePath = "/lien-he", consentState = true };
        using var first = await client.PostAsJsonAsync("/api/v1/public/contact-events", click);
        using var second = await client.PostAsJsonAsync("/api/v1/public/contact-events", click);
        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.ContactEvents.CountAsync(x => x.ClientEventId == eventId));
        Assert.Equal(after.TotalItems, (await admin.GetFromJsonAsync<LeadCount>("/api/v1/admin/leads"))!.TotalItems);
        using var editor = await LoginAsync(_editorEmail);
        using var forbidden = await editor.GetAsync("/api/v1/admin/leads");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task PublicLeadRateLimitRejectsSixthRequest()
    {
        using var client = _factory.CreateApiClient();
        HttpStatusCode last = default;
        for (var i = 0; i < 6; i++)
        {
            using var response = await client.PostAsJsonAsync("/api/v1/public/leads", new
            { name = "Rate test", phone = "0912345678", message = "Test request", sourcePage = "/lien-he",
                channel = "web_form", consent = false, website = "" });
            last = response.StatusCode;
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, last);
    }

    [Fact]
    public async Task PublicContactEventRateLimitRejectsThirtyFirstRequest()
    {
        using var client = _factory.CreateApiClient();
        for (var i = 0; i < 31; i++)
        {
            using var response = await client.PostAsJsonAsync("/api/v1/public/contact-events", new
            {
                clientEventId = Guid.NewGuid(), eventType = "contact_click", channel = "zalo",
                productId = (Guid?)null, pagePath = "/lien-he", consentState = false,
            });
            Assert.Equal(i == 30 ? HttpStatusCode.TooManyRequests : HttpStatusCode.Accepted,
                response.StatusCode);
        }
    }

    [Fact]
    public async Task PublicLeadCannotReferenceDraftOrArchivedProduct()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var category = new Category($"lead-category-{suffix}", "Test category", "",
            new SeoMetadata(null, null));
        category.MoveToInReview();
        category.Publish();
        Product CreateProduct(string label) => new($"lead-{label}-{suffix}", "Test product", "Summary",
            "{}", "<p>Description</p>", "test", "{}", new SeoMetadata(null, null),
            primaryCategoryId: category.Id);
        var draft = CreateProduct("draft");
        var archived = CreateProduct("archived");
        archived.MoveToInReview();
        archived.Publish(DateTimeOffset.UtcNow);
        archived.Archive(DateTimeOffset.UtcNow);
        db.AddRange(category, draft, archived);
        await db.SaveChangesAsync();

        using var client = _factory.CreateApiClient();
        var before = await db.Leads.CountAsync();
        foreach (var productId in new[] { draft.Id, archived.Id })
        {
            using var response = await client.PostAsJsonAsync("/api/v1/public/leads", new
            {
                name = "Khách thử", email = "test@example.invalid", message = "Yêu cầu kiểm thử.",
                productId, sourcePage = "/lien-he", channel = "web_form", consent = true,
                website = "",
            });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        }
        Assert.Equal(before, await db.Leads.CountAsync());
    }

    [Fact]
    public async Task EmptyArticleCannotBePublished()
    {
        using var admin = await LoginAsync(_adminEmail);
        using var create = await SendAdminAsync(admin, HttpMethod.Post, "/api/v1/admin/articles", new
        {
            slug = $"empty-article-{Guid.NewGuid():N}", title = "Bài trống", excerpt = "Bản nháp trống.",
            contentJson = """{"type":"doc","content":[{"type":"paragraph"}]}""",
            coverAssetId = (Guid?)null, categoryIds = Array.Empty<Guid>(),
            seo = new { seoTitle = "Bài trống", metaDescription = "Bản nháp trống.",
                robotsIndex = true, canonicalOverride = (string?)null, ogTitle = (string?)null,
                ogDescription = (string?)null, ogImageAssetId = (Guid?)null },
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();
        using var review = await SendAdminAsync(admin, HttpMethod.Post, $"/api/v1/admin/articles/{id}/review");
        Assert.Equal(HttpStatusCode.OK, review.StatusCode);
        using var publish = await SendAdminAsync(admin, HttpMethod.Post, $"/api/v1/admin/articles/{id}/publish");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, publish.StatusCode);
    }

    [Fact]
    public async Task PublicSubmitCorsAllowsConfiguredDevelopmentOriginOnly()
    {
        using var client = _factory.CreateApiClient();
        using var allowed = new HttpRequestMessage(HttpMethod.Options, "/api/v1/public/leads");
        allowed.Headers.Add("Origin", "http://localhost:3000");
        allowed.Headers.Add("Access-Control-Request-Method", "POST");
        allowed.Headers.Add("Access-Control-Request-Headers", "content-type");
        using var allowedResponse = await client.SendAsync(allowed);
        Assert.True(allowedResponse.IsSuccessStatusCode);
        Assert.Equal("http://localhost:3000", allowedResponse.Headers.GetValues("Access-Control-Allow-Origin").Single());

        using var denied = new HttpRequestMessage(HttpMethod.Options, "/api/v1/public/leads");
        denied.Headers.Add("Origin", "https://unapproved.example");
        denied.Headers.Add("Access-Control-Request-Method", "POST");
        denied.Headers.Add("Access-Control-Request-Headers", "content-type");
        using var deniedResponse = await client.SendAsync(denied);
        Assert.False(deniedResponse.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private async Task<HttpClient> LoginAsync(string email)
    {
        var client = _factory.CreateApiClient();
        var csrf = (await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/v1/admin/auth/csrf"))!;
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/auth/login")
        { Content = JsonContent.Create(new LoginRequest(email, Password)) };
        login.Headers.Add(csrf.HeaderName, csrf.RequestToken);
        using var response = await client.SendAsync(login);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    private static async Task<HttpResponseMessage> SendAdminAsync(HttpClient client, HttpMethod method,
        string path, object? payload = null)
    {
        var csrf = (await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/v1/admin/auth/csrf"))!;
        using var request = new HttpRequestMessage(method, path);
        if (payload is not null) request.Content = JsonContent.Create(payload);
        request.Headers.Add(csrf.HeaderName, csrf.RequestToken);
        return await client.SendAsync(request);
    }

    private async Task CreateUserAsync(string email, string role)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, Email = email,
            EmailConfirmed = true, LockoutEnabled = true };
        var created = await manager.CreateAsync(user, Password);
        Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(x => x.Code)));
        var assigned = await manager.AddToRoleAsync(user, role);
        Assert.True(assigned.Succeeded, string.Join(", ", assigned.Errors.Select(x => x.Code)));
    }

    private sealed record LeadCount(int TotalItems);
    private sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Postgres", connectionString);
        }
        public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
        { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost"), HandleCookies = true });
    }
}
