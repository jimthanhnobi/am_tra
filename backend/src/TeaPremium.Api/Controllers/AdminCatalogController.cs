using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Catalog;
using TeaPremium.Application.Security;

namespace TeaPremium.Api.Controllers;

[Route("admin/catalog")]
public sealed class AdminCatalogController(IAdminCatalogSummaryQuery summaryQuery)
    : AdminApiControllerBase
{
    [HttpGet("summary")]
    [Authorize(Policy = Permissions.CatalogRead)]
    public async Task<ActionResult<AdminCatalogSummary>> GetSummary(
        CancellationToken cancellationToken)
    {
        var summary = await summaryQuery.ExecuteAsync(cancellationToken);
        return Ok(summary);
    }
}
