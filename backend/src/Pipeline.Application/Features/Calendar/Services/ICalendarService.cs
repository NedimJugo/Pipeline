using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Features.Calendar.DTOs;

namespace Pipeline.Application.Features.Calendar.Services;

public interface ICalendarService
{
    Task<List<CalendarEventDto>> GetEventsAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<CalendarFeedUrlDto> GetFeedUrlAsync(string baseUrl, CancellationToken ct = default);
    Task<CalendarFeedUrlDto> RotateFeedTokenAsync(string baseUrl, CancellationToken ct = default);
    Task<string?> GenerateIcsFeedAsync(string token, CancellationToken ct = default);
}
