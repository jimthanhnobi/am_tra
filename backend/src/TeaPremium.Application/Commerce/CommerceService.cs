using TeaPremium.Domain.Commerce;

namespace TeaPremium.Application.Commerce;

public sealed record CustomerInput(string Name, string Phone, string Origin, string AcquisitionSource,
    bool HadPriorPurchase, string[]? Tags);
public sealed record CustomerDto(Guid Id, string Name, string Phone, string Origin,
    string AcquisitionSource, bool HadPriorPurchase, string[] Tags, DateTimeOffset CreatedAtUtc);
public sealed record CustomerPage(IReadOnlyList<CustomerDto> Items, int Page, int PageSize, int TotalItems);
public sealed record ImportCustomersInput(bool DryRun, IReadOnlyList<CustomerInput> Items);
public sealed record ImportCustomersResult(int InputCount, int NewCount, int ExistingCount, bool DryRun);
public sealed record InteractionInput(string Channel, string Kind, string Notes);
public sealed record InteractionDto(Guid Id, string Channel, string Kind, string Notes, DateTimeOffset OccurredAtUtc);
public sealed record OrderItemInput(Guid ProductId, int Quantity, decimal UnitPrice, decimal? UnitCost);
public sealed record CreateOrderInput(Guid CustomerId, Guid RequestId, Guid? LeadId,
    string ClosingChannel, string? CampaignCode, IReadOnlyList<OrderItemInput> Items);
public sealed record OrderItemDto(Guid ProductId, string ProductName, string? Sku,
    int Quantity, decimal UnitPrice, decimal? UnitCost, decimal LineAmount);
public sealed record OrderDto(Guid Id, string OrderCode, Guid CustomerId, string CustomerName,
    Guid? LeadId, string ClosingChannel, string AcquisitionSource, bool? IsReturning,
    string? CampaignCode, string Status, decimal TotalAmount, DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc, IReadOnlyList<OrderItemDto> Items);
public sealed record OrderPage(IReadOnlyList<OrderDto> Items, int Page, int PageSize, int TotalItems);
public sealed record ChannelMetric(string Channel, int Orders, decimal Revenue);
public sealed record SalesReport(int CompletedOrders, decimal Revenue, decimal KnownCost,
    int UnknownCostLines, int FirstTimeOrders, int ReturningOrders, int UnknownHistoryOrders,
    IReadOnlyList<ChannelMetric> ByClosingChannel, IReadOnlyList<ChannelMetric> ByAcquisitionSource);

public interface ICommerceStore
{
    Task<(IReadOnlyList<Customer> Items, int Count)> ListCustomersAsync(string? search, int skip, int take, CancellationToken ct);
    Task<Customer?> FindCustomerAsync(Guid id, CancellationToken ct);
    Task<Customer?> FindByPhoneAsync(string normalizedPhone, CancellationToken ct);
    Task AddCustomerAsync(Customer customer, CancellationToken ct);
    Task<(int Added, int Existing)> ImportCustomersAsync(IReadOnlyList<Customer> customers, bool dryRun, CancellationToken ct);
    Task UpdateCustomerAsync(CancellationToken ct);
    Task AddInteractionAsync(CustomerInteraction interaction, CancellationToken ct);
    Task<IReadOnlyList<CustomerInteraction>> ListInteractionsAsync(Guid customerId, CancellationToken ct);
    Task<SalesOrder> CreateOrderAsync(CreateOrderInput input, CancellationToken ct);
    Task<(IReadOnlyList<SalesOrder> Items, int Count)> ListOrdersAsync(int skip, int take, CancellationToken ct);
    Task<SalesOrder?> FindOrderAsync(Guid id, CancellationToken ct);
    Task SaveOrderAsync(CancellationToken ct);
    Task<SalesReport> GetSalesReportAsync(DateTimeOffset from, DateTimeOffset untilUtc, CancellationToken ct);
    Task<string> GetCustomerNameAsync(Guid id, CancellationToken ct);
}

public sealed class CommerceInputException(string message) : Exception(message);
public sealed class CommerceConflictException(string message) : Exception(message);
public sealed class CommerceNotFoundException(string message) : Exception(message);

public sealed class CommerceService(ICommerceStore store)
{
    private static readonly HashSet<string> Origins = ["legacy_import", "new", "unknown"];
    private static readonly HashSet<string> Sources =
        ["unknown", "legacy_base", "google_organic", "zalo_oa", "zalo_personal",
         "referral", "facebook", "direct", "other"];
    private static readonly HashSet<string> Channels =
        ["zalo_oa", "zalo_personal", "website", "phone", "in_person", "other"];
    private static readonly HashSet<string> Kinds = ["inquiry", "follow_up", "quote", "note"];

    public static string NormalizePhone(string? phone)
    {
        var raw = (phone ?? "").Trim();
        var compact = new string(raw.Where(char.IsDigit).ToArray());
        if (compact.Length == 10 && compact.StartsWith('0'))
            return "+84" + compact[1..];
        if (compact.Length == 11 && compact.StartsWith("84", StringComparison.Ordinal))
            return "+" + compact;
        throw new CommerceInputException("Phone must be a valid 10-digit Vietnamese number.");
    }

    private static Customer BuildCustomer(CustomerInput input)
    {
        var name = input.Name?.Trim() ?? "";
        if (name.Length is < 2 or > 200) throw new CommerceInputException("Customer name must be 2-200 characters.");
        if (!Origins.Contains(input.Origin) || !Sources.Contains(input.AcquisitionSource))
            throw new CommerceInputException("Unknown customer origin/acquisition source.");
        var phone = input.Phone?.Trim() ?? "";
        var normalized = NormalizePhone(phone);
        var tags = (input.Tags ?? []).Select(x => x.Trim().ToLowerInvariant()).Distinct().ToArray();
        if (tags.Length > 12 || tags.Any(x => x.Length is < 1 or > 40 || !x.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_')))
            throw new CommerceInputException("Tags must be short lowercase slugs (max 12).");
        return new Customer(name, phone, normalized, input.Origin, input.AcquisitionSource,
            input.HadPriorPurchase, tags);
    }

    public static CustomerDto Map(Customer c) =>
        new(c.Id, c.Name, c.Phone, c.Origin, c.AcquisitionSource, c.HadPriorPurchase, c.Tags, c.CreatedAtUtc);

    public async Task<CustomerPage> ListCustomersAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        ValidatePage(page, pageSize);
        var result = await store.ListCustomersAsync(search?.Trim(), (page - 1) * pageSize, pageSize, ct);
        return new CustomerPage(result.Items.Select(Map).ToArray(), page, pageSize, result.Count);
    }

    public async Task<CustomerDto> GetCustomerAsync(Guid id, CancellationToken ct) =>
        Map(await store.FindCustomerAsync(id, ct) ?? throw new CommerceNotFoundException("Customer not found."));

    public async Task<CustomerDto> CreateCustomerAsync(CustomerInput input, CancellationToken ct)
    {
        var customer = BuildCustomer(input);
        if (await store.FindByPhoneAsync(customer.NormalizedPhone, ct) is not null)
            throw new CommerceConflictException("Phone already belongs to a customer.");
        await store.AddCustomerAsync(customer, ct);
        return Map(customer);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(Guid id, CustomerInput input, CancellationToken ct)
    {
        var current = await store.FindCustomerAsync(id, ct) ?? throw new CommerceNotFoundException("Customer not found.");
        var candidate = BuildCustomer(input);
        if (candidate.NormalizedPhone != current.NormalizedPhone ||
            candidate.Origin != current.Origin || candidate.AcquisitionSource != current.AcquisitionSource)
            throw new CommerceConflictException("Phone, origin and acquisition source cannot be changed through profile edit.");
        current.Update(candidate.Name, candidate.Tags, candidate.HadPriorPurchase);
        await store.UpdateCustomerAsync(ct);
        return Map(current);
    }

    public async Task<ImportCustomersResult> ImportCustomersAsync(ImportCustomersInput input, CancellationToken ct)
    {
        if (input.Items is null || input.Items.Count is < 1 or > 1000)
            throw new CommerceInputException("Import requires 1-1000 rows.");
        var customers = input.Items.Select(BuildCustomer).ToArray();
        var distinct = customers.Select(c => c.NormalizedPhone).Distinct().Count();
        if (distinct != customers.Length) throw new CommerceInputException("Import contains duplicate phone numbers.");
        var (added, existing) = await store.ImportCustomersAsync(customers, input.DryRun, ct);
        return new ImportCustomersResult(customers.Length, added, existing, input.DryRun);
    }

    public async Task<InteractionDto> AddInteractionAsync(Guid customerId, InteractionInput input, CancellationToken ct)
    {
        if (!Channels.Contains(input.Channel) || !Kinds.Contains(input.Kind) ||
            string.IsNullOrWhiteSpace(input.Notes) || input.Notes.Length > 2000)
            throw new CommerceInputException("Invalid interaction.");
        if (await store.FindCustomerAsync(customerId, ct) is null)
            throw new CommerceNotFoundException("Customer not found.");
        var interaction = new CustomerInteraction(customerId, input.Channel, input.Kind, input.Notes.Trim());
        await store.AddInteractionAsync(interaction, ct);
        return new InteractionDto(interaction.Id, interaction.Channel, interaction.Kind,
            interaction.Notes, interaction.OccurredAtUtc);
    }

    public async Task<IReadOnlyList<InteractionDto>> GetInteractionsAsync(Guid customerId, CancellationToken ct)
    {
        if (await store.FindCustomerAsync(customerId, ct) is null) throw new CommerceNotFoundException("Customer not found.");
        return (await store.ListInteractionsAsync(customerId, ct))
            .Select(x => new InteractionDto(x.Id, x.Channel, x.Kind, x.Notes, x.OccurredAtUtc)).ToArray();
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderInput input, CancellationToken ct)
    {
        if (input.CustomerId == Guid.Empty || input.RequestId == Guid.Empty ||
            !Channels.Contains(input.ClosingChannel) || input.Items is null ||
            input.Items.Count is < 1 or > 50 ||
            input.Items.Any(x => x.ProductId == Guid.Empty || x.Quantity is < 1 or > 100000 ||
                x.UnitPrice < 0 || x.UnitPrice > 999999999999m ||
                x.UnitCost is < 0 or > 999999999999m) ||
            input.CampaignCode?.Length > 100)
            throw new CommerceInputException("Invalid order data.");
        var order = await store.CreateOrderAsync(input, ct);
        return await MapOrderAsync(order, ct);
    }

    public async Task<OrderPage> ListOrdersAsync(int page, int pageSize, CancellationToken ct)
    {
        ValidatePage(page, pageSize);
        var result = await store.ListOrdersAsync((page - 1) * pageSize, pageSize, ct);
        var items = new List<OrderDto>(result.Items.Count);
        foreach (var order in result.Items) items.Add(await MapOrderAsync(order, ct));
        return new OrderPage(items, page, pageSize, result.Count);
    }

    public async Task<OrderDto> GetOrderAsync(Guid id, CancellationToken ct) =>
        await MapOrderAsync(await store.FindOrderAsync(id, ct) ??
            throw new CommerceNotFoundException("Order not found."), ct);

    public async Task<OrderDto> TransitionOrderAsync(Guid id, string action, CancellationToken ct)
    {
        var order = await store.FindOrderAsync(id, ct) ?? throw new CommerceNotFoundException("Order not found.");
        var next = action switch
        {
            "complete" => SalesOrderStatus.Completed,
            "cancel" => SalesOrderStatus.Cancelled,
            _ => throw new CommerceInputException("Unknown order action."),
        };
        try { order.Transition(next); }
        catch (InvalidOperationException e) { throw new CommerceConflictException(e.Message); }
        await store.SaveOrderAsync(ct);
        return await MapOrderAsync(order, ct);
    }

    public Task<SalesReport> ReportAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        if (from >= to || to - from > TimeSpan.FromDays(366) || to > DateTimeOffset.UtcNow.AddDays(1))
            throw new CommerceInputException("Report range must be within one year.");
        return store.GetSalesReportAsync(from.ToUniversalTime(), to.ToUniversalTime(), ct);
    }

    private async Task<OrderDto> MapOrderAsync(SalesOrder o, CancellationToken ct) =>
        new(o.Id, o.OrderCode, o.CustomerId, await store.GetCustomerNameAsync(o.CustomerId, ct),
            o.LeadId, o.ClosingChannel, o.AcquisitionSource, o.IsReturning,
            o.CampaignCode, o.Status.ToString(), o.TotalAmount, o.CreatedAtUtc, o.CompletedAtUtc,
            o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Sku, i.Quantity,
                i.UnitPrice, i.UnitCost, i.LineAmount)).ToArray());

    private static void ValidatePage(int page, int size)
    {
        if (page < 1 || size is < 1 or > 50) throw new CommerceInputException("Invalid pagination.");
    }
}
