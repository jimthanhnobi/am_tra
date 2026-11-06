using Microsoft.EntityFrameworkCore;
using TeaPremium.Domain.Commerce;

namespace TeaPremium.IntegrationTests;

[Collection(PostgreSqlTestGroup.Name)]
public sealed class CommerceDatabaseTests(PostgreSqlFixture database)
{
    [Fact]
    public async Task MigrationPersistsCustomersAndNotesAndEnforcesUniquePhones()
    {
        await using var db = database.CreateContext();
        await using var tx = await db.Database.BeginTransactionAsync();
        var number = "09" + Random.Shared.Next(10000000, 99999999).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var normalized = "+84" + number[1..];
        var first = new Customer("Khách kiểm thử", number, normalized,
            "legacy_import", "legacy_base", hadPriorPurchase: true, ["wholesale"]);
        db.Customers.Add(first);
        await db.SaveChangesAsync();

        var note = new CustomerInteraction(first.Id, "zalo_personal", "quote", "Đã tư vấn một mẫu ấm.");
        db.CustomerInteractions.Add(note);
        await db.SaveChangesAsync();

        var loaded = await db.Customers.AsNoTracking().SingleAsync(c => c.NormalizedPhone == normalized);
        Assert.Equal("legacy_base", loaded.AcquisitionSource);
        Assert.True(loaded.HadPriorPurchase);
        Assert.Contains("wholesale", loaded.Tags);
        Assert.Equal(1, await db.CustomerInteractions.CountAsync(x => x.CustomerId == first.Id));

        // Do not modify production or shared Development data: all test changes rollback.
        await tx.RollbackAsync();
    }
}
