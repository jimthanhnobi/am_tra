using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TeaPremium.Api.Contracts.Authentication;
using TeaPremium.Application.Catalog;
using TeaPremium.Application.Media;
using TeaPremium.Application.Security;
using TeaPremium.Infrastructure.Identity;

namespace TeaPremium.IntegrationTests;

[Collection(PostgreSqlTestGroup.Name)]
public sealed class MediaUploadApiTests : IAsyncLifetime, IDisposable
{
    private const string Password = "Integration1!Password";

    private readonly string _adminEmail = $"media-admin-{Guid.NewGuid():N}@integration.test";
    private readonly string _readerEmail = $"media-reader-{Guid.NewGuid():N}@integration.test";
    private readonly FakeObjectStorage _storage = new();
    private MediaApiFactory _factory = null!;

    public MediaUploadApiTests(PostgreSqlFixture database)
    {
        ArgumentNullException.ThrowIfNull(database);
    }

    public async Task InitializeAsync()
    {
        _factory = new MediaApiFactory(
            PostgreSqlFixture.GetRequiredConnectionString(),
            _storage);
        await CreateUserAsync(_adminEmail, AdminRoles.Admin);
        await CreateUserAsync(_readerEmail, permission: Permissions.CatalogRead);
    }

    public async Task DisposeAsync()
    {
        await DeleteUserIfPresentAsync(_adminEmail);
        await DeleteUserIfPresentAsync(_readerEmail);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task AnonymousAndUserWithoutMediaPermissionCannotCreateUploadIntent()
    {
        using var anonymous = _factory.CreateMediaClient();
        using var anonymousResponse = await anonymous.PostAsJsonAsync(
            "/api/v1/admin/media/upload-intent",
            new CreateMediaUploadIntentCommand("teapot.png", "image/png", 32));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using var reader = await CreateAuthenticatedClientAsync(_readerEmail);
        using var forbidden = await PostWithCsrfAsync(
            reader,
            "/api/v1/admin/media/upload-intent",
            new CreateMediaUploadIntentCommand("teapot.png", "image/png", 32));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task UploadIntentCompletesOnceAfterServerVerifiesObject()
    {
        using var client = await CreateAuthenticatedClientAsync(_adminEmail);
        var bytes = CreatePngHeader(1200, 900);

        using var intentResponse = await PostWithCsrfAsync(
            client,
            "/api/v1/admin/media/upload-intent",
            new CreateMediaUploadIntentCommand("am-tu-sa.png", "image/png", bytes.Length));
        Assert.Equal(HttpStatusCode.OK, intentResponse.StatusCode);
        var intent = Assert.IsType<MediaUploadIntentDto>(
            await intentResponse.Content.ReadFromJsonAsync<MediaUploadIntentDto>());
        Assert.StartsWith("catalog/", intent.ObjectKey, StringComparison.Ordinal);
        Assert.Equal("image/png", intent.RequiredHeaders["Content-Type"]);
        Assert.DoesNotContain("secret-access-key", await intentResponse.Content.ReadAsStringAsync());

        _storage.Store(intent.ObjectKey, "image/png", bytes);

        using var completeResponse = await PostWithCsrfAsync<object?>(
            client,
            $"/api/v1/admin/media/{intent.UploadId}/complete",
            null);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var asset = Assert.IsType<AdminMediaAssetDto>(
            await completeResponse.Content.ReadFromJsonAsync<AdminMediaAssetDto>());
        Assert.Equal("am-tu-sa.png", asset.FileName);
        Assert.Equal(1200, asset.Width);
        Assert.Equal(900, asset.Height);
        Assert.Equal("image/png", asset.MimeType);
        Assert.Equal($"https://media.integration.test/{intent.ObjectKey}", asset.PublicUrl);

        using var duplicateComplete = await PostWithCsrfAsync<object?>(
            client,
            $"/api/v1/admin/media/{intent.UploadId}/complete",
            null);
        Assert.Equal(HttpStatusCode.Conflict, duplicateComplete.StatusCode);

        var library = await client.GetFromJsonAsync<PagedResult<AdminMediaAssetDto>>(
            "/api/v1/admin/media?search=am-tu-sa&pageSize=48");
        Assert.NotNull(library);
        Assert.Contains(library.Items, item => item.Id == asset.Id);
    }

    [Fact]
    public async Task CompleteRejectsContentWhoseSignatureDoesNotMatchMimeType()
    {
        using var client = await CreateAuthenticatedClientAsync(_adminEmail);
        var bytes = "<svg><script>alert(1)</script></svg>"u8.ToArray();
        using var intentResponse = await PostWithCsrfAsync(
            client,
            "/api/v1/admin/media/upload-intent",
            new CreateMediaUploadIntentCommand("unsafe.png", "image/png", bytes.Length));
        var intent = Assert.IsType<MediaUploadIntentDto>(
            await intentResponse.Content.ReadFromJsonAsync<MediaUploadIntentDto>());
        _storage.Store(intent.ObjectKey, "image/png", bytes);

        using var completeResponse = await PostWithCsrfAsync<object?>(
            client,
            $"/api/v1/admin/media/{intent.UploadId}/complete",
            null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, completeResponse.StatusCode);
    }

    [Fact]
    public async Task UploadReturnsServiceUnavailableWhenStorageIsNotConfigured()
    {
        await using var unavailableFactory = new MediaApiFactory(
            PostgreSqlFixture.GetRequiredConnectionString(),
            new FakeObjectStorage(isConfigured: false));
        using var client = unavailableFactory.CreateMediaClient();
        await LoginAsync(client, _adminEmail);

        using var response = await PostWithCsrfAsync(
            client,
            "/api/v1/admin/media/upload-intent",
            new CreateMediaUploadIntentCommand("teapot.png", "image/png", 32));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = _factory.CreateMediaClient();
        await LoginAsync(client, email);
        return client;
    }

    private static async Task LoginAsync(HttpClient client, string email)
    {
        var csrf = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest(email, Password)),
        };
        request.Headers.Add(csrf.HeaderName, csrf.RequestToken);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostWithCsrfAsync<T>(
        HttpClient client,
        string uri,
        T body)
    {
        var csrf = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = JsonContent.Create(body),
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
        var result = await userManager.CreateAsync(user, Password);
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Code)));

        if (role is not null)
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            Assert.True(roleResult.Succeeded);
        }

        if (permission is not null)
        {
            var claimResult = await userManager.AddClaimAsync(
                user,
                new Claim(CustomClaimTypes.Permission, permission));
            Assert.True(claimResult.Succeeded);
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

    private static byte[] CreatePngHeader(int width, int height)
    {
        var bytes = new byte[32];
        byte[] signature = [137, 80, 78, 71, 13, 10, 26, 10];
        signature.CopyTo(bytes, 0);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(16, 4), width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(20, 4), height);
        return bytes;
    }

    private sealed class MediaApiFactory(
        string connectionString,
        IObjectStorageService storage) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Postgres", connectionString);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IObjectStorageService>();
                services.AddSingleton(storage);
            });
        }

        public HttpClient CreateMediaClient() => CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = true,
            });
    }

    private sealed class FakeObjectStorage(bool isConfigured = true) : IObjectStorageService
    {
        private readonly ConcurrentDictionary<string, StoredObject> _objects = new();

        public bool IsConfigured { get; } = isConfigured;

        public Task<PresignedObjectUpload> CreatePresignedUploadAsync(
            string objectKey,
            string contentType,
            DateTimeOffset expiresAtUtc,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PresignedObjectUpload(
                $"https://upload.integration.test/{objectKey}?signature=fake",
                new Dictionary<string, string> { ["Content-Type"] = contentType }));

        public Task<StoredObjectMetadata?> GetMetadataAsync(
            string objectKey,
            CancellationToken cancellationToken)
        {
            var found = _objects.TryGetValue(objectKey, out var value);
            return Task.FromResult<StoredObjectMetadata?>(found
                ? new StoredObjectMetadata(value!.Bytes.Length, value.ContentType, "fake-checksum")
                : null);
        }

        public Task<byte[]> ReadPrefixAsync(
            string objectKey,
            int maximumBytes,
            CancellationToken cancellationToken) =>
            Task.FromResult(_objects[objectKey].Bytes.Take(maximumBytes).ToArray());

        public Task DeleteIfExistsAsync(
            string objectKey,
            CancellationToken cancellationToken)
        {
            _objects.TryRemove(objectKey, out _);
            return Task.CompletedTask;
        }

        public string GetPublicUrl(string objectKey) =>
            $"https://media.integration.test/{objectKey}";

        public void Store(string objectKey, string contentType, byte[] bytes) =>
            _objects[objectKey] = new StoredObject(contentType, bytes);

        private sealed record StoredObject(string ContentType, byte[] Bytes);
    }
}
