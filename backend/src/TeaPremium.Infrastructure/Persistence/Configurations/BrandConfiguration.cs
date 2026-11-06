using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeaPremium.Domain.Catalog;

namespace TeaPremium.Infrastructure.Persistence.Configurations;

internal sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("brands");
        builder.HasKey(brand => brand.Id).HasName("pk_brands");

        builder.Property(brand => brand.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(brand => brand.Name)
            .HasColumnName("name")
            .HasMaxLength(300)
            .IsRequired();
        builder.Property(brand => brand.Slug)
            .HasColumnName("slug")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(brand => brand.Origin)
            .HasColumnName("origin")
            .HasMaxLength(300);

        builder.HasIndex(brand => brand.Slug)
            .IsUnique()
            .HasDatabaseName("ux_brands_slug");
    }
}
