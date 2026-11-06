using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Commerce;
using TeaPremium.Application.Security;

namespace TeaPremium.Api.Controllers;

[Route("admin/customers")]
public sealed class AdminCustomersController(CommerceService commerce) : AdminApiControllerBase
{
    [HttpGet, Authorize(Policy = Permissions.LeadsRead)]
    public Task<CustomerPage> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, CancellationToken ct = default) =>
        commerce.ListCustomersAsync(page, pageSize, search, ct);

    [HttpGet("{id:guid}"), Authorize(Policy = Permissions.LeadsRead)]
    public Task<CustomerDto> Get(Guid id, CancellationToken ct) => commerce.GetCustomerAsync(id, ct);

    [HttpPost, Authorize(Policy = Permissions.LeadsManage)]
    public async Task<ActionResult<CustomerDto>> Create(CustomerInput input, CancellationToken ct)
    {
        var customer = await commerce.CreateCustomerAsync(input, ct);
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, customer);
    }

    [HttpPatch("{id:guid}"), Authorize(Policy = Permissions.LeadsManage)]
    public Task<CustomerDto> Update(Guid id, CustomerInput input, CancellationToken ct) =>
        commerce.UpdateCustomerAsync(id, input, ct);

    [HttpPost("import"), Authorize(Policy = Permissions.LeadsManage)]
    [RequestSizeLimit(200_000)]
    public Task<ImportCustomersResult> Import(ImportCustomersInput input, CancellationToken ct) =>
        commerce.ImportCustomersAsync(input, ct);

    [HttpGet("{id:guid}/interactions"), Authorize(Policy = Permissions.LeadsRead)]
    public Task<IReadOnlyList<InteractionDto>> Interactions(Guid id, CancellationToken ct) =>
        commerce.GetInteractionsAsync(id, ct);

    [HttpPost("{id:guid}/interactions"), Authorize(Policy = Permissions.LeadsManage)]
    public Task<InteractionDto> AddInteraction(Guid id, InteractionInput input, CancellationToken ct) =>
        commerce.AddInteractionAsync(id, input, ct);
}
