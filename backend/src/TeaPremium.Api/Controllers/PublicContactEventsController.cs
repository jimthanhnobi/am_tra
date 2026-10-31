using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeaPremium.Application.Leads;

namespace TeaPremium.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("public/contact-events")]
[EnableCors("public-submit")]
public sealed class PublicContactEventsController(LeadService leads) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("public-contact-events")]
    [RequestSizeLimit(2_048)]
    public async Task<IActionResult> Create(CreateContactEventInput input, CancellationToken ct)
    {
        await leads.RecordEventAsync(input, ct);
        return Accepted();
    }
}
