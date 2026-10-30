using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TeaPremium.Api.HealthChecks;

public sealed class SelfHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}

