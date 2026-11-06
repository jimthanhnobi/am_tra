using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TeaPremium.Api.HealthChecks;

public static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(report);

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMilliseconds = report.TotalDuration.TotalMilliseconds,
            traceId = Activity.Current?.TraceId.ToString(),
            correlationId = context.TraceIdentifier,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMilliseconds = entry.Value.Duration.TotalMilliseconds,
            }),
        };

        return context.Response.WriteAsJsonAsync(
            payload,
            cancellationToken: context.RequestAborted);
    }
}

