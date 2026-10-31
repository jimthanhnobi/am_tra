namespace TeaPremium.Domain.Content;

public sealed class ArticleCategory
{
    private ArticleCategory() { }
    public ArticleCategory(string slug, string name)
    {
        Id = Guid.NewGuid();
        Slug = slug;
        Name = name;
    }
    public Guid Id { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ICollection<ArticleCategoryLink> Articles { get; } = new List<ArticleCategoryLink>();
    public void Update(string slug, string name) { Slug = slug; Name = name; }
}
