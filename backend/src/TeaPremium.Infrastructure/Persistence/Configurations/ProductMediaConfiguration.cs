using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeaPremium.Domain.Media;

namespace TeaPremium.Infrastructure.Persistence.Configurations;

internal sealed class ProductMediaConfiguration : IEntityTypeConfiguration<ProductMedia>
{
    public void Configure(EntityTypeBuilder<ProductMedia> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("product_media", tableBuilder =>
            tableBuilder.HasCheckConstraint(
                "ck_product_media_sort_order_non_negative",
                "sort_order >= 0"));

        builder.HasKey(media => media.Id).HasName("pk_product_media");

        builder.Property(media => media.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(media => media.ProductId)
            .HasColumnName("product_id");
        builder.Property(media => media.AssetId)
            .HasColumnName("asset_id");
        builder.Property(media => media.AltText)
            .HasColumnName("alt_text")
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(media => media.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0)
            .IsRequired();
        builder.Property(media => media.IsCover)
            .HasColumnName("is_cover")
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(media => media.AssetId)
            .HasDatabaseName("ix_product_media_asset_id");
        builder.HasIndex(media => new { media.ProductId, media.SortOrder })
            .HasDatabaseName("ix_product_media_product_id_sort_order");
        builder.HasIndex(media => media.ProductId)
            .IsUnique()
            .HasFilter("is_cover = TRUE")
            .HasDatabaseName("ux_product_media_one_cover_per_product");

        builder.HasOne(media => media.Product)
            .WithMany(product => product.Media)
            .HasForeignKey(media => media.ProductId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_product_media_products_product_id");
        builder.HasOne(media => media.Asset)
            .WithMany(asset => asset.ProductMedia)
            .HasForeignKey(media => media.AssetId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_media_media_assets_asset_id");
    }
}
