using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaPremium.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contact_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    page_path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consent_state = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contact_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_contact_events_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_page = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "New"),
                    utm_jsonb = table.Column<string>(type: "jsonb", nullable: false),
                    consent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leads", x => x.id);
                    table.CheckConstraint("ck_leads_status", "status IN ('New', 'Contacted', 'Closed')");
                    table.CheckConstraint("ck_leads_utm_jsonb_object", "jsonb_typeof(utm_jsonb) = 'object'");
                    table.ForeignKey(
                        name: "fk_leads_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contact_events_occurred_at",
                table: "contact_events",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "IX_contact_events_product_id",
                table: "contact_events",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_contact_events_client_event_id",
                table: "contact_events",
                column: "client_event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leads_product_id",
                table: "leads",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_leads_status_created_at",
                table: "leads",
                columns: new[] { "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contact_events");

            migrationBuilder.DropTable(
                name: "leads");
        }
    }
}
