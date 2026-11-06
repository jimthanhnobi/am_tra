using System.Security.Claims;
using TeaPremium.Application.Security;

namespace TeaPremium.Api.Security;

internal sealed class HttpAuditContext(IHttpContextAccessor accessor) : IAuditContext
{
    public Guid? ActorUserId => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public string? CorrelationId => accessor.HttpContext?.TraceIdentifier;
}
