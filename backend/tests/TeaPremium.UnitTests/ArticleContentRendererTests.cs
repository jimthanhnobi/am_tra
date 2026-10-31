using Ganss.Xss;
using TeaPremium.Application.Content;

namespace TeaPremium.UnitTests;

public sealed class ArticleContentRendererTests
{
    private readonly ArticleContentRenderer _renderer = new(new HtmlSanitizer());

    [Fact]
    public void EncodesScriptTextAndNeverRendersUnknownHtmlNode()
    {
        var html = _renderer.Render("""
            {"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"<script>alert(1)</script>"}]},{"type":"html","attrs":{"html":"<img src=x onerror=alert(1)>"}}]}
            """);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsJavascriptLinksAndKeepsSafeFormatting()
    {
        var html = _renderer.Render("""
            {"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"bad","marks":[{"type":"link","attrs":{"href":"javascript:alert(1)"}}]},{"type":"text","text":" good","marks":[{"type":"bold"},{"type":"link","attrs":{"href":"https://example.org/guide"}}]}]}]}
            """);
        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<strong>", html, StringComparison.Ordinal);
        Assert.Contains("https://example.org/guide", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"type\":5}")]
    [InlineData("{\"type\":\"doc\",\"content\":[{\"type\":\"text\",\"text\":1}]}")]
    public void RejectsNonTiptapDocument(string input) =>
        Assert.Throws<ContentValidationException>(() => _renderer.Render(input));
}
