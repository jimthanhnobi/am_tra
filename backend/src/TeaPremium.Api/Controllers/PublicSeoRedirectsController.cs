using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Seo;
using TeaPremium.Domain.Seo;

namespace TeaPremium.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("public/seo-redirects")]
public sealed class PublicSeoRedirectsController(ISeoRedirectStore redirects) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<object>> Resolve([FromQuery] string? sourcePath, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (!SeoRedirect.IsValidContentPath(sourcePath)) return NotFound();
        var destination = await redirects.FindDestinationAsync(sourcePath!, cancellationToken);
        return destination is null ? NotFound() : Ok(new { destinationPath = destination, redirectType = 301 });
    }
}
