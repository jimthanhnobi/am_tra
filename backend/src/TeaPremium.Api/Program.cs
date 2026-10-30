using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using TeaPremium.Api.Configuration;
using TeaPremium.Api.Extensions;
using TeaPremium.Api.HealthChecks;
using TeaPremium.Api.Middleware;
using TeaPremium.Application;
using TeaPremium.Infrastructure;

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureStructuredLogging();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();
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
    app.MapOpenApi("/openapi/{documentName}.json");
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains(HealthCheckTags.Live),
    ResponseWriter = HealthResponseWriter.WriteAsync,
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains(HealthCheckTags.Ready),
    ResponseWriter = HealthResponseWriter.WriteAsync,
});

app.MapControllers();

app.Run();

public partial class Program;
