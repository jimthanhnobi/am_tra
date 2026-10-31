using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaPremium.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "article_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "articles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    excerpt = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    content_jsonb = table.Column<string>(type: "jsonb", nullable: false),
                    content_html = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cover_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    seo_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    meta_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    canonical_override = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    robots_index = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    og_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    og_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    og_image_asset_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_articles", x => x.id);
                    table.CheckConstraint("ck_articles_content_jsonb_object", "jsonb_typeof(content_jsonb) = 'object'");
                    table.CheckConstraint("ck_articles_status", "status IN ('Draft', 'InReview', 'Published', 'Archived')");
                    table.ForeignKey(
                        name: "fk_articles_media_assets_cover_asset_id",
                        column: x => x.cover_asset_id,
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_articles_media_assets_og_image_asset_id",
                        column: x => x.og_image_asset_id,
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_articles_users_author_id",
                        column: x => x.author_id,
                        principalTable: "identity_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "article_category_links",
                columns: table => new
                {
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_category_links", x => new { x.article_id, x.category_id });
                    table.ForeignKey(
                        name: "fk_article_category_links_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_article_category_links_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "article_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_article_categories_slug",
                table: "article_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_article_category_links_category_id",
                table: "article_category_links",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_articles_author_id",
                table: "articles",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_articles_cover_asset_id",
                table: "articles",
                column: "cover_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_articles_og_image_asset_id",
                table: "articles",
                column: "og_image_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_articles_status_published_at",
                table: "articles",
                columns: new[] { "status", "published_at" });

            migrationBuilder.CreateIndex(
                name: "ux_articles_slug",
                table: "articles",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_category_links");

            migrationBuilder.DropTable(
                name: "articles");

            migrationBuilder.DropTable(
                name: "article_categories");
        }
    }
}
