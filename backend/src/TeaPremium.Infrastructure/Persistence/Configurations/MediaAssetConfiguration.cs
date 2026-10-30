using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeaPremium.Domain.Media;

namespace TeaPremium.Infrastructure.Persistence.Configurations;

internal sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("media_assets", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("ck_media_assets_byte_size_positive", "byte_size > 0");
            tableBuilder.HasCheckConstraint("ck_media_assets_width_positive", "width > 0");
            tableBuilder.HasCheckConstraint("ck_media_assets_height_positive", "height > 0");
        });

        builder.HasKey(asset => asset.Id).HasName("pk_media_assets");

        builder.Property(asset => asset.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(asset => asset.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(1024)
            .IsRequired();
        builder.Property(asset => asset.MimeType)
            .HasColumnName("mime_type")
            .HasMaxLength(255)
            .IsRequired();
        builder.Property(asset => asset.ByteSize)
            .HasColumnName("byte_size")
            .IsRequired();
        builder.Property(asset => asset.Width)
            .HasColumnName("width")
            .IsRequired();
        builder.Property(asset => asset.Height)
            .HasColumnName("height")
            .IsRequired();
        builder.Property(asset => asset.Checksum)
            .HasColumnName("checksum")
            .HasMaxLength(128);
        builder.Property(asset => asset.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(asset => asset.StorageKey)
            .IsUnique()
            .HasDatabaseName("ux_media_assets_storage_key");
    }
}
