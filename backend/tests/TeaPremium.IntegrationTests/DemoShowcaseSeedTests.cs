using Microsoft.EntityFrameworkCore;
using TeaPremium.Api.Seeding;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Commerce;
using TeaPremium.Infrastructure.Identity;

namespace TeaPremium.IntegrationTests;

[Collection(PostgreSqlTestGroup.Name)]
public sealed class DemoShowcaseSeedTests(PostgreSqlFixture database)
{
    [Fact]
    public async Task RepeatedDemoSeedCreatesLinkedRecordsOnlyOnceAndOnlyCompletedOrdersCount()
    {
        await using var db = database.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "demo-seed-test-" + Guid.NewGuid().ToString("N"),
            Email = "demo-test@example.invalid",
            SecurityStamp = Guid.NewGuid().ToString("N"),
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var first = await DemoShowcaseSeeder.SeedAsync(db, admin.Id, CancellationToken.None,
            manageOwnTransaction: false);
        Assert.Contains("7 products", first);
        Assert.Equal(7, await db.Products.CountAsync(p => p.Slug.StartsWith("demo-")));
        Assert.Equal(5, await db.Categories.CountAsync(c => c.Slug.StartsWith("demo-")));
        Assert.Equal(9, await db.MediaAssets.CountAsync(a => a.StorageKey.StartsWith("demo-products/")));
        Assert.Equal(3, await db.Articles.CountAsync(a => a.Slug.StartsWith("demo-")));
        Assert.Equal(8, await db.Customers.CountAsync(c => c.Name.StartsWith("[DEMO]")));
        Assert.Equal(9, await db.SalesOrders.CountAsync(o =>
            o.CampaignCode != null && o.CampaignCode.StartsWith("demo-")));
        Assert.Equal(4, await db.Leads.CountAsync(l => l.Message.StartsWith("[DEMO]")));

        var product = await db.Products.Include(p => p.Media).Include(p => p.ProductCategories)
            .SingleAsync(p => p.Slug == "demo-am-tu-sa-tay-thi");
        Assert.Equal(PublicationStatus.Published, product.Status);
        Assert.Equal(2, product.Media.Count);
        Assert.Single(product.Media.Where(m => m.IsCover));
        Assert.Single(product.ProductCategories);
        Assert.All(product.Media, media => Assert.Contains("demo", media.AltText, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(7, await db.SalesOrders.CountAsync(o =>
            o.CampaignCode != null && o.CampaignCode.StartsWith("demo-") &&
            o.Status == SalesOrderStatus.Completed));
        Assert.Equal(1, await db.SalesOrders.CountAsync(o =>
            o.CampaignCode != null && o.CampaignCode.StartsWith("demo-") &&
            o.Status == SalesOrderStatus.Confirmed));

        await DemoShowcaseSeeder.SeedAsync(db, admin.Id, CancellationToken.None,
            manageOwnTransaction: false);
        Assert.Equal(7, await db.Products.CountAsync(p => p.Slug.StartsWith("demo-")));
        Assert.Equal(9, await db.MediaAssets.CountAsync(a => a.StorageKey.StartsWith("demo-products/")));
        Assert.Equal(3, await db.Articles.CountAsync(a => a.Slug.StartsWith("demo-")));
        Assert.Equal(8, await db.Customers.CountAsync(c => c.Name.StartsWith("[DEMO]")));
        Assert.Equal(9, await db.SalesOrders.CountAsync(o =>
            o.CampaignCode != null && o.CampaignCode.StartsWith("demo-")));
        Assert.Equal(4, await db.Leads.CountAsync(l => l.Message.StartsWith("[DEMO]")));

        // Test data and test user remain entirely inside PostgreSQL rollback.
        await transaction.RollbackAsync();
    }
}
