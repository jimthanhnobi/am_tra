using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaPremium.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261104070000_AddCommerceFoundation")]
public sealed class AddCommerceFoundation : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("customers", t => new
        {
            id = t.Column<Guid>("uuid", nullable: false),
            name = t.Column<string>("character varying(200)", maxLength: 200, nullable: false),
            phone = t.Column<string>("character varying(30)", maxLength: 30, nullable: false),
            normalized_phone = t.Column<string>("character varying(20)", maxLength: 20, nullable: false),
            origin = t.Column<string>("character varying(30)", maxLength: 30, nullable: false),
            acquisition_source = t.Column<string>("character varying(40)", maxLength: 40, nullable: false),
            had_prior_purchase = t.Column<bool>("boolean", nullable: false),
            tags = t.Column<string[]>("text[]", nullable: false),
            created_at_utc = t.Column<DateTimeOffset>("timestamp with time zone", nullable: false)
        }, constraints: t => t.PrimaryKey("pk_customers", x => x.id));
        m.CreateIndex("ux_customers_normalized_phone", "customers", "normalized_phone", unique: true);
        m.CreateIndex("ix_customers_created_at_utc", "customers", "created_at_utc");

        m.CreateTable("customer_interactions", t => new
        {
            id = t.Column<Guid>("uuid", nullable: false),
            customer_id = t.Column<Guid>("uuid", nullable: false),
            channel = t.Column<string>("character varying(30)", maxLength: 30, nullable: false),
            kind = t.Column<string>("character varying(30)", maxLength: 30, nullable: false),
            notes = t.Column<string>("character varying(2000)", maxLength: 2000, nullable: false),
            occurred_at_utc = t.Column<DateTimeOffset>("timestamp with time zone", nullable: false)
        }, constraints: t =>
        {
            t.PrimaryKey("pk_customer_interactions", x => x.id);
            t.ForeignKey("fk_interactions_customers", x => x.customer_id, "customers", "id", onDelete: ReferentialAction.Restrict);
        });
        m.CreateIndex("ix_customer_interactions_customer_time", "customer_interactions",
            new[] { "customer_id", "occurred_at_utc" });

        m.CreateTable("sales_orders", t => new
        {
            id = t.Column<Guid>("uuid", nullable: false),
            order_code = t.Column<string>("character varying(32)", maxLength: 32, nullable: false),
            request_id = t.Column<Guid>("uuid", nullable: false),
            customer_id = t.Column<Guid>("uuid", nullable: false),
            lead_id = t.Column<Guid>("uuid", nullable: true),
            closing_channel = t.Column<string>("character varying(30)", maxLength: 30, nullable: false),
            acquisition_source = t.Column<string>("character varying(40)", maxLength: 40, nullable: false),
            is_returning = t.Column<bool>("boolean", nullable: false),
            campaign_code = t.Column<string>("character varying(100)", maxLength: 100, nullable: true),
            status = t.Column<string>("character varying(20)", maxLength: 20, nullable: false),
            total_amount = t.Column<decimal>("numeric(18,2)", nullable: false),
            created_at_utc = t.Column<DateTimeOffset>("timestamp with time zone", nullable: false),
            completed_at_utc = t.Column<DateTimeOffset>("timestamp with time zone", nullable: true)
        }, constraints: t =>
        {
            t.PrimaryKey("pk_sales_orders", x => x.id);
            t.ForeignKey("fk_sales_orders_customers", x => x.customer_id, "customers", "id", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("fk_sales_orders_leads", x => x.lead_id, "leads", "id", onDelete: ReferentialAction.Restrict);
        });
        m.CreateIndex("ix_sales_orders_lead_id", "sales_orders", "lead_id");
        m.CreateIndex("ux_sales_orders_order_code", "sales_orders", "order_code", unique: true);
        m.CreateIndex("ux_sales_orders_request_id", "sales_orders", "request_id", unique: true);
        m.CreateIndex("ix_sales_orders_customer_time", "sales_orders", new[] { "customer_id", "created_at_utc" });
        m.CreateIndex("ix_sales_orders_completed", "sales_orders", new[] { "status", "completed_at_utc" });

        m.CreateTable("sales_order_items", t => new
        {
            id = t.Column<Guid>("uuid", nullable: false),
            sales_order_id = t.Column<Guid>("uuid", nullable: false),
            product_id = t.Column<Guid>("uuid", nullable: false),
            product_name = t.Column<string>("character varying(300)", maxLength: 300, nullable: false),
            sku = t.Column<string>("character varying(100)", maxLength: 100, nullable: true),
            quantity = t.Column<int>("integer", nullable: false),
            unit_price = t.Column<decimal>("numeric(18,2)", nullable: false),
            unit_cost = t.Column<decimal>("numeric(18,2)", nullable: true)
        }, constraints: t =>
        {
            t.PrimaryKey("pk_sales_order_items", x => x.id);
            t.ForeignKey("fk_sales_order_items_orders", x => x.sales_order_id, "sales_orders", "id", onDelete: ReferentialAction.Cascade);
            t.ForeignKey("fk_sales_order_items_products", x => x.product_id, "products", "id", onDelete: ReferentialAction.Restrict);
        });
        m.CreateIndex("ix_sales_order_items_sales_order_id", "sales_order_items", "sales_order_id");
        m.CreateIndex("ix_sales_order_items_product_id", "sales_order_items", "product_id");
    }

    protected override void Down(MigrationBuilder m)
    {
        m.DropTable("customer_interactions");
        m.DropTable("sales_order_items");
        m.DropTable("sales_orders");
        m.DropTable("customers");
    }
}
