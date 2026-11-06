using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaPremium.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenCommerceOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ordered_at_utc",
                table: "sales_orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            // Existing orders retain their original business date; never leave the CLR minimum date.
            migrationBuilder.Sql("UPDATE sales_orders SET ordered_at_utc = created_at_utc;");
            migrationBuilder.Sql("ALTER TABLE sales_orders ALTER COLUMN ordered_at_utc DROP DEFAULT;");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "returned_at_utc",
                table: "sales_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status_reason",
                table: "sales_orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "sales_orders",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<bool>(
                name: "is_sellable",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "products",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "customers",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    changes = table.Column<string>(type: "jsonb", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_orders_completed",
                table: "sales_orders",
                sql: "(status IN ('Completed','Returned')) = (completed_at_utc IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_orders_money",
                table: "sales_orders",
                sql: "total_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_orders_returned",
                table: "sales_orders",
                sql: "(status = 'Returned') = (returned_at_utc IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_orders_status",
                table: "sales_orders",
                sql: "status IN ('Confirmed','Completed','Cancelled','Returned')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_items_price",
                table: "sales_order_items",
                sql: "unit_price >= 0 AND (unit_cost IS NULL OR unit_cost >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_order_items_quantity",
                table: "sales_order_items",
                sql: "quantity BETWEEN 1 AND 100000");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity_id_occurred_at_utc",
                table: "audit_logs",
                columns: new[] { "entity_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_occurred_at_utc",
                table: "audit_logs",
                column: "occurred_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_orders_completed",
                table: "sales_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_orders_money",
                table: "sales_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_orders_returned",
                table: "sales_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_orders_status",
                table: "sales_orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_items_price",
                table: "sales_order_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_order_items_quantity",
                table: "sales_order_items");

            migrationBuilder.DropColumn(
                name: "ordered_at_utc",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "returned_at_utc",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "status_reason",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "is_sellable",
                table: "products");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "products");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "customers");
        }
    }
}
