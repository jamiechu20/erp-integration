using ErpSync.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ErpSync.Api.Controllers;

[ApiController]
[Route("api/sync")]
public class SyncController : ControllerBase
{
    private readonly IPurchaseOrderSyncService _syncService;

    public SyncController(IPurchaseOrderSyncService syncService)
    {
        _syncService = syncService;
    }

    // POST /api/sync/run — 手動觸發一次同步（demo 用，spec.md §10）
    [HttpPost("run")]
    public async Task<ActionResult<SyncResult>> Run(CancellationToken cancellationToken)
    {
        var result = await _syncService.SyncAsync(cancellationToken);
        return Ok(result);
    }
}
