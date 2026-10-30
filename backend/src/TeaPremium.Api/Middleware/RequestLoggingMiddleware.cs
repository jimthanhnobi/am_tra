using System.Diagnostics;

namespace TeaPremium.Api.Middleware;

public sealed partial class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            RequestCompleted(
                logger,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms")]
    private static partial void RequestCompleted(
        ILogger logger,
        string method,
        PathString path,
        int statusCode,
        double elapsedMilliseconds);
}
