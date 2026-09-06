using ErpSync.Application.DTOs.Responses;
using ErpSync.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ErpSync.Api.Controllers;

[ApiController]
[Route("api/purchase-orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderQueryService _queryService;

    public PurchaseOrdersController(IPurchaseOrderQueryService queryService)
    {
        _queryService = queryService;
    }

    // GET /api/purchase-orders?supplier=&companyCode= — spec.md §10
    [HttpGet]
    public async Task<ActionResult<List<PurchaseOrderDto>>> GetAll(
        [FromQuery] string? supplier, [FromQuery] string? companyCode, CancellationToken cancellationToken)
    {
        var result = await _queryService.GetPurchaseOrdersAsync(supplier, companyCode, cancellationToken);
        return Ok(result);
    }

    // GET /api/purchase-orders/{id}/items — spec.md §10
    [HttpGet("{id}/items")]
    public async Task<ActionResult<List<PurchaseOrderItemDto>>> GetItems(
        string id, CancellationToken cancellationToken)
    {
        var items = await _queryService.GetItemsAsync(id, cancellationToken);
        return items is null ? NotFound() : Ok(items);
    }
}
