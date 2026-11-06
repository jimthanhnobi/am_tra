using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Common;
using TeaPremium.Domain.Media;
using TeaPremium.Domain.Seo;
using TeaPremium.Infrastructure.Persistence;

namespace TeaPremium.IntegrationTests;

[Collection(PostgreSqlTestGroup.Name)]
public sealed class CatalogDatabaseTests(PostgreSqlFixture database)
{
    [Fact]
    public async Task CurrentSchemaCreatesExpectedApplicationTablesAndIndexes()
    {
        await using var context = database.CreateContext();
        var tables = await ReadStringsAsync(
            context,
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_type = 'BASE TABLE'
              AND table_name <> '__EFMigrationsHistory'
            ORDER BY table_name;
            """);

        string[] expectedTables =
        [
            "article_categories",
            "article_category_links",
            "articles",
            "audit_logs",
            "brands",
            "categories",
            "contact_events",
            "customer_interactions",
            "customers",
            "identity_role_claims",
            "identity_roles",
            "identity_user_claims",
            "identity_user_logins",
            "identity_user_roles",
            "identity_user_tokens",
            "identity_users",
            "leads",
            "media_assets",
            "media_upload_sessions",
            "product_categories",
            "product_media",
            "products",
            "sales_order_items",
            "sales_orders",
            "seo_redirects",
            "site_settings",
        ];

        Assert.Equal(expectedTables, tables);

        var indexes = await ReadStringsAsync(
            context,
            """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'public'
            ORDER BY indexname;
            """);

        Assert.Contains("ux_products_slug", indexes);
        Assert.Contains("ux_products_sku_not_null", indexes);
        Assert.Contains("ux_product_media_one_cover_per_product", indexes);
        Assert.Contains("ix_product_media_product_id_sort_order", indexes);
        Assert.Contains("ix_categories_parent_id_sort_order", indexes);
        Assert.Contains("ux_articles_slug", indexes);
        Assert.Contains("ux_article_categories_slug", indexes);
        Assert.Contains("ux_contact_events_client_event_id", indexes);
        Assert.Contains("ux_seo_redirects_source_path", indexes);
        Assert.Contains("ux_customers_normalized_phone", indexes);
        Assert.Contains("ix_customer_interactions_customer_time", indexes);
        Assert.Contains("ux_sales_orders_request_id", indexes);
        Assert.Contains("ix_sales_orders_completed", indexes);
    }

    [Fact]
    public async Task RedirectSourcePathIsUniqueInPostgreSql()
    {
        await using var context = database.CreateContext();
        var suffix = Guid.NewGuid().ToString("N");
        var source = $"/san-pham/unique-old-{suffix}";
        context.SeoRedirects.Add(new SeoRedirect(source,
            $"/san-pham/unique-new-a-{suffix}", DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();
        context.SeoRedirects.Add(new SeoRedirect(source,
            $"/san-pham/unique-new-b-{suffix}", DateTimeOffset.UtcNow));
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var postgres = Assert.IsType<PostgresException>(error.GetBaseException());
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal("ux_seo_redirects_source_path", postgres.ConstraintName);
    }

    [Fact]
    public async Task ProductMediaRejectsSecondCoverForSameProduct()
    {
        await using var context = database.CreateContext();
        var suffix = Guid.NewGuid().ToString("N");
        var product = CreateProduct($"cover-{suffix}");
        var firstAsset = new MediaAsset(
            $"tests/{suffix}/first.webp",
            "image/webp",
            1_024,
            800,
            600);
        var secondAsset = new MediaAsset(
            $"tests/{suffix}/second.webp",
            "image/webp",
            2_048,
            1200,
            900);

        context.AddRange(product, firstAsset, secondAsset);
        context.ProductMedia.Add(new ProductMedia(product.Id, firstAsset.Id, "First cover", 0, true));
        await context.SaveChangesAsync();

        context.ProductMedia.Add(new ProductMedia(product.Id, secondAsset.Id, "Second cover", 1, true));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
        var postgresException = Assert.IsType<PostgresException>(exception.GetBaseException());

        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
        Assert.Equal("ux_product_media_one_cover_per_product", postgresException.ConstraintName);
    }

    [Fact]
    public async Task ProductsRejectJsonArraysForObjectColumns()
    {
        await using var context = database.CreateContext();
        var id = Guid.NewGuid();
        var suffix = id.ToString("N");

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO products
                    (id, slug, name, summary, description_jsonb, description_html,
                     status, product_type, specs_jsonb)
                VALUES
                    ({id}, {$"invalid-json-{suffix}"}, {"Invalid JSON test"}, {"Summary"},
                     jsonb_build_object(), {"<p>Description</p>"}, 'Draft', {"test"},
                     jsonb_build_array());
                """));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal("ck_products_specs_jsonb_object", exception.ConstraintName);
    }

    [Fact]
    public async Task DeletingPrimaryCategoryDoesNotCascadeToProduct()
    {
        await using var context = database.CreateContext();
        var suffix = Guid.NewGuid().ToString("N");
        var category = new Category(
            $"category-{suffix}",
            "Category",
            "<p>Category</p>",
            new SeoMetadata(null, null));
        var product = new Product(
            $"product-{suffix}",
            "Product",
            "Summary",
            "{}",
            "<p>Description</p>",
            "test",
            "{}",
            new SeoMetadata(null, null),
            primaryCategoryId: category.Id);

        context.AddRange(category, product);
        context.ProductCategories.Add(new ProductCategory(product.Id, category.Id));
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM categories WHERE id = {category.Id};"));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        Assert.Equal("fk_products_categories_primary_category_id", exception.ConstraintName);
    }

    private static Product CreateProduct(string slug) =>
        new(
            slug,
            "Integration test product",
            "Integration test summary",
            "{}",
            "<p>Integration test description</p>",
            "test",
            "{}",
            new SeoMetadata(null, null));

    private static async Task<string[]> ReadStringsAsync(
        AppDbContext context,
        string commandText)
    {
        var connection = context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();

        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values.ToArray();
    }
}
