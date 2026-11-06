namespace TeaPremium.Domain.Content;

public sealed class ArticleCategoryLink
{
    private ArticleCategoryLink() { }
    public ArticleCategoryLink(Guid articleId, Guid categoryId)
    { ArticleId = articleId; CategoryId = categoryId; }
    public Guid ArticleId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Article Article { get; private set; } = null!;
    public ArticleCategory Category { get; private set; } = null!;
}
