using Microsoft.EntityFrameworkCore;
using TeaPremium.Infrastructure.Persistence;

namespace TeaPremium.IntegrationTests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    public const string ConnectionStringEnvironmentVariable =
        "TEAPREMIUM_TEST_CONNECTION_STRING";

    private readonly DbContextOptions<AppDbContext> _options;

    public PostgreSqlFixture()
    {
        var connectionString = GetRequiredConnectionString();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    public static string GetRequiredConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Set {ConnectionStringEnvironmentVariable} to an isolated PostgreSQL " +
                "development/test database. These tests intentionally do not use EF InMemory.");
        }

        return connectionString;
    }

    public AppDbContext CreateContext() => new(_options);

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlTestGroup : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL";
}
