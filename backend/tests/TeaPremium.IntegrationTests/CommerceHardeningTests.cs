using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeaPremium.Application;
using TeaPremium.Application.Catalog;
using TeaPremium.Application.Commerce;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Commerce;
using TeaPremium.Domain.Common;
using TeaPremium.Infrastructure;
using TeaPremium.Infrastructure.Persistence;

namespace TeaPremium.IntegrationTests;

[Collection(PostgreSqlTestGroup.Name)]
public sealed class CommerceHardeningTests(PostgreSqlFixture database)
{
    private static ServiceProvider Services()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:Postgres"] = PostgreSqlFixture.GetRequiredConnectionString() }).Build();
        var services = new ServiceCollection();
        services.AddLogging(); services.AddApplication(); services.AddInfrastructure(config);
        return services.BuildServiceProvider();
    }

    private static Product Product() => new("hardening-" + Guid.NewGuid().ToString("N"),
        "Test product", "Test summary", "{}", "<p>Test</p>", "test", "{}", new SeoMetadata(null, null));

    private static CustomerInput CustomerInput(string origin = "new") => new("Private customer",
        "09" + Random.Shared.Next(10000000, 99999999).ToString(System.Globalization.CultureInfo.InvariantCulture),
        origin, "unknown", false, []);

    [Fact]
    public async Task SellabilityReturningHistoryReturnsAndAuditAreConsistent()
    {
        ArgumentNullException.ThrowIfNull(database);
        await using var provider = Services();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<CommerceService>();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        await using var tx = await db.Database.BeginTransactionAsync();
        var product = Product(); db.Products.Add(product); await db.SaveChangesAsync();
        var input = CustomerInput();
        var customer = await service.CreateCustomerAsync(input, CancellationToken.None);
        var now = DateTimeOffset.UtcNow;
        var request = new CreateOrderInput(customer.Id, Guid.NewGuid(), null, "phone", null,
            [new(product.Id, 1, 150000m, 50000m)], now.AddDays(-3));
        var first = await service.CreateOrderAsync(request, CancellationToken.None);
        await Assert.ThrowsAsync<CatalogNotFoundException>(() => catalog.GetPublicProductAsync(product.Slug, CancellationToken.None));
        product.MoveToInReview(); product.Publish(now); product.Archive(now);
        await db.SaveChangesAsync();
        var second = await service.CreateOrderAsync(request with { RequestId = Guid.NewGuid() }, CancellationToken.None);
        await Assert.ThrowsAsync<CatalogNotFoundException>(() => catalog.GetPublicProductAsync(product.Slug, CancellationToken.None));
        product.SetSellable(false); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<CommerceInputException>(() => service.CreateOrderAsync(request with { RequestId = Guid.NewGuid() }, CancellationToken.None));
        product.ReturnToDraft(); Assert.Equal(PublicationStatus.Draft, product.Status);
        await db.SaveChangesAsync();

        // Both orders were created before either delivery; creation-time flags cannot classify the report.
        await service.TransitionOrderAsync(first.Id, "complete", CancellationToken.None, new(now.AddDays(-2)));
        await service.TransitionOrderAsync(second.Id, "complete", CancellationToken.None, new(now.AddDays(-1)));
        var report = await service.ReportAsync(now.AddDays(-3), now.AddDays(20), CancellationToken.None);
        Assert.Equal(1, report.FirstTimeOrders); Assert.Equal(1, report.ReturningOrders);
        var laterPeriod = await service.ReportAsync(now.AddHours(-36), now.AddDays(20), CancellationToken.None);
        Assert.Equal(0, laterPeriod.FirstTimeOrders); Assert.Equal(1, laterPeriod.ReturningOrders);
        await service.TransitionOrderAsync(second.Id, "return", CancellationToken.None, new(Reason: "Full return"));
        var adjusted = await service.ReportAsync(now.AddDays(-3), now.AddDays(20), CancellationToken.None);
        Assert.Equal(150000m, adjusted.Revenue); Assert.Equal(1, adjusted.CompletedOrders);
        var audits = await db.AuditLogs.Where(x => x.EntityId == second.Id && x.Action == "update").ToArrayAsync();
        Assert.Equal(2, audits.Length);
        Assert.Single(audits, x => x.ChangesJson.Contains("Returned", StringComparison.Ordinal));
        var customerAudit = await db.AuditLogs.SingleAsync(x => x.EntityId == customer.Id);
        Assert.DoesNotContain(input.Phone, customerAudit.ChangesJson, StringComparison.Ordinal);
        Assert.DoesNotContain(input.Name, customerAudit.ChangesJson, StringComparison.Ordinal);
        await tx.RollbackAsync();
    }

    [Fact]
    public async Task Import500RowsReportsErrorsAndCanBeRetriedWithoutDuplicates()
    {
        await using var provider = Services(); await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<CommerceService>();
        await using var tx = await db.Database.BeginTransactionAsync();
        var start = Random.Shared.Next(10000000, 99000000);
        var items = Enumerable.Range(0, 500).Select(i => new CustomerInput("Imported test " + i,
            "09" + (start + i).ToString(System.Globalization.CultureInfo.InvariantCulture),
            "legacy_import", "legacy_base", i % 2 == 0, [i % 2 == 0 ? "wholesale" : "end_user"], "Private note")).ToArray();
        items[5] = items[5] with { Phone = "0000000000" };
        items[10] = items[10] with { Name = "x" };
        items[20] = items[20] with { ImportError = "Invalid history value" };
        items[498] = items[0]; items[499] = items[1];
        var before = await db.Customers.CountAsync();
        var preview = await service.ImportCustomersAsync(new(true, items), CancellationToken.None);
        Assert.Equal(495, preview.NewCount); Assert.Equal(3, preview.InvalidCount); Assert.Equal(2, preview.DuplicateCount);
        Assert.Equal("invalid", preview.Rows.Single(x => x.Row == 7).Status);
        Assert.Equal(before, await db.Customers.CountAsync());
        var imported = await service.ImportCustomersAsync(new(false, items), CancellationToken.None);
        Assert.Equal(495, imported.NewCount);
        var retry = await service.ImportCustomersAsync(new(false, items), CancellationToken.None);
        Assert.Equal(0, retry.NewCount); Assert.Equal(495, retry.ExistingCount);
        Assert.Equal(before + 495, await db.Customers.CountAsync());
        var phone = CommerceService.NormalizePhone(items[0].Phone);
        var customer = await db.Customers.SingleAsync(x => x.NormalizedPhone == phone);
        Assert.True(customer.HadPriorPurchase); Assert.Contains("wholesale", customer.Tags);
        Assert.Single(await db.CustomerInteractions.Where(x => x.CustomerId == customer.Id).ToArrayAsync());
        await tx.RollbackAsync();
    }

    [Fact]
    public async Task ConcurrentCompleteAndCancelCannotOverwriteOneAnother()
    {
        await using var provider = Services();
        await using var setupScope = provider.CreateAsyncScope();
        var setup = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = setupScope.ServiceProvider.GetRequiredService<CommerceService>();
        var product = Product(); setup.Products.Add(product); await setup.SaveChangesAsync();
        var customer = await service.CreateCustomerAsync(CustomerInput(), CancellationToken.None);
        var order = await service.CreateOrderAsync(new(customer.Id, Guid.NewGuid(), null, "phone", null,
            [new(product.Id, 1, 100m, null)]), CancellationToken.None);
        try
        {
            await using var first = provider.CreateAsyncScope(); await using var second = provider.CreateAsyncScope();
            var a = first.ServiceProvider.GetRequiredService<AppDbContext>();
            var b = second.ServiceProvider.GetRequiredService<AppDbContext>();
            await a.SalesOrders.SingleAsync(x => x.Id == order.Id);
            await b.SalesOrders.SingleAsync(x => x.Id == order.Id);
            var attempts = await Task.WhenAll(
                Record.ExceptionAsync(() => first.ServiceProvider.GetRequiredService<CommerceService>().TransitionOrderAsync(order.Id, "complete", CancellationToken.None)),
                Record.ExceptionAsync(() => second.ServiceProvider.GetRequiredService<CommerceService>().TransitionOrderAsync(order.Id, "cancel", CancellationToken.None, new(Reason: "Customer cancelled"))));
            Assert.Single(attempts, x => x is null);
            Assert.Single(attempts, x => x is CommerceConflictException);
            Assert.Equal(1, await setup.AuditLogs.CountAsync(x => x.EntityId == order.Id && x.Action == "update"));
        }
        finally
        {
            await setup.SalesOrders.Where(x => x.Id == order.Id).ExecuteDeleteAsync();
            await setup.Customers.Where(x => x.Id == customer.Id).ExecuteDeleteAsync();
            await setup.Products.Where(x => x.Id == product.Id).ExecuteDeleteAsync();
        }
    }
}
