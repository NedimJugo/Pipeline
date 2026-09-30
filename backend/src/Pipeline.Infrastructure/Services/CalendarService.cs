using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Calendar.DTOs;
using Pipeline.Application.Features.Calendar.Services;
using Pipeline.Domain.Entities;
using Pipeline.Infrastructure.Persistence;

namespace Pipeline.Infrastructure.Services;

public class CalendarService : ICalendarService
{
    private readonly PipelineDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CalendarService(PipelineDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<CalendarEventDto>> GetEventsAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var fromUtc = from ?? DateTime.UtcNow.AddMonths(-1);
        var toUtc = to ?? DateTime.UtcNow.AddMonths(3);

        return await FetchEventsInternalAsync(_currentUserService.UserId ?? Guid.Empty, fromUtc, toUtc, ct);
    }

    public async Task<CalendarFeedUrlDto> GetFeedUrlAsync(string baseUrl, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
        {
            throw new KeyNotFoundException("Current user record was not found.");
        }

        if (string.IsNullOrWhiteSpace(user.CalendarFeedToken))
        {
            user.CalendarFeedToken = Guid.NewGuid().ToString("N");
            await _dbContext.SaveChangesAsync(ct);
        }

        return BuildFeedDto(baseUrl, user.CalendarFeedToken);
    }

    public async Task<CalendarFeedUrlDto> RotateFeedTokenAsync(string baseUrl, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
        {
            throw new KeyNotFoundException("Current user record was not found.");
        }

        user.CalendarFeedToken = Guid.NewGuid().ToString("N");
        await _dbContext.SaveChangesAsync(ct);

        return BuildFeedDto(baseUrl, user.CalendarFeedToken);
    }

    public async Task<string?> GenerateIcsFeedAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.CalendarFeedToken == token, ct);

        if (user == null) return null;

        var fromUtc = DateTime.UtcNow.AddDays(-30);
        var toUtc = DateTime.UtcNow.AddDays(180);

        var events = await FetchEventsInternalAsync(user.Id, fromUtc, toUtc, ct, bypassUserFilter: true);

        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//Pipeline//Job Search Command Center//EN");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");
        sb.AppendLine("X-WR-CALNAME:Pipeline Job Search Calendar");
        sb.AppendLine("X-WR-TIMEZONE:UTC");

        foreach (var ev in events)
        {
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{ev.Id}@pipeline.local");
            sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMdd\\THHmmss\\Z}");

            if (ev.IsAllDay)
            {
                sb.AppendLine($"DTSTART;VALUE=DATE:{ev.StartAt:yyyyMMdd}");
                sb.AppendLine($"DTEND;VALUE=DATE:{ev.StartAt.AddDays(1):yyyyMMdd}");
            }
            else
            {
                sb.AppendLine($"DTSTART:{ev.StartAt:yyyyMMdd\\THHmmss\\Z}");
                var end = ev.EndAt ?? ev.StartAt.AddMinutes(30);
                sb.AppendLine($"DTEND:{end:yyyyMMdd\\THHmmss\\Z}");
            }

            sb.AppendLine($"SUMMARY:{EscapeIcsText(ev.Title)}");

            if (!string.IsNullOrWhiteSpace(ev.Description))
            {
                sb.AppendLine($"DESCRIPTION:{EscapeIcsText(ev.Description)}");
            }

            if (!string.IsNullOrWhiteSpace(ev.Location))
            {
                sb.AppendLine($"LOCATION:{EscapeIcsText(ev.Location)}");
            }

            if (!string.IsNullOrWhiteSpace(ev.Status))
            {
                sb.AppendLine($"STATUS:{ev.Status.ToUpperInvariant()}");
            }

            sb.AppendLine("END:VEVENT");
        }

        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    private async Task<List<CalendarEventDto>> FetchEventsInternalAsync(
        Guid userId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct,
        bool bypassUserFilter = false)
    {
        var result = new List<CalendarEventDto>();

        // 1. Interviews
        var interviewsQuery = bypassUserFilter
            ? _dbContext.Interviews.IgnoreQueryFilters().Where(i => i.UserId == userId)
            : _dbContext.Interviews.AsQueryable();

        var interviews = await interviewsQuery
            .Include(i => i.Application)
                .ThenInclude(a => a!.Company)
            .AsNoTracking()
            .Where(i => i.ScheduledAt >= fromUtc && i.ScheduledAt <= toUtc)
            .ToListAsync(ct);

        foreach (var i in interviews)
        {
            var companyName = i.Application?.Company?.Name ?? "Unknown Company";
            var roleTitle = i.Application?.RoleTitle ?? "Position";
            var title = $"Interview ({i.Type}): {roleTitle} @ {companyName}";
            var endAt = i.ScheduledAt.AddMinutes(i.DurationMinutes > 0 ? i.DurationMinutes : 45);

            result.Add(new CalendarEventDto(
                Id: $"interview_{i.Id}",
                Title: title,
                Type: CalendarEventType.Interview,
                StartAt: i.ScheduledAt,
                EndAt: endAt,
                IsAllDay: false,
                Description: i.PrepNotes,
                Location: i.Location ?? i.MeetingLink,
                ApplicationId: i.ApplicationId,
                RoleTitle: roleTitle,
                CompanyName: companyName,
                ContactId: null,
                ContactName: null,
                Status: i.Status.ToString(),
                Url: $"/interviews/{i.Id}"));
        }

        // 2. Tasks
        var tasksQuery = bypassUserFilter
            ? _dbContext.Tasks.IgnoreQueryFilters().Where(t => t.UserId == userId)
            : _dbContext.Tasks.AsQueryable();

        var tasks = await tasksQuery
            .Include(t => t.Application)
                .ThenInclude(a => a!.Company)
            .Include(t => t.Contact)
            .AsNoTracking()
            .Where(t => t.DueAt.HasValue && t.DueAt.Value >= fromUtc && t.DueAt.Value <= toUtc)
            .ToListAsync(ct);

        foreach (var t in tasks)
        {
            var companyName = t.Application?.Company?.Name;
            var roleTitle = t.Application?.RoleTitle;
            var contactName = t.Contact?.FullName;

            result.Add(new CalendarEventDto(
                Id: $"task_{t.Id}",
                Title: $"Task: {t.Title}",
                Type: CalendarEventType.Task,
                StartAt: t.DueAt!.Value,
                EndAt: null,
                IsAllDay: true,
                Description: t.Notes,
                Location: null,
                ApplicationId: t.ApplicationId,
                RoleTitle: roleTitle,
                CompanyName: companyName,
                ContactId: t.ContactId,
                ContactName: contactName,
                Status: t.CompletedAt.HasValue ? "Completed" : "Pending",
                Url: "/tasks"));
        }

        // 3. Application Offer Deadlines
        var appsQuery = bypassUserFilter
            ? _dbContext.Applications.IgnoreQueryFilters().Where(a => a.UserId == userId && a.DeletedAt == null)
            : _dbContext.Applications.AsQueryable();

        var offers = await appsQuery
            .Include(a => a.Company)
            .AsNoTracking()
            .Where(a => a.OfferDeadline.HasValue && a.OfferDeadline.Value >= fromUtc && a.OfferDeadline.Value <= toUtc)
            .ToListAsync(ct);

        foreach (var o in offers)
        {
            var companyName = o.Company?.Name ?? "Unknown Company";
            result.Add(new CalendarEventDto(
                Id: $"offer_{o.Id}",
                Title: $"Offer Deadline: {o.RoleTitle} @ {companyName}",
                Type: CalendarEventType.OfferDeadline,
                StartAt: o.OfferDeadline!.Value,
                EndAt: null,
                IsAllDay: true,
                Description: $"Decision deadline for {o.RoleTitle} offer at {companyName}.",
                Location: null,
                ApplicationId: o.Id,
                RoleTitle: o.RoleTitle,
                CompanyName: companyName,
                ContactId: null,
                ContactName: null,
                Status: o.Status.ToString(),
                Url: $"/applications/{o.Id}"));
        }

        // 4. Contact Follow-ups
        var contactsQuery = bypassUserFilter
            ? _dbContext.Contacts.IgnoreQueryFilters().Where(c => c.UserId == userId && c.DeletedAt == null)
            : _dbContext.Contacts.AsQueryable();

        var followUps = await contactsQuery
            .Include(c => c.Company)
            .AsNoTracking()
            .Where(c => c.NextFollowUpAt.HasValue && c.NextFollowUpAt.Value >= fromUtc && c.NextFollowUpAt.Value <= toUtc)
            .ToListAsync(ct);

        foreach (var c in followUps)
        {
            var fullName = c.FullName;
            var companyName = c.Company?.Name;
            result.Add(new CalendarEventDto(
                Id: $"followup_{c.Id}",
                Title: $"Follow up with {fullName}" + (companyName != null ? $" ({companyName})" : ""),
                Type: CalendarEventType.FollowUp,
                StartAt: c.NextFollowUpAt!.Value,
                EndAt: null,
                IsAllDay: true,
                Description: $"Scheduled follow up with {fullName}.",
                Location: null,
                ApplicationId: null,
                RoleTitle: null,
                CompanyName: companyName,
                ContactId: c.Id,
                ContactName: fullName,
                Status: "Scheduled",
                Url: $"/contacts/{c.Id}"));
        }

        return result.OrderBy(e => e.StartAt).ToList();
    }

    private static CalendarFeedUrlDto BuildFeedDto(string baseUrl, string token)
    {
        var cleanBase = baseUrl.TrimEnd('/');
        var feedUrl = $"{cleanBase}/api/calendar/feed/{token}.ics";
        var webcalUrl = feedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? "webcal://" + feedUrl.Substring(8)
            : (feedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                ? "webcal://" + feedUrl.Substring(7)
                : feedUrl);

        return new CalendarFeedUrlDto(feedUrl, webcalUrl, token);
    }

    private static string EscapeIcsText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n");
    }
}
