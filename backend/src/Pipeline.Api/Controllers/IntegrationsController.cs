using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Features.Settings.DTOs;
using Pipeline.Application.Features.Settings.Services;

namespace Pipeline.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/settings/[controller]")]
[Route("api/[controller]")]
public class IntegrationsController : ControllerBase
{
    private readonly IIntegrationService _integrationService;

    public IntegrationsController(IIntegrationService integrationService)
    {
        _integrationService = integrationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        var settings = await _integrationService.GetSettingsAsync(ct);
        return Ok(settings);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateIntegrationSettingsRequest request, CancellationToken ct)
    {
        var updated = await _integrationService.UpdateSettingsAsync(request, ct);
        return Ok(updated);
    }

    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail([FromBody] TestEmailRequest request, CancellationToken ct)
    {
        var result = await _integrationService.SendTestEmailAsync(request, ct);
        return Ok(result);
    }
}
