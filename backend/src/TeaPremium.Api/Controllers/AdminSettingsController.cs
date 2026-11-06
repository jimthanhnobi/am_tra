using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Security;
using TeaPremium.Application.Settings;

namespace TeaPremium.Api.Controllers;

[Route("admin/settings")]
public sealed class AdminSettingsController(SiteSettingsService settings) : AdminApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.SettingsManage)]
    public Task<AdminSiteSettingsDto> Get(CancellationToken cancellationToken) =>
        settings.GetAdminAsync(cancellationToken);

    [HttpPut]
    [Authorize(Policy = Permissions.SettingsManage)]
    public Task<AdminSiteSettingsDto> Update(SiteSettingsInput input, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
            throw new SiteSettingsValidationException("Authenticated user identifier is invalid.");
        return settings.UpdateAsync(input, actorId, cancellationToken);
    }
}
