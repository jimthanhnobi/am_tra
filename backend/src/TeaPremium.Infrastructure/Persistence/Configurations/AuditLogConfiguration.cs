using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeaPremium.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc");
        b.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        b.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(80);
        b.Property(x => x.EntityId).HasColumnName("entity_id");
        b.Property(x => x.Action).HasColumnName("action").HasMaxLength(20);
        b.Property(x => x.ChangesJson).HasColumnName("changes").HasColumnType("jsonb");
        b.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(64);
        b.HasIndex(x => new { x.EntityId, x.OccurredAtUtc });
        b.HasIndex(x => x.OccurredAtUtc);
    }
}
