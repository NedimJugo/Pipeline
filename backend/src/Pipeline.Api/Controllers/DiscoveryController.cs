using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Features.Discovery.DTOs;
using Pipeline.Application.Features.Discovery.Services;

namespace Pipeline.Api.Controllers;

[ApiController]
[Route("api/discovery")]
[Authorize]
public class DiscoveryController : ControllerBase
{
    private readonly IJobDiscoveryService _discoveryService;

    public DiscoveryController(IJobDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<DiscoveredJobListDto>> GetDiscoveredJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] Guid? sourceId = null,
        CancellationToken ct = default)
    {
        var result = await _discoveryService.GetDiscoveredJobsAsync(page, pageSize, status, search, sourceId, ct);
        return Ok(result);
    }

    [HttpGet("sources")]
    public async Task<IActionResult> GetJobSources(CancellationToken ct = default)
    {
        var sources = await _discoveryService.GetJobSourcesAsync(ct);
        return Ok(sources);
    }

    [HttpPost("jobs/{id:guid}/save")]
    public async Task<IActionResult> SaveJobToWishlist(Guid id, CancellationToken ct = default)
    {
        var application = await _discoveryService.SaveJobToWishlistAsync(id, ct);
        return Ok(application);
    }

    [HttpPost("jobs/{id:guid}/dismiss")]
    public async Task<IActionResult> DismissJob(Guid id, CancellationToken ct = default)
    {
        await _discoveryService.DismissJobAsync(id, ct);
        return NoContent();
    }

    [HttpPost("ingest")]
    public async Task<ActionResult<IngestJobsResultDto>> IngestJobs(CancellationToken ct = default)
    {
        var result = await _discoveryService.IngestJobsAsync(ct);
        return Ok(result);
    }
}
