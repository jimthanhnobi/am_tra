using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaPremium.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSeoRedirectsAndSiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "seo_redirects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_path = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    destination_path = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    redirect_type = table.Column<int>(type: "integer", nullable: false, defaultValue: 301),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seo_redirects", x => x.id);
                    table.CheckConstraint("ck_seo_redirects_distinct", "source_path <> destination_path");
                    table.CheckConstraint("ck_seo_redirects_local_paths", "source_path ~ '^/(san-pham|danh-muc|kien-thuc)/[a-z0-9]+(-[a-z0-9]+)*$' AND destination_path ~ '^/(san-pham|danh-muc|kien-thuc)/[a-z0-9]+(-[a-z0-9]+)*$'");
                    table.CheckConstraint("ck_seo_redirects_type", "redirect_type = 301");
                });

            migrationBuilder.CreateTable(
                name: "site_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    site_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    site_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    brand_tagline = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    logo_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    favicon_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hotline = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    zalo_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    facebook_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    opening_hours = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    default_seo_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    default_meta_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    default_og_image_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    public_website_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_settings", x => x.id);
                    table.CheckConstraint("ck_site_settings_singleton", "id = 1");
                    table.ForeignKey(
                        name: "FK_site_settings_media_assets_default_og_image_asset_id",
                        column: x => x.default_og_image_asset_id,
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_site_settings_media_assets_favicon_asset_id",
                        column: x => x.favicon_asset_id,
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_site_settings_media_assets_logo_asset_id",
                        column: x => x.logo_asset_id,
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_seo_redirects_destination_path",
                table: "seo_redirects",
                column: "destination_path");

            migrationBuilder.CreateIndex(
                name: "ux_seo_redirects_source_path",
                table: "seo_redirects",
                column: "source_path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_site_settings_default_og_image_asset_id",
                table: "site_settings",
                column: "default_og_image_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_site_settings_favicon_asset_id",
                table: "site_settings",
                column: "favicon_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_site_settings_logo_asset_id",
                table: "site_settings",
                column: "logo_asset_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "seo_redirects");

            migrationBuilder.DropTable(
                name: "site_settings");
        }
    }
}
