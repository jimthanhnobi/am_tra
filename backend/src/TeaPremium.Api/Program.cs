using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using TeaPremium.Api.Configuration;
using TeaPremium.Api.Extensions;
using TeaPremium.Api.HealthChecks;
using TeaPremium.Api.Middleware;
using TeaPremium.Application;
using TeaPremium.Application.Authentication;
using TeaPremium.Infrastructure;

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

var builder = WebApplication.CreateBuilder(args);

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

var apiMetadata = app.Services.GetRequiredService<IOptions<ApiMetadataOptions>>().Value;

ApiLog.StartingApi(
    app.Logger,
    apiMetadata.Name,
    apiMetadata.Version);

app.UseMiddleware<CorrelationIdMiddleware>();
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
    app.UseHttpsRedirection();
}

app.UseRouting();
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
