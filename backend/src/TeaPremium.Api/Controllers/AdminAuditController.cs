using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Commerce;
using TeaPremium.Application.Security;

namespace TeaPremium.Api.Controllers;

[Route("admin/audit"), Authorize(Policy = Permissions.UsersManage)]
public sealed class AdminAuditController(IAuditQuery audit) : AdminApiControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AuditEntryDto>> List([FromQuery] Guid? entityId,
        [FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        if (page is < 1 or > 100000) throw new CommerceInputException("Invalid audit page.");
        return audit.ListAsync(entityId, page, cancellationToken);
    }
}
