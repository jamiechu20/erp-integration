using ErpSync.Application.DTOs.Responses;
using ErpSync.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ErpSync.Api.Controllers;

[ApiController]
[Route("api/sync-logs")]
public class SyncLogsController : ControllerBase
{
    private readonly ISyncLogQueryService _queryService;

    public SyncLogsController(ISyncLogQueryService queryService)
    {
        _queryService = queryService;
    }

    // GET /api/sync-logs — spec.md §10
    [HttpGet]
    public async Task<ActionResult<List<SyncLogDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _queryService.GetSyncLogsAsync(cancellationToken);
        return Ok(result);
    }
}
