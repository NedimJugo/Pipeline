using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Features.Calendar.Services;

namespace Pipeline.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CalendarController : ControllerBase
{
    private readonly ICalendarService _calendarService;

    public CalendarController(ICalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    [HttpGet]
    public async Task<IActionResult> GetEvents(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var result = await _calendarService.GetEventsAsync(from, to, ct);
        return Ok(result);
    }

    [HttpGet("feed-url")]
    public async Task<IActionResult> GetFeedUrl(CancellationToken ct)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _calendarService.GetFeedUrlAsync(baseUrl, ct);
        return Ok(result);
    }

    [HttpPost("rotate-token")]
    public async Task<IActionResult> RotateToken(CancellationToken ct)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _calendarService.RotateFeedTokenAsync(baseUrl, ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("feed/{token}.ics")]
    public async Task<IActionResult> GetIcsFeed(string token, CancellationToken ct)
    {
        var icsContent = await _calendarService.GenerateIcsFeedAsync(token, ct);
        if (icsContent == null)
        {
            return NotFound("Calendar feed was not found or the subscription token is invalid.");
        }

        return File(Encoding.UTF8.GetBytes(icsContent), "text/calendar", "pipeline-calendar.ics");
    }
}
