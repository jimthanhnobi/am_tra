using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Content;
using TeaPremium.Domain.Leads;
using TeaPremium.Domain.Media;
using TeaPremium.Domain.Seo;
using TeaPremium.Domain.Settings;
using TeaPremium.Infrastructure.Identity;

namespace TeaPremium.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public const string ConnectionStringName = "Postgres";

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    public DbSet<ProductMedia> ProductMedia => Set<ProductMedia>();

    public DbSet<MediaUploadSession> MediaUploadSessions => Set<MediaUploadSession>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleCategory> ArticleCategories => Set<ArticleCategory>();
    public DbSet<ArticleCategoryLink> ArticleCategoryLinks => Set<ArticleCategoryLink>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<ContactEvent> ContactEvents => Set<ContactEvent>();
    public DbSet<SeoRedirect> SeoRedirects => Set<SeoRedirect>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
