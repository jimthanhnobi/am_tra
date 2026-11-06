using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Catalog;
using TeaPremium.Application.Security;

namespace TeaPremium.Api.Controllers;

[Route("admin/categories")]
public sealed class AdminCategoriesController(ICatalogService catalogService)
    : AdminApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.CatalogRead)]
    public Task<IReadOnlyList<AdminCategoryNode>> GetCategories(
        CancellationToken cancellationToken) =>
        catalogService.GetAdminCategoriesAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.CatalogRead)]
    public Task<AdminCategoryDetail> GetCategory(
        Guid id,
        CancellationToken cancellationToken) =>
        catalogService.GetAdminCategoryAsync(id, cancellationToken);

    [HttpPost]
    [Authorize(Policy = Permissions.CatalogWrite)]
    public async Task<ActionResult<AdminCategoryDetail>> CreateCategory(
        CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var category = await catalogService.CreateCategoryAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = Permissions.CatalogWrite)]
    public Task<AdminCategoryDetail> UpdateCategory(
        Guid id,
        UpdateCategoryCommand command,
        CancellationToken cancellationToken) =>
        catalogService.UpdateCategoryAsync(id, command, cancellationToken);
}
