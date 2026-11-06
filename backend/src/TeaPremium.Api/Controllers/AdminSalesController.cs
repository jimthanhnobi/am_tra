using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeaPremium.Application.Commerce;
using TeaPremium.Application.Security;
using TeaPremium.Domain.Common;

namespace TeaPremium.Api.Controllers;

[Route("admin/orders")]
public sealed class AdminSalesController(CommerceService commerce) : AdminApiControllerBase
{
    [HttpGet, Authorize(Policy = Permissions.LeadsRead)]
    public Task<OrderPage> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, CancellationToken ct = default) => commerce.ListOrdersAsync(page, pageSize, ct, status);

    [HttpGet("products"), Authorize(Policy = Permissions.LeadsRead)]
    public Task<IReadOnlyList<SellableProductDto>> Products(
        [FromQuery] string? search,
        [FromQuery] Guid? includeId,
        CancellationToken ct) => commerce.SearchProductsAsync(search, includeId, ct);

    [HttpGet("{id:guid}"), Authorize(Policy = Permissions.LeadsRead)]
    public Task<OrderDto> Get(Guid id, CancellationToken ct) => commerce.GetOrderAsync(id, ct);

    [HttpPost, Authorize(Policy = Permissions.LeadsManage)]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderInput input, CancellationToken ct)
    {
        var order = await commerce.CreateOrderAsync(input, ct);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    [HttpPost("{id:guid}/complete"), Authorize(Policy = Permissions.LeadsManage)]
    public Task<OrderDto> Complete(Guid id, [FromBody] OrderTransitionInput input, CancellationToken ct) =>
        commerce.TransitionOrderAsync(id, "complete", ct, input);

    [HttpPost("{id:guid}/cancel"), Authorize(Policy = Permissions.LeadsManage)]
    public Task<OrderDto> Cancel(Guid id, [FromBody] OrderTransitionInput input, CancellationToken ct) =>
        commerce.TransitionOrderAsync(id, "cancel", ct, input);

    [HttpPost("{id:guid}/return"), Authorize(Policy = Permissions.LeadsManage)]
    public Task<OrderDto> Return(Guid id, [FromBody] OrderTransitionInput input, CancellationToken ct) =>
        commerce.TransitionOrderAsync(id, "return", ct, input);
}

[Route("admin/reports")]
public sealed class AdminCommerceReportsController(CommerceService commerce) : AdminApiControllerBase
{
    [HttpGet("sales-overview"), Authorize(Policy = Permissions.LeadsRead)]
    public Task<SalesReport> Sales([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        if (fromDate.HasValue && toDate.HasValue && !from.HasValue && !to.HasValue)
            return commerce.ReportAsync(BusinessCalendar.StartOfDayUtc(fromDate.Value),
                BusinessCalendar.StartOfDayUtc(toDate.Value), ct);
        if (from.HasValue && to.HasValue && !fromDate.HasValue && !toDate.HasValue)
            return commerce.ReportAsync(from.Value, to.Value, ct);
        throw new CommerceInputException("Supply fromDate/toDate (Vietnam dates) or from/to (UTC instants), not both.");
    }
}
