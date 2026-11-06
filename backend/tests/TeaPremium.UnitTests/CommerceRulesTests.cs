using TeaPremium.Application.Commerce;
using TeaPremium.Domain.Commerce;

namespace TeaPremium.UnitTests;

public sealed class CommerceRulesTests
{
    [Theory]
    [InlineData("0912345678", "+84912345678")]
    [InlineData("+84 912 345 678", "+84912345678")]
    [InlineData("84-912-345-678", "+84912345678")]
    public void PhoneNormalizationSupportsVietnameseNumbers(string input, string expected) =>
        Assert.Equal(expected, CommerceService.NormalizePhone(input));

    [Theory]
    [InlineData("123")]
    [InlineData("84912")]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0000000000")]
    [InlineData("abc0912345678")]
    public void RejectsInvalidPhones(string input) =>
        Assert.Throws<CommerceInputException>(() => CommerceService.NormalizePhone(input));

    [Fact]
    public void CustomerProfileUpdateNeverLosesKnownPurchaseHistory()
    {
        var c = new Customer("Khách thử", "0912345678", "+84912345678",
            "legacy_import", "legacy_base", true, ["vip"]);
        c.Update("Khách đã sửa", ["wholesale"], hadPriorPurchase: false);
        Assert.True(c.HadPriorPurchase);
        Assert.Equal("Khách đã sửa", c.Name);
        Assert.Equal("legacy_base", c.AcquisitionSource);
    }

    [Fact]
    public void ConfirmedOrderCanOnlyBeCompletedOnce()
    {
        var item = new SalesOrderItem(Guid.NewGuid(), "Ấm thử", "SKU-1", 2, 1200000m, 750000m);
        var order = new SalesOrder(Guid.NewGuid(), Guid.NewGuid(), null,
            "zalo_oa", "referral", false, "launch", [item]);
        Assert.Equal(2400000m, order.TotalAmount);
        Assert.Equal(SalesOrderStatus.Confirmed, order.Status);
        order.Transition(SalesOrderStatus.Completed);
        Assert.NotNull(order.CompletedAtUtc);
        Assert.Throws<InvalidOperationException>(() => order.Transition(SalesOrderStatus.Cancelled));
    }

    [Fact]
    public void CancelledOrderCannotBeRecompleted()
    {
        var item = new SalesOrderItem(Guid.NewGuid(), "Ấm thử", null, 1, 100000m, null);
        var order = new SalesOrder(Guid.NewGuid(), Guid.NewGuid(), null,
            "zalo_personal", "legacy_base", true, null, [item]);
        order.Transition(SalesOrderStatus.Cancelled, reason: "Customer cancelled before delivery.");
        Assert.Null(order.CompletedAtUtc);
        Assert.Throws<InvalidOperationException>(() => order.Transition(SalesOrderStatus.Completed));
    }

    [Fact]
    public void UnknownImportedPurchaseHistoryRemainsUnclassified()
    {
        var item = new SalesOrderItem(Guid.NewGuid(), "Ấm thử", null, 1, 500000m, null);
        var order = new SalesOrder(Guid.NewGuid(), Guid.NewGuid(), null,
            "zalo_personal", "legacy_base", null, null, [item]);
        Assert.Null(order.IsReturning);
    }

    [Fact]
    public void InvalidLineValuesAreRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SalesOrderItem(Guid.NewGuid(), "Ấm thử", null, 0, 200000m, null));

    [Fact]
    public void BusinessDatesAndOrderCodeUseVietnamMidnight()
    {
        var now = new DateTimeOffset(2026, 11, 30, 23, 0, 0, TimeSpan.Zero);
        Assert.Equal(new DateOnly(2026, 12, 1), TeaPremium.Domain.Common.BusinessCalendar.DateAt(now));
        Assert.Equal(new DateTimeOffset(2026, 11, 30, 17, 0, 0, TimeSpan.Zero),
            TeaPremium.Domain.Common.BusinessCalendar.StartOfDayUtc(new DateOnly(2026, 12, 1)));
        var order = new SalesOrder(Guid.NewGuid(), Guid.NewGuid(), null, "phone", "unknown", false, null,
            [new SalesOrderItem(Guid.NewGuid(), "Test", null, 1, 100m, null)], nowUtc: now);
        Assert.StartsWith("TP-20261201-", order.OrderCode, StringComparison.Ordinal);
    }

    [Fact]
    public void DeliveryCanBeBackdatedAndReturnedWithReason()
    {
        var now = new DateTimeOffset(2026, 11, 5, 10, 0, 0, TimeSpan.Zero);
        var order = new SalesOrder(Guid.NewGuid(), Guid.NewGuid(), null, "phone", "unknown", false, null,
            [new SalesOrderItem(Guid.NewGuid(), "Test", null, 1, 100m, null)], now.AddDays(-10), now);
        Assert.Throws<ArgumentException>(() => order.Transition(SalesOrderStatus.Completed, now.AddDays(1), nowUtc: now));
        Assert.Throws<ArgumentException>(() => order.Transition(SalesOrderStatus.Completed, now.AddDays(-11), nowUtc: now));
        order.Transition(SalesOrderStatus.Completed, now.AddDays(-5), nowUtc: now);
        Assert.Equal(now.AddDays(-5), order.CompletedAtUtc);
        Assert.Throws<ArgumentException>(() => order.Transition(SalesOrderStatus.Returned, nowUtc: now));
        order.Transition(SalesOrderStatus.Returned, reason: "Customer returned all goods", nowUtc: now);
        Assert.Equal(now, order.ReturnedAtUtc);
        Assert.Equal(now.AddDays(-5), order.CompletedAtUtc);
        Assert.Throws<InvalidOperationException>(() => order.Transition(SalesOrderStatus.Completed, nowUtc: now));
    }
}
