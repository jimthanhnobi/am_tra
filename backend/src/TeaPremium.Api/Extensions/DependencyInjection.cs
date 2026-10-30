using System.Diagnostics;
using TeaPremium.Api.Configuration;
using TeaPremium.Api.HealthChecks;
using TeaPremium.Api.Routing;

namespace TeaPremium.Api.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

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

        services
            .AddHealthChecks()
            .AddCheck<SelfHealthCheck>(
                "self",
                tags: [HealthCheckTags.Live, HealthCheckTags.Ready]);

        return services;
    }
}

