using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeaPremium.Domain.Commerce;

namespace TeaPremium.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("customers");
        b.HasKey(x => x.Id).HasName("pk_customers");
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        b.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(30).IsRequired();
        b.Property(x => x.NormalizedPhone).HasColumnName("normalized_phone").HasMaxLength(20).IsRequired();
        b.Property(x => x.Origin).HasColumnName("origin").HasMaxLength(30).IsRequired();
        b.Property(x => x.AcquisitionSource).HasColumnName("acquisition_source").HasMaxLength(40).IsRequired();
        b.Property(x => x.HadPriorPurchase).HasColumnName("had_prior_purchase");
        b.Property(x => x.Tags).HasColumnName("tags").HasColumnType("text[]").IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        b.HasIndex(x => x.NormalizedPhone).IsUnique().HasDatabaseName("ux_customers_normalized_phone");
        b.HasIndex(x => x.CreatedAtUtc).HasDatabaseName("ix_customers_created_at_utc");
    }
}

internal sealed class CustomerInteractionConfiguration : IEntityTypeConfiguration<CustomerInteraction>
{
    public void Configure(EntityTypeBuilder<CustomerInteraction> b)
    {
        b.ToTable("customer_interactions");
        b.HasKey(x => x.Id).HasName("pk_customer_interactions");
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.Channel).HasColumnName("channel").HasMaxLength(30).IsRequired();
        b.Property(x => x.Kind).HasColumnName("kind").HasMaxLength(30).IsRequired();
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(2000).IsRequired();
        b.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc").HasColumnType("timestamp with time zone");
        b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_interactions_customers");
        b.HasIndex(x => new { x.CustomerId, x.OccurredAtUtc })
            .HasDatabaseName("ix_customer_interactions_customer_time");
    }
}

internal sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> b)
    {
        b.ToTable("sales_orders");
        b.HasKey(x => x.Id).HasName("pk_sales_orders");
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.OrderCode).HasColumnName("order_code").HasMaxLength(32).IsRequired();
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.LeadId).HasColumnName("lead_id");
        b.Property(x => x.ClosingChannel).HasColumnName("closing_channel").HasMaxLength(30).IsRequired();
        b.Property(x => x.AcquisitionSource).HasColumnName("acquisition_source").HasMaxLength(40).IsRequired();
        b.Property(x => x.IsReturning).HasColumnName("is_returning");
        b.Property(x => x.CampaignCode).HasColumnName("campaign_code").HasMaxLength(100);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 2);
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        b.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc").HasColumnType("timestamp with time zone");
        b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_sales_orders_customers");
        b.HasOne<TeaPremium.Domain.Leads.Lead>().WithMany().HasForeignKey(x => x.LeadId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_sales_orders_leads");
        b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_sales_order_items_orders");
        b.HasIndex(x => x.LeadId).HasDatabaseName("ix_sales_orders_lead_id");
        b.HasIndex(x => x.OrderCode).IsUnique().HasDatabaseName("ux_sales_orders_order_code");
        b.HasIndex(x => x.RequestId).IsUnique().HasDatabaseName("ux_sales_orders_request_id");
        b.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc }).HasDatabaseName("ix_sales_orders_customer_time");
        b.HasIndex(x => new { x.Status, x.CompletedAtUtc }).HasDatabaseName("ix_sales_orders_completed");
    }
}

internal sealed class SalesOrderItemConfiguration : IEntityTypeConfiguration<SalesOrderItem>
{
    public void Configure(EntityTypeBuilder<SalesOrderItem> b)
    {
        b.ToTable("sales_order_items");
        b.HasKey(x => x.Id).HasName("pk_sales_order_items");
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.SalesOrderId).HasColumnName("sales_order_id");
        b.Property(x => x.ProductId).HasColumnName("product_id");
        b.Property(x => x.ProductName).HasColumnName("product_name").HasMaxLength(300).IsRequired();
        b.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(100);
        b.Property(x => x.Quantity).HasColumnName("quantity");
        b.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
        b.Property(x => x.UnitCost).HasColumnName("unit_cost").HasPrecision(18, 2);
        b.Ignore(x => x.LineAmount);
        b.HasOne<TeaPremium.Domain.Catalog.Product>().WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_sales_order_items_products");
        b.HasIndex(x => x.SalesOrderId).HasDatabaseName("ix_sales_order_items_sales_order_id");
        b.HasIndex(x => x.ProductId).HasDatabaseName("ix_sales_order_items_product_id");
    }
}
