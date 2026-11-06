using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Settings;

namespace TeaPremium.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("public/settings")]
public sealed class PublicSettingsController(SiteSettingsService settings) : ControllerBase
{
    [HttpGet]
    public Task<PublicSiteSettingsDto> Get(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        return settings.GetPublicAsync(cancellationToken);
    }
}
