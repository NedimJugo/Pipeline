using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Infrastructure.Persistence;

namespace Pipeline.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly PipelineDbContext _dbContext;

    public HealthController(PipelineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth(CancellationToken ct)
    {
        var dbHealthy = true;
        try
        {
            dbHealthy = await _dbContext.Database.CanConnectAsync(ct);
        }
        catch
        {
            dbHealthy = false;
        }

        var result = new
        {
            status = dbHealthy ? "Healthy" : "Degraded",
            database = dbHealthy ? "Healthy" : "Unhealthy",
            timestamp = DateTime.UtcNow
        };

        return Ok(result);
    }
}
