using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeaPremium.Application;
using TeaPremium.Application.Commerce;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Common;
using TeaPremium.Infrastructure;
using TeaPremium.Infrastructure.Persistence;

namespace TeaPremium.IntegrationTests;

[Collection(PostgreSqlTestGroup.Name)]
public sealed class CommerceSalesIntegrationTests(PostgreSqlFixture database)
{
    [Fact]
    public async Task OrderIdempotencyCompletionAndUnknownHistoryReportingUsePostgreSql()
    {
        ArgumentNullException.ThrowIfNull(database);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = PostgreSqlFixture.GetRequiredConnectionString(),
            }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var commerce = scope.ServiceProvider.GetRequiredService<CommerceService>();
        await using var tx = await db.Database.BeginTransactionAsync();

        var slug = "commerce-test-" + Guid.NewGuid().ToString("N");
        var product = new Product(slug, "Sản phẩm kiểm thử", "Giới thiệu sản phẩm",
            "{}", "<p>Chi tiết</p>", "test", "{}",
            new SeoMetadata("Sản phẩm kiểm thử", "Mô tả sản phẩm."));
        product.MoveToInReview();
        product.Publish(DateTimeOffset.UtcNow);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var suffix = Random.Shared.Next(10000000, 99999999).ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        var customer = await commerce.CreateCustomerAsync(new CustomerInput(
            "Khách kiểm thử", "09" + suffix, "legacy_import", "legacy_base",
            HadPriorPurchase: false, Tags: ["end_user"]), CancellationToken.None);

        var request = new CreateOrderInput(
            customer.Id, Guid.NewGuid(), null, "zalo_oa", "hang-moi-thang-11",
            [new OrderItemInput(product.Id, 2, 1200000m, 700000m)]);

        var first = await commerce.CreateOrderAsync(request, CancellationToken.None);
        var retry = await commerce.CreateOrderAsync(request, CancellationToken.None);
        Assert.Equal(first.Id, retry.Id);

        await Assert.ThrowsAsync<CommerceConflictException>(() =>
            commerce.CreateOrderAsync(request with
            {
                Items = [new OrderItemInput(product.Id, 2, 1300000m, 700000m)],
            }, CancellationToken.None));

        var before = await commerce.ReportAsync(
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1),
            CancellationToken.None);
        Assert.DoesNotContain(before.ByClosingChannel, x => x.Channel == "zalo_oa");

        await commerce.TransitionOrderAsync(first.Id, "complete", CancellationToken.None);
        var after = await commerce.ReportAsync(
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1),
            CancellationToken.None);
        Assert.Equal(1, after.CompletedOrders);
        Assert.Equal(2400000m, after.Revenue);
        Assert.Equal(1400000m, after.KnownCost);
        Assert.Equal(0, after.FirstTimeOrders);
        Assert.Equal(0, after.ReturningOrders);
        Assert.Equal(1, after.UnknownHistoryOrders);
        Assert.Contains(after.ByClosingChannel, x => x.Channel == "zalo_oa" &&
            x.Orders == 1 && x.Revenue == 2400000m);
        Assert.Contains(after.ByAcquisitionSource, x => x.Channel == "legacy_base" &&
            x.Orders == 1);

        // Isolated test DB; rollback cleans customer, product and order together.
        await tx.RollbackAsync();
    }
}
