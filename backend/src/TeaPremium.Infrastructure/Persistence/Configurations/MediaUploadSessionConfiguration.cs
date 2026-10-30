using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeaPremium.Domain.Media;

namespace TeaPremium.Infrastructure.Persistence.Configurations;

internal sealed class MediaUploadSessionConfiguration
    : IEntityTypeConfiguration<MediaUploadSession>
{
    public void Configure(EntityTypeBuilder<MediaUploadSession> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("media_upload_sessions", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_media_upload_sessions_expected_byte_size_positive",
                "expected_byte_size > 0");
            tableBuilder.HasCheckConstraint(
                "ck_media_upload_sessions_expiry_after_creation",
                "expires_at_utc > created_at_utc");
        });

        builder.HasKey(upload => upload.Id).HasName("pk_media_upload_sessions");
        builder.Property(upload => upload.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(upload => upload.ObjectKey)
            .HasColumnName("object_key")
            .HasMaxLength(1024)
            .IsRequired();
        builder.Property(upload => upload.OriginalFileName)
            .HasColumnName("original_file_name")
            .HasMaxLength(255)
            .IsRequired();
        builder.Property(upload => upload.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(255)
            .IsRequired();
        builder.Property(upload => upload.ExpectedByteSize)
            .HasColumnName("expected_byte_size")
            .IsRequired();
        builder.Property(upload => upload.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();
        builder.Property(upload => upload.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(upload => upload.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(upload => upload.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(upload => upload.CompletedAtUtc)
            .HasColumnName("completed_at_utc")
            .HasColumnType("timestamp with time zone");
        builder.Property(upload => upload.AssetId).HasColumnName("asset_id");

        builder.HasIndex(upload => upload.ObjectKey)
            .IsUnique()
            .HasDatabaseName("ux_media_upload_sessions_object_key");
        builder.HasIndex(upload => upload.AssetId)
            .IsUnique()
            .HasFilter("asset_id IS NOT NULL")
            .HasDatabaseName("ux_media_upload_sessions_asset_id_not_null");
        builder.HasIndex(upload => new { upload.Status, upload.ExpiresAtUtc })
            .HasDatabaseName("ix_media_upload_sessions_status_expires_at_utc");
        builder.HasIndex(upload => upload.CreatedByUserId)
            .HasDatabaseName("ix_media_upload_sessions_created_by_user_id");

        builder.HasOne(upload => upload.Asset)
            .WithOne()
            .HasForeignKey<MediaUploadSession>(upload => upload.AssetId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_media_upload_sessions_media_assets_asset_id");
    }
}
