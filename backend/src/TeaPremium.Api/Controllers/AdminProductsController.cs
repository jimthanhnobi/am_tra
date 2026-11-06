using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Catalog;
using TeaPremium.Application.Security;

namespace TeaPremium.Api.Controllers;

[Route("admin/products")]
public sealed class AdminProductsController(ICatalogService catalogService)
    : AdminApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.CatalogRead)]
    public Task<PagedResult<AdminProductListItem>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? brandId = null,
        [FromQuery] string sort = "updated_desc",
        CancellationToken cancellationToken = default) =>
        catalogService.GetAdminProductsAsync(
            new AdminProductListQuery(
                page,
                pageSize,
                search,
                status,
                categoryId,
                brandId,
                sort),
            cancellationToken);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.CatalogRead)]
    public Task<AdminProductDetail> GetProduct(
        Guid id,
        CancellationToken cancellationToken) =>
        catalogService.GetAdminProductAsync(id, cancellationToken);

    [HttpPost]
    [Authorize(Policy = Permissions.CatalogWrite)]
    public async Task<ActionResult<AdminProductDetail>> CreateProduct(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var product = await catalogService.CreateProductAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = Permissions.CatalogWrite)]
    public Task<AdminProductDetail> UpdateProduct(
        Guid id,
        UpdateProductCommand command,
        CancellationToken cancellationToken) =>
        catalogService.UpdateProductAsync(id, command, cancellationToken);

    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = Permissions.CatalogPublish)]
    public Task<AdminProductDetail> PublishProduct(
        Guid id,
        CancellationToken cancellationToken) =>
        catalogService.PublishProductAsync(id, cancellationToken);

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = Permissions.CatalogPublish)]
    public Task<AdminProductDetail> ArchiveProduct(
        Guid id,
        CancellationToken cancellationToken) =>
        catalogService.ArchiveProductAsync(id, cancellationToken);

    [HttpPost("{id:guid}/media")]
    [Authorize(Policy = Permissions.CatalogWrite)]
    public async Task<ActionResult<AdminProductMediaDto>> AttachMedia(
        Guid id,
        AttachProductMediaCommand command,
        CancellationToken cancellationToken)
    {
        var media = await catalogService.AttachProductMediaAsync(id, command, cancellationToken);
        return CreatedAtAction(nameof(GetProduct), new { id }, media);
    }

    [HttpPatch("{id:guid}/media/{mediaId:guid}")]
    [Authorize(Policy = Permissions.CatalogWrite)]
    public Task<AdminProductMediaDto> UpdateMedia(
        Guid id,
        Guid mediaId,
        UpdateProductMediaCommand command,
        CancellationToken cancellationToken) =>
        catalogService.UpdateProductMediaAsync(id, mediaId, command, cancellationToken);

    [HttpDelete("{id:guid}/media/{mediaId:guid}")]
    [Authorize(Policy = Permissions.CatalogWrite)]
    public async Task<IActionResult> RemoveMedia(
        Guid id,
        Guid mediaId,
        CancellationToken cancellationToken)
    {
        await catalogService.RemoveProductMediaAsync(id, mediaId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/media/{mediaId:guid}/cover")]
    [Authorize(Policy = Permissions.CatalogWrite)]
    public Task<AdminProductMediaDto> SetCover(
        Guid id,
        Guid mediaId,
        CancellationToken cancellationToken) =>
        catalogService.SetProductCoverAsync(id, mediaId, cancellationToken);
}
