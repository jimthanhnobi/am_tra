using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Media;

namespace TeaPremium.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("products", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_products_status",
                "status IN ('Draft', 'InReview', 'Published', 'Archived')");
            tableBuilder.HasCheckConstraint(
                "ck_products_description_jsonb_object",
                "jsonb_typeof(description_jsonb) = 'object'");
            tableBuilder.HasCheckConstraint(
                "ck_products_specs_jsonb_object",
                "jsonb_typeof(specs_jsonb) = 'object'");
            tableBuilder.HasCheckConstraint(
                "ck_products_capacity_ml_positive",
                "capacity_ml IS NULL OR capacity_ml > 0");
        });

        builder.HasKey(product => product.Id).HasName("pk_products");

        builder.Property(product => product.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(product => product.Sku)
            .HasColumnName("sku")
            .HasMaxLength(100);
        builder.Property(product => product.Slug)
            .HasColumnName("slug")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(product => product.Name)
            .HasColumnName("name")
            .HasMaxLength(300)
            .IsRequired();
        builder.Property(product => product.Summary)
            .HasColumnName("summary")
            .HasMaxLength(1000)
            .IsRequired();
        builder.Property(product => product.DescriptionJson)
            .HasColumnName("description_jsonb")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(product => product.DescriptionHtml)
            .HasColumnName("description_html")
            .HasColumnType("text")
            .IsRequired();
        builder.Property(product => product.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PublicationStatus.Draft)
            .IsRequired();
        builder.Property(product => product.PrimaryCategoryId)
            .HasColumnName("primary_category_id");
        builder.Property(product => product.BrandId)
            .HasColumnName("brand_id");
        builder.Property(product => product.ProductType)
            .HasColumnName("product_type")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(product => product.MaterialText)
            .HasColumnName("material_text")
            .HasMaxLength(300);
        builder.Property(product => product.CapacityMl)
            .HasColumnName("capacity_ml");
        builder.Property(product => product.SpecsJson)
            .HasColumnName("specs_jsonb")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(product => product.PublishedAt)
            .HasColumnName("published_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(product => product.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();
        builder.Property(product => product.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(product => product.Slug)
            .IsUnique()
            .HasDatabaseName("ux_products_slug");
        builder.HasIndex(product => product.Sku)
            .IsUnique()
            .HasFilter("sku IS NOT NULL")
            .HasDatabaseName("ux_products_sku_not_null");
        builder.HasIndex(product => new { product.Status, product.PublishedAt })
            .HasDatabaseName("ix_products_status_published_at");
        builder.HasIndex(product => product.PrimaryCategoryId)
            .HasDatabaseName("ix_products_primary_category_id");
        builder.HasIndex(product => product.BrandId)
            .HasDatabaseName("ix_products_brand_id");

        builder.HasOne(product => product.PrimaryCategory)
            .WithMany(category => category.PrimaryProducts)
            .HasForeignKey(product => product.PrimaryCategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_products_categories_primary_category_id");
        builder.HasOne(product => product.Brand)
            .WithMany(brand => brand.Products)
            .HasForeignKey(product => product.BrandId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_products_brands_brand_id");

        builder.OwnsOne(product => product.Seo, seo =>
        {
            seo.Property(metadata => metadata.SeoTitle)
                .HasColumnName("seo_title")
                .HasMaxLength(200);
            seo.Property(metadata => metadata.MetaDescription)
                .HasColumnName("meta_description")
                .HasMaxLength(500);
            seo.Property(metadata => metadata.CanonicalOverride)
                .HasColumnName("canonical_override")
                .HasMaxLength(2048);
            seo.Property(metadata => metadata.RobotsIndex)
                .HasColumnName("robots_index")
                .HasDefaultValue(true)
                .IsRequired();
            seo.Property(metadata => metadata.OgTitle)
                .HasColumnName("og_title")
                .HasMaxLength(200);
            seo.Property(metadata => metadata.OgDescription)
                .HasColumnName("og_description")
                .HasMaxLength(500);
            seo.Property(metadata => metadata.OgImageAssetId)
                .HasColumnName("og_image_asset_id");

            seo.HasIndex(metadata => metadata.OgImageAssetId)
                .HasDatabaseName("ix_products_og_image_asset_id");
            seo.HasOne<MediaAsset>()
                .WithMany()
                .HasForeignKey(metadata => metadata.OgImageAssetId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_products_media_assets_og_image_asset_id");
        });

        builder.Navigation(product => product.Seo).IsRequired();
    }
}
