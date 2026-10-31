using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Leads;

namespace TeaPremium.Infrastructure.Persistence.Configurations;

internal sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads", table =>
        {
            table.HasCheckConstraint("ck_leads_status", "status IN ('New', 'Contacted', 'Closed')");
            table.HasCheckConstraint("ck_leads_utm_jsonb_object", "jsonb_typeof(utm_jsonb) = 'object'");
        });
        builder.HasKey(x => x.Id).HasName("pk_leads");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(254);
        builder.Property(x => x.Message).HasColumnName("message").HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.SourcePage).HasColumnName("source_page").HasMaxLength(2048).IsRequired();
        builder.Property(x => x.Channel).HasColumnName("channel").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20)
            .HasDefaultValue(LeadStatus.New).IsRequired();
        builder.Property(x => x.UtmJson).HasColumnName("utm_jsonb").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ConsentAt).HasColumnName("consent_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_leads_products_product_id");
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc }).HasDatabaseName("ix_leads_status_created_at");
    }
}

internal sealed class ContactEventConfiguration : IEntityTypeConfiguration<ContactEvent>
{
    public void Configure(EntityTypeBuilder<ContactEvent> builder)
    {
        builder.ToTable("contact_events");
        builder.HasKey(x => x.Id).HasName("pk_contact_events");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ClientEventId).HasColumnName("client_event_id").IsRequired();
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Channel).HasColumnName("channel").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.PagePath).HasColumnName("page_path").HasMaxLength(2048).IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ConsentState).HasColumnName("consent_state").IsRequired();
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_contact_events_products_product_id");
        builder.HasIndex(x => x.ClientEventId).IsUnique().HasDatabaseName("ux_contact_events_client_event_id");
        builder.HasIndex(x => x.OccurredAt).HasDatabaseName("ix_contact_events_occurred_at");
    }
}
