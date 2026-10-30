using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TeaPremium.Api.Contracts.Authentication;
using TeaPremium.Application.Security;
using TeaPremium.Infrastructure.Identity;

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
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
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
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AdminCanLoginReadCurrentUserAccessCatalogAndLogout()
    {
        using var client = CreateClient();

        using var loginResponse = await LoginAsync(client, _adminEmail);
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
