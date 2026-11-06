using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Content;
using TeaPremium.Domain.Media;
using TeaPremium.Infrastructure.Identity;

namespace TeaPremium.Infrastructure.Persistence.Configurations;

internal sealed class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("articles", table =>
        {
            table.HasCheckConstraint("ck_articles_status", "status IN ('Draft', 'InReview', 'Published', 'Archived')");
            table.HasCheckConstraint("ck_articles_content_jsonb_object", "jsonb_typeof(content_jsonb) = 'object'");
        });
        builder.HasKey(x => x.Id).HasName("pk_articles");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(x => x.Excerpt).HasColumnName("excerpt").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ContentJson).HasColumnName("content_jsonb").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ContentHtml).HasColumnName("content_html").HasColumnType("text").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20)
            .HasDefaultValue(PublicationStatus.Draft).IsRequired();
        builder.Property(x => x.AuthorId).HasColumnName("author_id").IsRequired();
        builder.Property(x => x.CoverAssetId).HasColumnName("cover_asset_id");
        builder.Property(x => x.PublishedAt).HasColumnName("published_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("ux_articles_slug");
        builder.HasIndex(x => new { x.Status, x.PublishedAt }).HasDatabaseName("ix_articles_status_published_at");
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_articles_users_author_id");
        builder.HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.CoverAssetId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_articles_media_assets_cover_asset_id");
        builder.OwnsOne(x => x.Seo, seo =>
        {
            seo.Property(x => x.SeoTitle).HasColumnName("seo_title").HasMaxLength(200);
            seo.Property(x => x.MetaDescription).HasColumnName("meta_description").HasMaxLength(500);
            seo.Property(x => x.CanonicalOverride).HasColumnName("canonical_override").HasMaxLength(2048);
            seo.Property(x => x.RobotsIndex).HasColumnName("robots_index").HasDefaultValue(true).IsRequired();
            seo.Property(x => x.OgTitle).HasColumnName("og_title").HasMaxLength(200);
            seo.Property(x => x.OgDescription).HasColumnName("og_description").HasMaxLength(500);
            seo.Property(x => x.OgImageAssetId).HasColumnName("og_image_asset_id");
            seo.HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.OgImageAssetId)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_articles_media_assets_og_image_asset_id");
        });
        builder.Navigation(x => x.Seo).IsRequired();
    }
}

internal sealed class ArticleCategoryConfiguration : IEntityTypeConfiguration<ArticleCategory>
{
    public void Configure(EntityTypeBuilder<ArticleCategory> builder)
    {
        builder.ToTable("article_categories");
        builder.HasKey(x => x.Id).HasName("pk_article_categories");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("ux_article_categories_slug");
    }
}

internal sealed class ArticleCategoryLinkConfiguration : IEntityTypeConfiguration<ArticleCategoryLink>
{
    public void Configure(EntityTypeBuilder<ArticleCategoryLink> builder)
    {
        builder.ToTable("article_category_links");
        builder.HasKey(x => new { x.ArticleId, x.CategoryId }).HasName("pk_article_category_links");
        builder.Property(x => x.ArticleId).HasColumnName("article_id");
        builder.Property(x => x.CategoryId).HasColumnName("category_id");
        builder.HasOne(x => x.Article).WithMany(x => x.Categories).HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_article_category_links_articles_article_id");
        builder.HasOne(x => x.Category).WithMany(x => x.Articles).HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_article_category_links_categories_category_id");
        builder.HasIndex(x => x.CategoryId).HasDatabaseName("ix_article_category_links_category_id");
    }
}
