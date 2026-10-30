using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TeaPremium.IntegrationTests;

public sealed class HealthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        var connectionString = PostgreSqlFixture.GetRequiredConnectionString();

        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Postgres", connectionString);
        })
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpointReturnsHealthyJson(string endpoint)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var response = await _client.GetAsync(endpoint, timeout.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));

        await using var responseStream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var document = await JsonDocument.ParseAsync(
            responseStream,
            cancellationToken: timeout.Token);

        Assert.Equal("Healthy", document.RootElement.GetProperty("status").GetString());
        Assert.NotEmpty(document.RootElement.GetProperty("checks").EnumerateArray());
    }

    [Fact]
    public async Task UnknownRouteReturnsProblemDetailsWithCorrelationId()
    {
        const string correlationId = "foundation-contract-test";
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/not-found");
        request.Headers.Add("X-Correlation-ID", correlationId);

        using var response = await _client.SendAsync(request, timeout.Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").Single());

        await using var responseStream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var document = await JsonDocument.ParseAsync(
            responseStream,
            cancellationToken: timeout.Token);

        Assert.Equal(404, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(
            correlationId,
            document.RootElement.GetProperty("correlationId").GetString());
    }
}

