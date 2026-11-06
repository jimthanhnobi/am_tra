using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Media;

namespace TeaPremium.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("categories", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_categories_status",
                "status IN ('Draft', 'InReview', 'Published', 'Archived')");
            tableBuilder.HasCheckConstraint(
                "ck_categories_sort_order_non_negative",
                "sort_order >= 0");
        });

        builder.HasKey(category => category.Id).HasName("pk_categories");

        builder.Property(category => category.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(category => category.ParentId)
            .HasColumnName("parent_id");
        builder.Property(category => category.Slug)
            .HasColumnName("slug")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(category => category.Name)
            .HasColumnName("name")
            .HasMaxLength(300)
            .IsRequired();
        builder.Property(category => category.DescriptionHtml)
            .HasColumnName("description_html")
            .HasColumnType("text")
            .IsRequired();
        builder.Property(category => category.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0)
            .IsRequired();
        builder.Property(category => category.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PublicationStatus.Draft)
            .IsRequired();
        builder.Property(category => category.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();
        builder.Property(category => category.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(category => category.Slug)
            .IsUnique()
            .HasDatabaseName("ux_categories_slug");
        builder.HasIndex(category => new { category.ParentId, category.SortOrder })
            .HasDatabaseName("ix_categories_parent_id_sort_order");

        builder.HasOne(category => category.Parent)
            .WithMany(category => category.Children)
            .HasForeignKey(category => category.ParentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_categories_categories_parent_id");

        builder.OwnsOne(category => category.Seo, seo =>
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
                .HasDatabaseName("ix_categories_og_image_asset_id");
            seo.HasOne<MediaAsset>()
                .WithMany()
                .HasForeignKey(metadata => metadata.OgImageAssetId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_categories_media_assets_og_image_asset_id");
        });

        builder.Navigation(category => category.Seo).IsRequired();
    }
}
