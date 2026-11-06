using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Catalog;
using TeaPremium.Application.Security;

namespace TeaPremium.Api.Controllers;

[Route("admin/brands")]
public sealed class AdminBrandsController(ICatalogService catalogService)
    : AdminApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.CatalogRead)]
    public Task<IReadOnlyList<AdminBrandDto>> GetBrands(CancellationToken cancellationToken) =>
        catalogService.GetAdminBrandsAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.CatalogRead)]
    public Task<AdminBrandDto> GetBrand(Guid id, CancellationToken cancellationToken) =>
        catalogService.GetAdminBrandAsync(id, cancellationToken);

    [HttpPost]
    [Authorize(Policy = Permissions.CatalogWrite)]
    public async Task<ActionResult<AdminBrandDto>> CreateBrand(
        CreateBrandCommand command,
        CancellationToken cancellationToken)
    {
        var brand = await catalogService.CreateBrandAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetBrand), new { id = brand.Id }, brand);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = Permissions.CatalogWrite)]
    public Task<AdminBrandDto> UpdateBrand(
        Guid id,
        UpdateBrandCommand command,
        CancellationToken cancellationToken) =>
        catalogService.UpdateBrandAsync(id, command, cancellationToken);
}
