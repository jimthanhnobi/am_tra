using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TeaPremium.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaUploadSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "media_upload_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    object_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    expected_byte_size = table.Column<long>(type: "bigint", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media_upload_sessions", x => x.id);
                    table.CheckConstraint("ck_media_upload_sessions_expected_byte_size_positive", "expected_byte_size > 0");
                    table.CheckConstraint("ck_media_upload_sessions_expiry_after_creation", "expires_at_utc > created_at_utc");
                    table.ForeignKey(
                        name: "fk_media_upload_sessions_media_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "identity_role_claims",
                columns: new[] { "id", "claim_type", "claim_value", "role_id" },
                values: new object[,]
                {
                    { 24, "permission", "Media.Write", new Guid("10000000-0000-0000-0000-000000000001") },
                    { 25, "permission", "Media.Write", new Guid("10000000-0000-0000-0000-000000000002") },
                    { 26, "permission", "Media.Write", new Guid("10000000-0000-0000-0000-000000000003") }
                });

            migrationBuilder.CreateIndex(
                name: "ix_media_upload_sessions_created_by_user_id",
                table: "media_upload_sessions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_media_upload_sessions_status_expires_at_utc",
                table: "media_upload_sessions",
                columns: new[] { "status", "expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_media_upload_sessions_asset_id_not_null",
                table: "media_upload_sessions",
                column: "asset_id",
                unique: true,
                filter: "asset_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_media_upload_sessions_object_key",
                table: "media_upload_sessions",
                column: "object_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media_upload_sessions");

            migrationBuilder.DeleteData(
                table: "identity_role_claims",
                keyColumn: "id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "identity_role_claims",
                keyColumn: "id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "identity_role_claims",
                keyColumn: "id",
                keyValue: 26);
        }
    }
}
