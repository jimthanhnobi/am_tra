using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TeaPremium.Api.Seeding;
using TeaPremium.Infrastructure.Persistence;
using TeaPremium.Api.Configuration;
using TeaPremium.Api.Extensions;
using TeaPremium.Api.HealthChecks;
using TeaPremium.Api.Middleware;
using TeaPremium.Application;
using TeaPremium.Application.Authentication;
using TeaPremium.Application.Content;
using TeaPremium.Application.Security;
using TeaPremium.Infrastructure;
using TeaPremium.Infrastructure.Identity;

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

var builder = WebApplication.CreateBuilder(args);

var trustedProxyIp = builder.Configuration["TRUSTED_PROXY_IP"]?.Trim();
if (!string.IsNullOrEmpty(trustedProxyIp))
{
    if (!IPAddress.TryParse(trustedProxyIp, out var proxyAddress))
        throw new InvalidOperationException("TRUSTED_PROXY_IP must be a single IP address.");
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownProxies.Add(proxyAddress);
    });
}

builder.ConfigureStructuredLogging();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration, builder.Environment);

var app = builder.Build();

if (args.Any(argument => string.Equals(
        argument,
        "bootstrap-admin",
        StringComparison.OrdinalIgnoreCase)))
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "bootstrap-admin is restricted to the Development environment.");
    }

    await using var scope = app.Services.CreateAsyncScope();
    var bootstrapper = scope.ServiceProvider.GetRequiredService<IAdminBootstrapper>();
    var result = await bootstrapper.BootstrapAsync();
    ApiLog.AdminBootstrapCompleted(
        app.Logger,
        result.UserCreated,
        result.RoleGranted);
    return;
}

if (args.Any(argument => string.Equals(argument, "seed-demo-article", StringComparison.OrdinalIgnoreCase)))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("seed-demo-article is restricted to Development.");

    await using var scope = app.Services.CreateAsyncScope();
    var store = scope.ServiceProvider.GetRequiredService<IArticleStore>();
    const string demoSlug = "bai-viet-mau-cho-moi-truong-phat-trien";
    if (!await store.SlugExistsAsync(demoSlug, null, CancellationToken.None))
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = (await users.GetUsersInRoleAsync(AdminRoles.Admin)).FirstOrDefault()
            ?? throw new InvalidOperationException("Bootstrap an Admin before seeding the demo article.");
        var articles = scope.ServiceProvider.GetRequiredService<ArticleService>();
        var created = await articles.CreateAsync(new ArticleInput(
            demoSlug,
            "Bài viết mẫu cho môi trường phát triển",
            "Bài viết mẫu dùng để kiểm tra trình biên tập và trang kiến thức.",
            """{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"Đây là nội dung mẫu để kiểm tra CMS. Hãy thay bằng nội dung đã được xác minh trước khi triển khai thật."}]}]}""",
            null, [], new ArticleSeoInput(
                "Bài viết mẫu cho môi trường phát triển",
                "Nội dung mẫu để kiểm tra CMS; không phải thông tin về hàng hóa hay xuất xứ.",
                null, true, null, null, null)), admin.Id, CancellationToken.None);
        await articles.TransitionAsync(created.Id, "review", CancellationToken.None);
        await articles.TransitionAsync(created.Id, "publish", CancellationToken.None);
    }
    return;
}

if (args.Any(argument => string.Equals(argument, "seed-demo-showcase", StringComparison.OrdinalIgnoreCase)))
{
    if (!app.Environment.IsDevelopment() ||
        !string.Equals(Environment.GetEnvironmentVariable("TEAPREMIUM_DEMO_SEED_CONFIRM"),
            "local-demo-only", StringComparison.Ordinal))
        throw new InvalidOperationException(
            "Demo seed is Development-only and requires TEAPREMIUM_DEMO_SEED_CONFIRM=local-demo-only.");

    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var settings = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString());
    var databaseName = settings.Database ?? string.Empty;
    if (settings.Host is not ("localhost" or "127.0.0.1" or "postgres") ||
        (databaseName != "teapremium" &&
            !databaseName.StartsWith("teapremium_demo", StringComparison.OrdinalIgnoreCase)))
        throw new InvalidOperationException("Demo seed requires a local teapremium/teapremium_demo PostgreSQL database.");

    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var admin = (await users.GetUsersInRoleAsync(AdminRoles.Admin)).FirstOrDefault()
        ?? throw new InvalidOperationException("Run bootstrap-admin before seed-demo-showcase.");
    var result = await DemoShowcaseSeeder.SeedAsync(db, admin.Id, CancellationToken.None);
    Console.WriteLine(result);
    return;
}

var apiMetadata = app.Services.GetRequiredService<IOptions<ApiMetadataOptions>>().Value;

ApiLog.StartingApi(
    app.Logger,
    apiMetadata.Name,
    apiMetadata.Version);

if (!string.IsNullOrEmpty(trustedProxyIp)) app.UseForwardedHeaders();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<AdminNoStoreMiddleware>();
app.UseExceptionHandler();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseStatusCodePages(async statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;
    var statusCode = httpContext.Response.StatusCode;

    await Results.Problem(
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            instance: httpContext.Request.Path)
        .ExecuteAsync(httpContext);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{documentName}.json").AllowAnonymous();
}
else
{
    app.UseHsts();
    // With a trusted TLS proxy, the API also receives private HTTP calls from
    // the Next BFF. The proxy owns the public HTTP-to-HTTPS redirect.
    if (string.IsNullOrEmpty(trustedProxyIp)) app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains(HealthCheckTags.Live),
    ResponseWriter = HealthResponseWriter.WriteAsync,
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains(HealthCheckTags.Ready),
    ResponseWriter = HealthResponseWriter.WriteAsync,
}).AllowAnonymous();

app.MapControllers();

app.Run();

public partial class Program;
