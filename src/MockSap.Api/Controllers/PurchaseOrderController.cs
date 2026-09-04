using Microsoft.AspNetCore.Mvc;
using MockSap.Api.Models;

namespace MockSap.Api.Controllers;

/// <summary>
/// 模擬 SAP API_PURCHASEORDER_PROCESS_SRV（OData V4）的 PurchaseOrder entity set。
/// </summary>
[ApiController]
[Route("PurchaseOrder")]
public class PurchaseOrderController : ControllerBase
{
    private readonly PurchaseOrderStore _store;

    public PurchaseOrderController(PurchaseOrderStore store)
    {
        _store = store;
    }

    // GET /PurchaseOrder?$expand=to_Item
    [HttpGet]
    public ActionResult<ODataCollectionResponse<PurchaseOrderHeader>> Get()
    {
        var response = new ODataCollectionResponse<PurchaseOrderHeader>
        {
            Value = _store.GetAll().ToList(),
        };

        return Ok(response);
    }

    // POST /PurchaseOrder — demo 用，模擬 SAP 新建一張採購單
    [HttpPost]
    public ActionResult<PurchaseOrderHeader> Post([FromBody] PurchaseOrderHeader order)
    {
        _store.Add(order);
        return CreatedAtAction(nameof(Get), new { }, order);
    }
}
