using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeaPremium.Domain.Catalog;

namespace TeaPremium.Infrastructure.Persistence.Configurations;

internal sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("product_categories");
        builder.HasKey(link => new { link.ProductId, link.CategoryId })
            .HasName("pk_product_categories");

        builder.Property(link => link.ProductId).HasColumnName("product_id");
        builder.Property(link => link.CategoryId).HasColumnName("category_id");

        builder.HasIndex(link => link.CategoryId)
            .HasDatabaseName("ix_product_categories_category_id");

        builder.HasOne(link => link.Product)
            .WithMany(product => product.ProductCategories)
            .HasForeignKey(link => link.ProductId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_product_categories_products_product_id");
        builder.HasOne(link => link.Category)
            .WithMany(category => category.ProductCategories)
            .HasForeignKey(link => link.CategoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_product_categories_categories_category_id");
    }
}
