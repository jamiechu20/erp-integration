using ErpSync.Application.DTOs.Responses;
using ErpSync.Application.Interfaces;
using ErpSync.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ErpSync.Api.Controllers;

[ApiController]
[Route("api/anomalies")]
public class AnomaliesController : ControllerBase
{
    private readonly IAnomalyQueryService _queryService;

    public AnomaliesController(IAnomalyQueryService queryService)
    {
        _queryService = queryService;
    }

    // GET /api/anomalies?ruleType=Overdue|PriceVariance — spec.md §10
    [HttpGet]
    public async Task<ActionResult<List<AnomalyDto>>> GetAll(
        [FromQuery] string? ruleType, CancellationToken cancellationToken)
    {
        AnomalyRuleType? parsedRuleType = null;
        if (!string.IsNullOrWhiteSpace(ruleType))
        {
            if (!Enum.TryParse<AnomalyRuleType>(ruleType, ignoreCase: true, out var value))
            {
                return BadRequest($"無效的 ruleType：{ruleType}");
            }

            parsedRuleType = value;
        }

        var result = await _queryService.GetAnomaliesAsync(parsedRuleType, cancellationToken);
        return Ok(result);
    }
}
