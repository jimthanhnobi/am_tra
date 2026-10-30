using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Catalog;

namespace TeaPremium.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("public/products")]
public sealed class PublicProductsController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<PublicProductListItem>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] string sort = "latest",
        CancellationToken cancellationToken = default) =>
        catalogService.GetPublicProductsAsync(
            new PublicProductListQuery(page, pageSize, search, category, sort),
            cancellationToken);

    [HttpGet("{slug}")]
    public Task<PublicProductDetail> GetProduct(
        string slug,
        CancellationToken cancellationToken) =>
        catalogService.GetPublicProductAsync(slug, cancellationToken);
}
