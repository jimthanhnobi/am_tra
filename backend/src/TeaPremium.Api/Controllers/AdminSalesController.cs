using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Commerce;
using TeaPremium.Application.Security;

namespace TeaPremium.Api.Controllers;

[Route("admin/orders")]
public sealed class AdminSalesController(CommerceService commerce) : AdminApiControllerBase
{
    [HttpGet, Authorize(Policy = Permissions.LeadsRead)]
    public Task<OrderPage> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default) => commerce.ListOrdersAsync(page, pageSize, ct);

    [HttpGet("{id:guid}"), Authorize(Policy = Permissions.LeadsRead)]
    public Task<OrderDto> Get(Guid id, CancellationToken ct) => commerce.GetOrderAsync(id, ct);

    [HttpPost, Authorize(Policy = Permissions.LeadsManage)]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderInput input, CancellationToken ct)
    {
        var order = await commerce.CreateOrderAsync(input, ct);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    [HttpPost("{id:guid}/complete"), Authorize(Policy = Permissions.LeadsManage)]
    public Task<OrderDto> Complete(Guid id, CancellationToken ct) =>
        commerce.TransitionOrderAsync(id, "complete", ct);

    [HttpPost("{id:guid}/cancel"), Authorize(Policy = Permissions.LeadsManage)]
    public Task<OrderDto> Cancel(Guid id, CancellationToken ct) =>
        commerce.TransitionOrderAsync(id, "cancel", ct);
}

[Route("admin/reports")]
public sealed class AdminCommerceReportsController(CommerceService commerce) : AdminApiControllerBase
{
    [HttpGet("sales-overview"), Authorize(Policy = Permissions.LeadsRead)]
    public Task<SalesReport> Sales([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to,
        CancellationToken ct) => commerce.ReportAsync(from, to, ct);
}
