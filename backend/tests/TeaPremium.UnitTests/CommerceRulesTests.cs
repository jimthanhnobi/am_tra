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
        order.Transition(SalesOrderStatus.Cancelled);
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
}
