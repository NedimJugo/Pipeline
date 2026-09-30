using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Features.Analytics.Services;

namespace Pipeline.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] int? days, CancellationToken ct)
    {
        var result = await _analyticsService.GetOverviewAsync(days, ct);
        return Ok(result);
    }

    [HttpGet("funnel")]
    public async Task<IActionResult> GetFunnel([FromQuery] int? days, CancellationToken ct)
    {
        var result = await _analyticsService.GetFunnelAsync(days, ct);
        return Ok(result);
    }

    [HttpGet("by-source")]
    public async Task<IActionResult> GetBySource([FromQuery] int? days, CancellationToken ct)
    {
        var result = await _analyticsService.GetBySourceAsync(days, ct);
        return Ok(result);
    }

    [HttpGet("by-document")]
    public async Task<IActionResult> GetByDocument([FromQuery] int? days, CancellationToken ct)
    {
        var result = await _analyticsService.GetByDocumentAsync(days, ct);
        return Ok(result);
    }

    [HttpGet("by-work-mode")]
    public async Task<IActionResult> GetByWorkMode([FromQuery] int? days, CancellationToken ct)
    {
        var result = await _analyticsService.GetByWorkModeAsync(days, ct);
        return Ok(result);
    }

    [HttpGet("stage-durations")]
    public async Task<IActionResult> GetStageDurations([FromQuery] int? days, CancellationToken ct)
    {
        var result = await _analyticsService.GetStageDurationsAsync(days, ct);
        return Ok(result);
    }

    [HttpGet("weekly")]
    public async Task<IActionResult> GetWeekly([FromQuery] int? days, CancellationToken ct)
    {
        var result = await _analyticsService.GetWeeklyVelocityAsync(days, ct);
        return Ok(result);
    }

    [HttpGet("insights")]
    public async Task<IActionResult> GetInsights([FromQuery] int? days, CancellationToken ct)
    {
        var result = await _analyticsService.GetInsightsAsync(days, ct);
        return Ok(result);
    }
}
