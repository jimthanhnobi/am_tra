using Microsoft.EntityFrameworkCore;
using TeaPremium.Domain.Catalog;
using TeaPremium.Domain.Media;

namespace TeaPremium.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public const string ConnectionStringName = "Postgres";

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    public DbSet<ProductMedia> ProductMedia => Set<ProductMedia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
