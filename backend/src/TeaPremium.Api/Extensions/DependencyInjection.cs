using System.Diagnostics;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using TeaPremium.Api.Configuration;
using TeaPremium.Api.HealthChecks;
using TeaPremium.Api.Routing;
using TeaPremium.Api.Security;
using TeaPremium.Application.Security;

namespace TeaPremium.Api.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services
            .AddOptions<ApiMetadataOptions>()
            .Bind(configuration.GetSection(ApiMetadataOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Name), "API name is required.")
            .Validate(options => options.Version == "v1", "API version must be v1 for this contract.")
            .ValidateOnStart();

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions["traceId"] =
                    Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
                context.ProblemDetails.Extensions["correlationId"] =
                    context.HttpContext.TraceIdentifier;
            };
        });

        services.AddControllers(options =>
        {
            options.Conventions.Insert(0, new ApiVersionRouteConvention());
        });

        services.AddOpenApi("v1");

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = environment.IsDevelopment()
                ? AdminSecurityDefaults.DevelopmentAuthCookieName
                : AdminSecurityDefaults.ProductionAuthCookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.Path = "/";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = true;
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                },
                OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                },
            };
        });

        services.Configure<SecurityStampValidatorOptions>(options =>
            options.ValidationInterval = TimeSpan.FromMinutes(5));

        services.AddAntiforgery(options =>
        {
            options.HeaderName = AdminSecurityDefaults.AntiforgeryHeaderName;
            options.Cookie.Name = environment.IsDevelopment()
                ? AdminSecurityDefaults.DevelopmentAntiforgeryCookieName
                : AdminSecurityDefaults.ProductionAntiforgeryCookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.Path = "/";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
        });

        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
            {
                options.AddPolicy(
                    permission,
                    policy => policy.RequireClaim(CustomClaimTypes.Permission, permission));
            }
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                await Results.Problem(
                        statusCode: StatusCodes.Status429TooManyRequests,
                        title: "Quá nhiều lần thử đăng nhập.",
                        detail: "Vui lòng thử lại sau.")
                    .ExecuteAsync(context.HttpContext);
            };
            options.AddPolicy(
                AdminSecurityDefaults.LoginRateLimitPolicy,
                context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 10,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1),
                    }));
        });

        services
            .AddHealthChecks()
            .AddCheck<SelfHealthCheck>(
                "self",
                tags: [HealthCheckTags.Live, HealthCheckTags.Ready]);

        return services;
    }
}

