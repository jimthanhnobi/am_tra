using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Content;
using TeaPremium.Application.Security;

namespace TeaPremium.Api.Controllers;

[Route("admin/articles")]
public sealed class AdminArticlesController(ArticleService articles) : AdminApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.ContentRead)]
    public Task<ArticlePage<ArticleSummary>> List([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        articles.ListAsync(false, page, pageSize, ct);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.ContentRead)]
    public Task<ArticleDetail> Get(Guid id, CancellationToken ct) => articles.GetAsync(id, ct);

    [HttpGet("{id:guid}/preview")]
    [Authorize(Policy = Permissions.ContentRead)]
    public async Task<ActionResult<ArticleDetail>> Preview(Guid id, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        return await articles.GetAsync(id, ct);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.ContentWrite)]
    public async Task<ActionResult<ArticleDetail>> Create(ArticleInput input, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var authorId))
            return Unauthorized();
        var article = await articles.CreateAsync(input, authorId, ct);
        return CreatedAtAction(nameof(Get), new { id = article.Id }, article);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = Permissions.ContentWrite)]
    public Task<ArticleDetail> Update(Guid id, ArticleInput input, CancellationToken ct) =>
        articles.UpdateAsync(id, input, ct);

    [HttpPost("{id:guid}/review")]
    [Authorize(Policy = Permissions.ContentWrite)]
    public Task<ArticleDetail> Review(Guid id, CancellationToken ct) => articles.TransitionAsync(id, "review", ct);

    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = Permissions.ContentPublish)]
    public Task<ArticleDetail> Publish(Guid id, CancellationToken ct) => articles.TransitionAsync(id, "publish", ct);

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = Permissions.ContentPublish)]
    public Task<ArticleDetail> Archive(Guid id, CancellationToken ct) => articles.TransitionAsync(id, "archive", ct);
}
