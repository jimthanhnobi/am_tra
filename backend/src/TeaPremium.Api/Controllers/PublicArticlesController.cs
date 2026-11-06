using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Content;

namespace TeaPremium.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("public/articles")]
public sealed class PublicArticlesController(ArticleService articles) : ControllerBase
{
    [HttpGet]
    public async Task<object> List([FromQuery] int page = 1, [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var result = await articles.ListAsync(true, page, pageSize, ct);
        return new
        {
            items = result.Items.Select(x => new { x.Slug, x.Title, x.Excerpt, x.PublishedAt,
                x.CoverUrl, x.CoverWidth, x.CoverHeight }),
            result.Page, result.PageSize, result.TotalItems, result.TotalPages,
        };
    }

    [HttpGet("{slug}")]
    public async Task<object> Get(string slug, CancellationToken ct)
    {
        var article = await articles.GetPublicAsync(slug, ct);
        return new
        {
            article.Slug, article.Title, article.Excerpt, article.ContentHtml,
            article.PublishedAt, article.UpdatedAtUtc,
            seo = new { article.Seo.SeoTitle, article.Seo.MetaDescription,
                article.Seo.CanonicalOverride, article.Seo.RobotsIndex,
                article.Seo.OgTitle, article.Seo.OgDescription },
            categories = article.Categories.Select(x => new { x.Slug, x.Name }),
            article.CoverUrl, article.CoverWidth, article.CoverHeight,
            article.OgImageUrl, article.OgImageWidth, article.OgImageHeight,
        };
    }
}
