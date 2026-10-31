using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Content;
using TeaPremium.Application.Security;

namespace TeaPremium.Api.Controllers;

[Route("admin/article-categories")]
public sealed class AdminArticleCategoriesController(ArticleService articles) : AdminApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.ContentRead)]
    public Task<IReadOnlyList<ArticleCategoryDto>> List(CancellationToken ct) => articles.CategoriesAsync(ct);

    [HttpPost]
    [Authorize(Policy = Permissions.ContentWrite)]
    public async Task<ActionResult<ArticleCategoryDto>> Create(ArticleCategoryInput input, CancellationToken ct)
    {
        var category = await articles.SaveCategoryAsync(null, input, ct);
        return StatusCode(StatusCodes.Status201Created, category);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = Permissions.ContentWrite)]
    public Task<ArticleCategoryDto> Update(Guid id, ArticleCategoryInput input, CancellationToken ct) =>
        articles.SaveCategoryAsync(id, input, ct);
}
