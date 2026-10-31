using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Leads;
using TeaPremium.Application.Security;

namespace TeaPremium.Api.Controllers;

[Route("admin/leads")]
public sealed class AdminLeadsController(LeadService leads) : AdminApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.LeadsRead)]
    public Task<LeadPage> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default) => leads.ListAsync(page, pageSize, ct);

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Permissions.LeadsManage)]
    public Task<LeadDto> SetStatus(Guid id, LeadStatusInput input, CancellationToken ct) =>
        leads.SetStatusAsync(id, input.Status, ct);
}

public sealed record LeadStatusInput(string Status);
