using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Features.Users.DTOs;
using Pipeline.Application.Features.Users.Services;

namespace Pipeline.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MeController : ControllerBase
{
    private readonly IUserService _userService;

    public MeController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var profile = await _userService.GetProfileAsync(ct);
        return Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var updated = await _userService.UpdateProfileAsync(request, ct);
        return Ok(updated);
    }

    [HttpPost("complete-onboarding")]
    public async Task<IActionResult> CompleteOnboarding(CancellationToken ct)
    {
        var updated = await _userService.CompleteOnboardingAsync(ct);
        return Ok(updated);
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request, CancellationToken ct)
    {
        var updated = await _userService.UpdatePreferencesAsync(request, ct);
        return Ok(updated);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportGdprData(CancellationToken ct)
    {
        var data = await _userService.ExportGdprDataAsync(ct);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };
        var json = JsonSerializer.Serialize(data, options);
        var bytes = Encoding.UTF8.GetBytes(json);
        var filename = $"pipeline_gdpr_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

        return File(bytes, "application/json", filename);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        await _userService.DeleteAccountAsync(ct);
        return NoContent();
    }

    [HttpPost("seed-demo")]
    public async Task<IActionResult> SeedDemoData(CancellationToken ct)
    {
        await _userService.SeedDemoDataAsync(ct);
        var profile = await _userService.GetProfileAsync(ct);
        return Ok(new { message = "Demo data seeded successfully.", profile });
    }

    [HttpPost("seed-nedim")]
    public async Task<IActionResult> SeedNedimData(CancellationToken ct)
    {
        await _userService.SeedNedimDataAsync(ct);
        var profile = await _userService.GetProfileAsync(ct);
        return Ok(new { message = "Nedim's job application history (24 applications, 27 contacts, 62 interactions) seeded successfully.", profile });
    }
}
