using Microsoft.Extensions.DependencyInjection;

namespace TeaPremium.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}

/// <summary>
/// Provides a stable reference to the Application assembly for architecture tests.
/// </summary>
public static class ApplicationAssemblyReference;

