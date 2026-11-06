using Microsoft.EntityFrameworkCore;
using TeaPremium.Application.Security;

namespace TeaPremium.Infrastructure.Persistence;

internal sealed class AuditQuery(AppDbContext db) : IAuditQuery
{
    public async Task<IReadOnlyList<AuditEntryDto>> ListAsync(Guid? entityId, int page, CancellationToken cancellationToken)
    {
        var query = db.AuditLogs.AsNoTracking();
        if (entityId.HasValue) query = query.Where(x => x.EntityId == entityId.Value);
        return await query.OrderByDescending(x => x.OccurredAtUtc).ThenBy(x => x.Id)
            .Skip((page - 1) * 50).Take(50)
            .Select(x => new AuditEntryDto(x.Id, x.OccurredAtUtc, x.ActorUserId, x.EntityType,
                x.EntityId, x.Action, x.ChangesJson, x.CorrelationId)).ToArrayAsync(cancellationToken);
    }
}
