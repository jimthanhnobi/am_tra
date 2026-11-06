using Microsoft.EntityFrameworkCore;
using Npgsql;
using TeaPremium.Application.Leads;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Leads;
using TeaPremium.Infrastructure.Persistence;

namespace TeaPremium.Infrastructure.Leads;

internal sealed class LeadStore(AppDbContext db) : ILeadStore
{
    public Task<bool> ProductExistsAsync(Guid id, CancellationToken ct) =>
        db.Products.AnyAsync(x => x.Id == id && x.Status == PublicationStatus.Published &&
            x.PrimaryCategory != null && x.PrimaryCategory.Status == PublicationStatus.Published, ct);
    public async Task SaveLeadAsync(Lead lead, CancellationToken ct)
    {
        db.Leads.Add(lead);
        await db.SaveChangesAsync(ct);
    }
    public async Task SaveEventIfNewAsync(ContactEvent contactEvent, CancellationToken ct)
    {
        if (await db.ContactEvents.AnyAsync(x => x.ClientEventId == contactEvent.ClientEventId, ct)) return;
        db.ContactEvents.Add(contactEvent);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "23505" })
        {
            db.Entry(contactEvent).State = EntityState.Detached;
        }
    }
    public async Task<LeadPage> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var total = await db.Leads.CountAsync(ct);
        var leads = await db.Leads.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new LeadPage(leads.Select(LeadService.Map).ToArray(), page, pageSize,
            total, (int)Math.Ceiling(total / (double)pageSize));
    }
    public Task<Lead?> FindAsync(Guid id, CancellationToken ct) => db.Leads.FindAsync([id], ct).AsTask();
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
