namespace TeaPremium.Domain.Catalog;

public sealed class ProductCategory
{
    private ProductCategory()
    {
    }

    public ProductCategory(Guid productId, Guid categoryId)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID is required.", nameof(productId));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category ID is required.", nameof(categoryId));
        }

        ProductId = productId;
        CategoryId = categoryId;
    }

    public Guid ProductId { get; private set; }

    public Guid CategoryId { get; private set; }

    public Product Product { get; private set; } = null!;

    public Category Category { get; private set; } = null!;
}
