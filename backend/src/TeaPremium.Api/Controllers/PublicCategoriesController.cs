using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Catalog;

namespace TeaPremium.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("public/categories")]
public sealed class PublicCategoriesController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<PublicCategoryNode>> GetCategories(
        CancellationToken cancellationToken) =>
        catalogService.GetPublicCategoriesAsync(cancellationToken);

    [HttpGet("{slug}")]
    public Task<PublicCategoryDetail> GetCategory(
        string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default) =>
        catalogService.GetPublicCategoryAsync(slug, page, pageSize, cancellationToken);
}
