using Microsoft.Net.Http.Headers;

namespace TeaPremium.Api.Middleware;

public sealed class AdminNoStoreMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Request.Path.StartsWithSegments("/api/v1/admin"))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderNames.CacheControl] = "no-store";
                context.Response.Headers[HeaderNames.Pragma] = "no-cache";
                return Task.CompletedTask;
            });
        }

        return next(context);
    }
}
