using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeaPremium.Domain.Seo;

namespace TeaPremium.Infrastructure.Persistence.Configurations;

internal sealed class SeoRedirectConfiguration : IEntityTypeConfiguration<SeoRedirect>
{
    public void Configure(EntityTypeBuilder<SeoRedirect> builder)
    {
        builder.ToTable("seo_redirects", table =>
        {
            table.HasCheckConstraint("ck_seo_redirects_type", "redirect_type = 301");
            table.HasCheckConstraint("ck_seo_redirects_distinct", "source_path <> destination_path");
            table.HasCheckConstraint("ck_seo_redirects_local_paths", "source_path ~ '^/(san-pham|danh-muc|kien-thuc)/[a-z0-9]+(-[a-z0-9]+)*$' AND destination_path ~ '^/(san-pham|danh-muc|kien-thuc)/[a-z0-9]+(-[a-z0-9]+)*$'");
        });
        builder.HasKey(x => x.Id).HasName("pk_seo_redirects");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.SourcePath).HasColumnName("source_path").HasMaxLength(220).IsRequired();
        builder.Property(x => x.DestinationPath).HasColumnName("destination_path").HasMaxLength(220).IsRequired();
        builder.Property(x => x.RedirectType).HasColumnName("redirect_type").HasDefaultValue(301).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(x => x.SourcePath).IsUnique().HasDatabaseName("ux_seo_redirects_source_path");
        builder.HasIndex(x => x.DestinationPath).HasDatabaseName("ix_seo_redirects_destination_path");
    }
}
