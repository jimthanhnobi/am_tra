namespace TeaPremium.Domain.Catalog;

public sealed class Brand
{
    private Brand()
    {
    }

    public Brand(string name, string slug, string? origin = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        Id = Guid.NewGuid();
        Name = name;
        Slug = slug;
        Origin = origin;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? Origin { get; private set; }

    public ICollection<Product> Products { get; } = new List<Product>();
}
