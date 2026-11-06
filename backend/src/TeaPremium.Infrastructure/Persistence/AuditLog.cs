namespace TeaPremium.Infrastructure.Persistence;

public sealed class AuditLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAtUtc { get; init; }
    public Guid? ActorUserId { get; init; }
    public string EntityType { get; init; } = "";
    public Guid EntityId { get; init; }
    public string Action { get; init; } = "";
    public string ChangesJson { get; init; } = "{}";
    public string? CorrelationId { get; init; }
}
