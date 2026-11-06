using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeaPremium.Application.Leads;

namespace TeaPremium.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("public/leads")]
[EnableCors("public-submit")]
public sealed class PublicLeadsController(LeadService leads) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("public-leads")]
    [RequestSizeLimit(8_192)]
    public async Task<IActionResult> Create(CreateLeadInput input, CancellationToken ct)
    {
        await leads.SubmitAsync(input, ct);
        return Accepted();
    }
}
