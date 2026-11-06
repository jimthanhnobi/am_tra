using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeaPremium.Api.Contracts.Authentication;
using TeaPremium.Application.Authentication;
using TeaPremium.Application.Security;
using TeaPremium.Infrastructure.Identity;
using TeaPremium.Infrastructure.Persistence;
using TeaPremium.Application.Commerce;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Common;

namespace TeaPremium.IntegrationTests;

[Collection(PostgreSqlTestGroup.Name)]
public sealed class AuthenticationTests : IAsyncLifetime, IDisposable
{
    private const string Password = "Integration1!Password";

    private readonly string _adminEmail = $"admin-{Guid.NewGuid():N}@integration.test";
    private readonly string _unprivilegedEmail = $"user-{Guid.NewGuid():N}@integration.test";
    private AuthApiFactory _factory = null!;

    public AuthenticationTests(PostgreSqlFixture database)
    {
        ArgumentNullException.ThrowIfNull(database);
    }

    public async Task InitializeAsync()
    {
        _factory = new AuthApiFactory(PostgreSqlFixture.GetRequiredConnectionString());

        await CreateUserAsync(_adminEmail, AdminRoles.Admin);
        await CreateUserAsync(_unprivilegedEmail);
    }

    public async Task DisposeAsync()
    {
        await DeleteUserIfPresentAsync(_adminEmail);
        await DeleteUserIfPresentAsync(_unprivilegedEmail);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task AnonymousAdminRequestReturnsUnauthorized()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/api/v1/admin/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OnlyAuthorizedAdminCanRegisterAnEditorWithCsrf()
    {
        var email = $"editor-created-{Guid.NewGuid():N}@integration.test";
        try
        {
            using var anonymous = CreateClient();
            using var anonymousResponse = await anonymous.PostAsJsonAsync(
                "/api/v1/admin/users", new CreateAdminUserCommand(email, Password, AdminRoles.Editor));
            Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

            using var unprivileged = CreateClient();
            await LoginAsync(unprivileged, _unprivilegedEmail);
            var unprivilegedCsrf = await GetAntiforgeryTokenAsync(unprivileged);
            using var forbiddenRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/users")
            { Content = JsonContent.Create(new CreateAdminUserCommand(email, Password, AdminRoles.Editor)) };
            forbiddenRequest.Headers.Add(unprivilegedCsrf.HeaderName, unprivilegedCsrf.RequestToken);
            using var forbidden = await unprivileged.SendAsync(forbiddenRequest);
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

            using var admin = CreateClient();
            await LoginAsync(admin, _adminEmail);
            using var missingCsrf = await admin.PostAsJsonAsync(
                "/api/v1/admin/users", new CreateAdminUserCommand(email, Password, AdminRoles.Editor));
            Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

            var csrf = await GetAntiforgeryTokenAsync(admin);
            async Task<HttpResponseMessage> RegisterAsync(string role)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/users")
                { Content = JsonContent.Create(new CreateAdminUserCommand(email, Password, role)) };
                request.Headers.Add(csrf.HeaderName, csrf.RequestToken);
                return await admin.SendAsync(request);
            }
            using var rejectedRole = await RegisterAsync(AdminRoles.Admin);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, rejectedRole.StatusCode);
            using var created = await RegisterAsync(AdminRoles.Editor);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var duplicate = await RegisterAsync(AdminRoles.Editor);
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

            await using var scope = _factory.Services.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.True(await users.IsInRoleAsync(user, AdminRoles.Editor));
            Assert.True(await users.CheckPasswordAsync(user, Password));
        }
        finally { await DeleteUserIfPresentAsync(email); }
    }

    [Fact]
    public async Task LoginWithoutAntiforgeryTokenIsRejected()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/admin/auth/login",
            new LoginRequest(_adminEmail, Password));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AuthenticatedUserWithoutPermissionReturnsForbidden()
    {
        using var client = CreateClient();
        await LoginAsync(client, _unprivilegedEmail);

        using var response = await client.GetAsync("/api/v1/admin/catalog/summary");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AdminCanLoginReadCurrentUserAccessCatalogAndLogout()
    {
        using var client = CreateClient();

        using var loginResponse = await LoginAsync(client, _adminEmail);
        Assert.Contains("no-store", loginResponse.Headers.CacheControl?.ToString());
        var setCookie = Assert.Single(
            loginResponse.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith("TeaPremium.Admin=", StringComparison.Ordinal));
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/", setCookie, StringComparison.OrdinalIgnoreCase);

        var loginUser = await loginResponse.Content
            .ReadFromJsonAsync<CurrentAdminUserResponse>();
        Assert.NotNull(loginUser);
        Assert.Contains(Permissions.CatalogRead, loginUser.Permissions);
        Assert.Contains(Permissions.UsersManage, loginUser.Permissions);

        var currentUser = await client.GetFromJsonAsync<CurrentAdminUserResponse>(
            "/api/v1/admin/auth/me");
        Assert.NotNull(currentUser);
        Assert.Equal(_adminEmail, currentUser.Email, ignoreCase: true);
        Assert.Contains(AdminRoles.Admin, currentUser.Roles);
        Assert.Contains(Permissions.CatalogRead, currentUser.Permissions);
        Assert.Contains(Permissions.UsersManage, currentUser.Permissions);

        using var catalogResponse = await client.GetAsync("/api/v1/admin/catalog/summary");
        Assert.Equal(HttpStatusCode.OK, catalogResponse.StatusCode);
        Assert.Contains("no-store", catalogResponse.Headers.CacheControl?.ToString());

        var csrf = await GetAntiforgeryTokenAsync(client);
        using var logoutRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/admin/auth/logout");
        logoutRequest.Headers.Add(csrf.HeaderName, csrf.RequestToken);
        using var logoutResponse = await client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Contains(
            logoutResponse.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith("TeaPremium.Admin=", StringComparison.Ordinal) &&
                     value.Contains("expires=", StringComparison.OrdinalIgnoreCase));

        using var afterLogoutResponse = await client.GetAsync("/api/v1/admin/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogoutResponse.StatusCode);
    }

    [Fact]
    public async Task LoginRateLimitReturnsTooManyRequests()
    {
        await using var isolatedFactory = new AuthApiFactory(
            PostgreSqlFixture.GetRequiredConnectionString());
        using var client = isolatedFactory.CreateAuthClient();
        var csrf = await GetAntiforgeryTokenAsync(client);

        HttpStatusCode lastStatus = default;
        for (var attempt = 0; attempt < 11; attempt++)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/v1/admin/auth/login")
            {
                Content = JsonContent.Create(new LoginRequest(
                    $"missing-{Guid.NewGuid():N}@integration.test",
                    Password)),
            };
            request.Headers.Add(csrf.HeaderName, csrf.RequestToken);

            using var response = await client.SendAsync(request);
            lastStatus = response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastStatus);
    }

    [Fact]
    public async Task RepeatedFailuresLockAccountAndKeepGenericErrorMessage()
    {
        var lockedEmail = $"locked-{Guid.NewGuid():N}@integration.test";
        await CreateUserAsync(lockedEmail, AdminRoles.Admin);

        try
        {
            using var client = CreateClient();
            var csrf = await GetAntiforgeryTokenAsync(client);
            string? genericDetail = null;

            for (var attempt = 0; attempt < 5; attempt++)
            {
                using var response = await SendLoginAsync(
                    client,
                    csrf,
                    lockedEmail,
                    "Wrong1!Password");

                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                var detail = await ReadProblemDetailAsync(response);
                genericDetail ??= detail;
                Assert.Equal(genericDetail, detail);
                Assert.DoesNotContain(lockedEmail, detail, StringComparison.OrdinalIgnoreCase);
            }

            using var lockedResponse = await SendLoginAsync(
                client,
                csrf,
                lockedEmail,
                Password);

            Assert.Equal(HttpStatusCode.Unauthorized, lockedResponse.StatusCode);
            Assert.Equal(genericDetail, await ReadProblemDetailAsync(lockedResponse));
        }
        finally
        {
            await DeleteUserIfPresentAsync(lockedEmail);
        }
    }

    private HttpClient CreateClient() => _factory.CreateAuthClient();

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email)
    {
        var csrf = await GetAntiforgeryTokenAsync(client);
        var response = await SendLoginAsync(client, csrf, email, Password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return response;
    }

    [Fact]
    public async Task ReportsAcceptVietnamMonthRangeAndRejectInvalidOrMixedRanges()
    {
        using var client = CreateClient();
        await LoginAsync(client, _adminEmail);
        var today = TeaPremium.Domain.Common.BusinessCalendar.DateAt(DateTimeOffset.UtcNow);
        var first = new DateOnly(today.Year, today.Month, 1);
        var next = first.AddMonths(1);
        using var response = await client.GetAsync(FormattableString.Invariant(
            $"/api/v1/admin/reports/sales-overview?fromDate={first:yyyy-MM-dd}&toDate={next:yyyy-MM-dd}"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var invalid = await client.GetAsync("/api/v1/admin/reports/sales-overview?fromDate=2026-12-01&toDate=2026-11-01");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        using var mixed = await client.GetAsync("/api/v1/admin/reports/sales-overview?fromDate=2026-11-01&toDate=2026-12-01&from=2026-11-01T00:00:00Z");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, mixed.StatusCode);
        using var denied = CreateClient();
        await LoginAsync(denied, _unprivilegedEmail);
        using var auditResponse = await denied.GetAsync("/api/v1/admin/audit");
        Assert.Equal(HttpStatusCode.Forbidden, auditResponse.StatusCode);
    }

    [Fact]
    public async Task OrderHttpWorkflowRequiresCsrfAndKeepsDraftProductAndAuditPrivate()
    {
        using var client = CreateClient();
        await LoginAsync(client, _adminEmail);
        var csrf = await GetAntiforgeryTokenAsync(client);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var commerce = scope.ServiceProvider.GetRequiredService<CommerceService>();
        var product = new Product("http-order-" + Guid.NewGuid().ToString("N"), "Test draft", "Test",
            "{}", "", "test", "{}", new SeoMetadata(null, null));
        var disabled = new Product("disabled-" + Guid.NewGuid().ToString("N"), "Test disabled", "Test",
            "{}", "", "test", "{}", new SeoMetadata(null, null));
        disabled.SetSellable(false);
        db.Products.AddRange(product, disabled); await db.SaveChangesAsync();
        var customer = await commerce.CreateCustomerAsync(new("HTTP test customer",
            "09" + Random.Shared.Next(10000000, 99999999).ToString(System.Globalization.CultureInfo.InvariantCulture),
            "new", "unknown", false, []), CancellationToken.None);
        var input = new CreateOrderInput(customer.Id, Guid.NewGuid(), null, "phone", null,
            [new(product.Id, 1, 100000m, null)]);
        try
        {
            using var noCsrf = await client.PostAsJsonAsync("/api/v1/admin/orders", input);
            Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
            client.DefaultRequestHeaders.Add(csrf.HeaderName, csrf.RequestToken);
            using var rejected = await client.PostAsJsonAsync("/api/v1/admin/orders", input with
            { Items = [new(disabled.Id, 1, 100000m, null)] });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, rejected.StatusCode);
            using var created = await client.PostAsJsonAsync("/api/v1/admin/orders", input);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var order = (await created.Content.ReadFromJsonAsync<OrderDto>())!;
            using var completed = await client.PostAsJsonAsync($"/api/v1/admin/orders/{order.Id}/complete", new OrderTransitionInput());
            Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
            using var staleCancel = await client.PostAsJsonAsync($"/api/v1/admin/orders/{order.Id}/cancel", new OrderTransitionInput(Reason: "Stale tab"));
            Assert.Equal(HttpStatusCode.Conflict, staleCancel.StatusCode);
            Assert.Equal("application/problem+json", staleCancel.Content.Headers.ContentType?.MediaType);
            using var noReason = await client.PostAsJsonAsync($"/api/v1/admin/orders/{order.Id}/return", new OrderTransitionInput());
            Assert.Equal(HttpStatusCode.UnprocessableEntity, noReason.StatusCode);
            using var returned = await client.PostAsJsonAsync($"/api/v1/admin/orders/{order.Id}/return", new OrderTransitionInput(Reason: "Full return"));
            Assert.Equal(HttpStatusCode.OK, returned.StatusCode);
            using var publicProduct = await client.GetAsync($"/api/v1/public/products/{product.Slug}");
            Assert.Equal(HttpStatusCode.NotFound, publicProduct.StatusCode);
            var audit = await db.AuditLogs.Where(x => x.EntityId == order.Id).ToArrayAsync();
            Assert.Equal(3, audit.Length);
            Assert.All(audit, x => Assert.NotNull(x.ActorUserId));
        }
        finally
        {
            await db.SalesOrders.Where(x => x.CustomerId == customer.Id).ExecuteDeleteAsync();
            await db.Customers.Where(x => x.Id == customer.Id).ExecuteDeleteAsync();
            await db.Products.Where(x => x.Id == product.Id || x.Id == disabled.Id).ExecuteDeleteAsync();
        }
    }

    private static async Task<HttpResponseMessage> SendLoginAsync(
        HttpClient client,
        AntiforgeryTokenResponse csrf,
        string email,
        string password)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/admin/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest(email, password)),
        };
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

    private static async Task<string> ReadProblemDetailAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.GetProperty("detail").GetString() ?? string.Empty;
    }

    private async Task CreateUserAsync(string email, string? role = null)
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

    private sealed class AuthApiFactory(string connectionString)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Postgres", connectionString);
        }

        public HttpClient CreateAuthClient() => CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = true,
            });
    }
}
