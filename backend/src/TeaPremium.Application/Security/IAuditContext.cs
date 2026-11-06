namespace TeaPremium.Application.Security;

public interface IAuditContext
{
    Guid? ActorUserId { get; }
    string? CorrelationId { get; }
}

public sealed record AuditEntryDto(Guid Id, DateTimeOffset OccurredAtUtc, Guid? ActorUserId,
    string EntityType, Guid EntityId, string Action, string ChangesJson, string? CorrelationId);

public interface IAuditQuery
{
    Task<IReadOnlyList<AuditEntryDto>> ListAsync(Guid? entityId, int page, CancellationToken cancellationToken);
}
