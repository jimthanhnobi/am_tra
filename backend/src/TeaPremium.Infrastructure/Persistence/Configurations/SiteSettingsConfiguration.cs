using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeaPremium.Domain.Settings;

namespace TeaPremium.Infrastructure.Persistence.Configurations;

internal sealed class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> builder)
    {
        builder.ToTable("site_settings", table => table.HasCheckConstraint("ck_site_settings_singleton", "id = 1"));
        builder.HasKey(x => x.Id).HasName("pk_site_settings");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.SiteName).HasColumnName("site_name").HasMaxLength(160);
        builder.Property(x => x.SiteDescription).HasColumnName("site_description").HasMaxLength(500);
        builder.Property(x => x.BrandTagline).HasColumnName("brand_tagline").HasMaxLength(200);
        builder.Property(x => x.LogoAssetId).HasColumnName("logo_asset_id");
        builder.Property(x => x.FaviconAssetId).HasColumnName("favicon_asset_id");
        builder.Property(x => x.Hotline).HasColumnName("hotline").HasMaxLength(25);
        builder.Property(x => x.ZaloUrl).HasColumnName("zalo_url").HasMaxLength(2048);
        builder.Property(x => x.FacebookUrl).HasColumnName("facebook_url").HasMaxLength(2048);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(254);
        builder.Property(x => x.Address).HasColumnName("address").HasMaxLength(500);
        builder.Property(x => x.OpeningHours).HasColumnName("opening_hours").HasMaxLength(300);
        builder.Property(x => x.DefaultSeoTitle).HasColumnName("default_seo_title").HasMaxLength(200);
        builder.Property(x => x.DefaultMetaDescription).HasColumnName("default_meta_description").HasMaxLength(500);
        builder.Property(x => x.DefaultOgImageAssetId).HasColumnName("default_og_image_asset_id");
        builder.Property(x => x.PublicWebsiteUrl).HasColumnName("public_website_url").HasMaxLength(2048);
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
        builder.HasOne<TeaPremium.Domain.Media.MediaAsset>().WithMany().HasForeignKey(x => x.LogoAssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TeaPremium.Domain.Media.MediaAsset>().WithMany().HasForeignKey(x => x.FaviconAssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TeaPremium.Domain.Media.MediaAsset>().WithMany().HasForeignKey(x => x.DefaultOgImageAssetId).OnDelete(DeleteBehavior.Restrict);
    }
}
