using System.Globalization;
using TeaPremium.Domain.Common;
namespace TeaPremium.Domain.Commerce;

public enum SalesOrderStatus { Confirmed, Completed, Cancelled, Returned }

public sealed class SalesOrder
{
    private SalesOrder() { }

    public SalesOrder(Guid customerId, Guid requestId, Guid? leadId, string closingChannel,
        string acquisitionSource, bool? isReturning, string? campaignCode,
        IReadOnlyCollection<SalesOrderItem> items, DateTimeOffset? orderedAtUtc = null,
        DateTimeOffset? nowUtc = null)
    {
        if (items.Count is 0 or > 50) throw new ArgumentException("Order items count must be 1-50.");
        Id = Guid.NewGuid();
        var now = (nowUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
        OrderedAtUtc = (orderedAtUtc ?? now).ToUniversalTime();
        if (OrderedAtUtc > now || OrderedAtUtc < now.AddDays(-90))
            throw new ArgumentException("Order date must be within the past 90 days.");
        OrderCode = "TP-" + BusinessCalendar.DateAt(OrderedAtUtc).ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "-" +
            Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        RequestId = requestId;
        CustomerId = customerId;
        LeadId = leadId;
        ClosingChannel = closingChannel;
        AcquisitionSource = acquisitionSource;
        IsReturning = isReturning;
        CampaignCode = campaignCode;
        Status = SalesOrderStatus.Confirmed;
        CreatedAtUtc = now;
        Items = items.ToList();
        TotalAmount = Items.Sum(x => x.LineAmount);
    }

    public Guid Id { get; private set; }
    public string OrderCode { get; private set; } = "";
    public Guid RequestId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? LeadId { get; private set; }
    public string ClosingChannel { get; private set; } = "";
    public string AcquisitionSource { get; private set; } = "";
    public bool? IsReturning { get; private set; }
    public string? CampaignCode { get; private set; }
    public SalesOrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset OrderedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? ReturnedAtUtc { get; private set; }
    public string? StatusReason { get; private set; }
    public ICollection<SalesOrderItem> Items { get; private set; } = new List<SalesOrderItem>();

    public void Transition(SalesOrderStatus next, DateTimeOffset? occurredAtUtc = null,
        string? reason = null, DateTimeOffset? nowUtc = null)
    {
        var allowed = Status == SalesOrderStatus.Confirmed &&
            next is SalesOrderStatus.Completed or SalesOrderStatus.Cancelled ||
            Status == SalesOrderStatus.Completed && next == SalesOrderStatus.Returned;
        if (!allowed) throw new InvalidOperationException("This order transition is not allowed.");
        if (reason?.Trim().Length > 500) throw new ArgumentException("Reason must be at most 500 characters.");
        if (next is SalesOrderStatus.Cancelled or SalesOrderStatus.Returned &&
            (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500))
            throw new ArgumentException("Cancellation/return requires a reason of 1-500 characters.");
        var now = (nowUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var occurred = (occurredAtUtc ?? now).ToUniversalTime();
        if (occurred > now || occurred < OrderedAtUtc || occurred < now.AddDays(-90) ||
            next == SalesOrderStatus.Returned && occurred < CompletedAtUtc)
            throw new ArgumentException("Event date must follow the order/delivery date and be within the past 90 days.");
        Status = next;
        StatusReason = reason?.Trim();
        if (next == SalesOrderStatus.Completed) CompletedAtUtc = occurred;
        if (next == SalesOrderStatus.Returned) ReturnedAtUtc = occurred;
    }
}

public sealed class SalesOrderItem
{
    private SalesOrderItem() { }

    public SalesOrderItem(Guid productId, string productName, string? sku, int quantity,
        decimal unitPrice, decimal? unitCost)
    {
        if (quantity is < 1 or > 100000 || unitPrice < 0 || unitPrice > 999999999999m ||
            unitCost is < 0 or > 999999999999m)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Invalid quantity or money amount.");
        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
        Quantity = quantity;
        UnitPrice = unitPrice;
        UnitCost = unitCost;
    }

    public Guid Id { get; private set; }
    public Guid SalesOrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = "";
    public string? Sku { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal? UnitCost { get; private set; }
    public decimal LineAmount => Quantity * UnitPrice;
}
