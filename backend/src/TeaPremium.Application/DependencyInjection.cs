using Ganss.Xss;
using Microsoft.Extensions.DependencyInjection;
using TeaPremium.Application.Catalog;
using TeaPremium.Application.Content;
using TeaPremium.Application.Leads;
using TeaPremium.Application.Media;
using TeaPremium.Application.Settings;

namespace TeaPremium.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<HtmlSanitizer>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ArticleContentRenderer>();
        services.AddScoped<ArticleService>();
        services.AddScoped<LeadService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<SiteSettingsService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}

/// <summary>
/// Provides a stable reference to the Application assembly for architecture tests.
/// </summary>
public static class ApplicationAssemblyReference;

