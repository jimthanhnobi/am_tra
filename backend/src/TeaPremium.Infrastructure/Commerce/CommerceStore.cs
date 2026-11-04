using Microsoft.EntityFrameworkCore;
using Npgsql;
using TeaPremium.Application.Commerce;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Commerce;
using TeaPremium.Infrastructure.Persistence;

namespace TeaPremium.Infrastructure.Commerce;

internal sealed class CommerceStore(AppDbContext db) : ICommerceStore
{
    public async Task<(IReadOnlyList<Customer> Items, int Count)> ListCustomersAsync(
        string? search, int skip, int take, CancellationToken ct)
    {
        var query = db.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var normalized = new string(term.Where(char.IsDigit).ToArray());
            query = query.Where(c => EF.Functions.ILike(c.Name, "%" + term + "%") ||
                c.Phone.Contains(term) || (normalized.Length > 2 && c.NormalizedPhone.Contains(normalized)));
        }
        return (await query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Skip(skip).Take(take).ToListAsync(ct), await query.CountAsync(ct));
    }

    public Task<Customer?> FindCustomerAsync(Guid id, CancellationToken ct) =>
        db.Customers.FindAsync([id], ct).AsTask();

    public Task<Customer?> FindByPhoneAsync(string normalizedPhone, CancellationToken ct) =>
        db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.NormalizedPhone == normalizedPhone, ct);

    public async Task AddCustomerAsync(Customer customer, CancellationToken ct)
    {
        db.Customers.Add(customer);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (IsUniqueViolation(e))
        {
            db.Entry(customer).State = EntityState.Detached;
            throw new CommerceConflictException("Customer phone already exists.");
        }
    }

    public async Task<(int Added, int Existing)> ImportCustomersAsync(
        IReadOnlyList<Customer> customers, bool dryRun, CancellationToken ct)
    {
        var numbers = customers.Select(x => x.NormalizedPhone).ToArray();
        var existing = await db.Customers.AsNoTracking().Where(x => numbers.Contains(x.NormalizedPhone))
            .Select(x => x.NormalizedPhone).ToListAsync(ct);
        var current = existing.ToHashSet(StringComparer.Ordinal);
        var pending = customers.Where(x => !current.Contains(x.NormalizedPhone)).ToArray();
        if (dryRun) return (pending.Length, current.Count);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        db.Customers.AddRange(pending);
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException e) when (IsUniqueViolation(e))
        {
            await transaction.RollbackAsync(ct);
            foreach (var customer in pending) db.Entry(customer).State = EntityState.Detached;
            throw new CommerceConflictException("Import conflicted with another customer update. Preview and retry.");
        }
        return (pending.Length, current.Count);
    }

    public Task UpdateCustomerAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task AddInteractionAsync(CustomerInteraction interaction, CancellationToken ct)
    {
        db.CustomerInteractions.Add(interaction);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CustomerInteraction>> ListInteractionsAsync(Guid customerId, CancellationToken ct) =>
        await db.CustomerInteractions.AsNoTracking().Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.OccurredAtUtc).Take(100).ToListAsync(ct);

    public async Task<SalesOrder> CreateOrderAsync(CreateOrderInput input, CancellationToken ct)
    {
        var previous = await db.SalesOrders.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.RequestId == input.RequestId, ct);
        if (previous is not null)
        {
            if (!SameRequest(previous, input))
                throw new CommerceConflictException("Request ID was reused with different order data.");
            return previous;
        }

        var customer = await db.Customers.FindAsync([input.CustomerId], ct)
            ?? throw new CommerceNotFoundException("Customer not found.");
        if (input.LeadId is Guid leadId)
        {
            var lead = await db.Leads.AsNoTracking().FirstOrDefaultAsync(x => x.Id == leadId, ct)
                ?? throw new CommerceNotFoundException("Lead not found.");
            if (!string.IsNullOrWhiteSpace(lead.Phone) &&
                CommerceService.NormalizePhone(lead.Phone) != customer.NormalizedPhone)
                throw new CommerceConflictException("Lead phone does not match customer.");
        }

        var ids = input.Items.Select(x => x.ProductId).Distinct().ToArray();
        var products = await db.Products.AsNoTracking()
            .Where(x => ids.Contains(x.Id) && x.Status == PublicationStatus.Published)
            .Select(x => new { x.Id, x.Name, x.Sku }).ToDictionaryAsync(x => x.Id, ct);
        if (products.Count != ids.Length) throw new CommerceInputException("All order products must be published.");
        var lines = input.Items.Select(x =>
            new SalesOrderItem(x.ProductId, products[x.ProductId].Name, products[x.ProductId].Sku,
                x.Quantity, x.UnitPrice, x.UnitCost)).ToArray();
        var hasConfirmedHistory = customer.HadPriorPurchase ||
            await db.SalesOrders.AnyAsync(x => x.CustomerId == customer.Id &&
                x.Status == SalesOrderStatus.Completed, ct);
        bool? returning = hasConfirmedHistory ? true : customer.Origin == "new" ? false : null;
        var order = new SalesOrder(customer.Id, input.RequestId, input.LeadId, input.ClosingChannel,
            customer.AcquisitionSource, returning, input.CampaignCode?.Trim(), lines);
        if (order.TotalAmount > 9999999999999999m)
            throw new CommerceInputException("Order total is too large.");
        db.SalesOrders.Add(order);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (IsUniqueViolation(e))
        {
            db.Entry(order).State = EntityState.Detached;
            var concurrent = await db.SalesOrders.AsNoTracking().Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.RequestId == input.RequestId, ct);
            if (concurrent is null || !SameRequest(concurrent, input))
                throw new CommerceConflictException("Order conflict. Retry with a new request ID.");
            return concurrent;
        }
        return order;
    }

    public async Task<(IReadOnlyList<SalesOrder> Items, int Count)> ListOrdersAsync(
        int skip, int take, CancellationToken ct)
    {
        var query = db.SalesOrders.AsNoTracking();
        var count = await query.CountAsync(ct);
        var orders = await query.Include(x => x.Items).OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id).Skip(skip).Take(take).ToListAsync(ct);
        return (orders, count);
    }

    public Task<SalesOrder?> FindOrderAsync(Guid id, CancellationToken ct) =>
        db.SalesOrders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task SaveOrderAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task<string> GetCustomerNameAsync(Guid id, CancellationToken ct) =>
        await db.Customers.Where(x => x.Id == id).Select(x => x.Name).FirstAsync(ct);

    public async Task<SalesReport> GetSalesReportAsync(DateTimeOffset from, DateTimeOffset untilUtc, CancellationToken ct)
    {
        // The sale is attributed once to the closing channel and once to the acquisition source
        // in separate breakdowns. Never sum those breakdowns together.
        var query = db.SalesOrders.AsNoTracking().Where(x =>
            x.Status == SalesOrderStatus.Completed &&
            x.CompletedAtUtc >= from && x.CompletedAtUtc < untilUtc);
        var summary = await query.GroupBy(_ => 1)
            .Select(g => new
            {
                Orders = g.Count(), Revenue = g.Sum(x => x.TotalAmount),
                NewCount = g.Count(x => x.IsReturning == false), ReturningCount = g.Count(x => x.IsReturning == true),
                UnknownCount = g.Count(x => x.IsReturning == null),
            }).FirstOrDefaultAsync(ct);

        var channels = await query.GroupBy(x => x.ClosingChannel)
            .Select(g => new ChannelMetric(g.Key, g.Count(), g.Sum(x => x.TotalAmount)))
            .OrderByDescending(x => x.Revenue).ToListAsync(ct);
        var sources = await query.GroupBy(x => x.AcquisitionSource)
            .Select(g => new ChannelMetric(g.Key, g.Count(), g.Sum(x => x.TotalAmount)))
            .OrderByDescending(x => x.Revenue).ToListAsync(ct);

        var lines = db.SalesOrderItems.AsNoTracking().Where(x =>
            db.SalesOrders.Any(o => o.Id == x.SalesOrderId &&
                o.Status == SalesOrderStatus.Completed &&
                o.CompletedAtUtc >= from && o.CompletedAtUtc < untilUtc));
        var knownCost = await lines.Where(x => x.UnitCost != null)
            .SumAsync(x => (decimal?)((decimal)x.UnitCost! * x.Quantity), ct) ?? 0m;
        var missing = await lines.CountAsync(x => x.UnitCost == null, ct);
        return new SalesReport(summary?.Orders ?? 0, summary?.Revenue ?? 0m,
            knownCost, missing, summary?.NewCount ?? 0, summary?.ReturningCount ?? 0,
            summary?.UnknownCount ?? 0, channels, sources);
    }

    private static bool SameRequest(SalesOrder existing, CreateOrderInput input)
    {
        if (existing.CustomerId != input.CustomerId ||
            existing.LeadId != input.LeadId ||
            existing.ClosingChannel != input.ClosingChannel ||
            existing.CampaignCode != input.CampaignCode?.Trim() ||
            existing.Items.Count != input.Items.Count) return false;

        var previous = existing.Items.OrderBy(x => x.ProductId).ThenBy(x => x.Quantity)
            .ThenBy(x => x.UnitPrice).ThenBy(x => x.UnitCost).ToArray();
        var incoming = input.Items.OrderBy(x => x.ProductId).ThenBy(x => x.Quantity)
            .ThenBy(x => x.UnitPrice).ThenBy(x => x.UnitCost).ToArray();
        for (var i = 0; i < previous.Length; i++)
        {
            if (previous[i].ProductId != incoming[i].ProductId ||
                previous[i].Quantity != incoming[i].Quantity ||
                previous[i].UnitPrice != incoming[i].UnitPrice ||
                previous[i].UnitCost != incoming[i].UnitCost) return false;
        }
        return true;
    }

    private static bool IsUniqueViolation(DbUpdateException error) =>
        error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
