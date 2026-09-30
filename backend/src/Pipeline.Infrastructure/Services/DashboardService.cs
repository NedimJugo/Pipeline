using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Automation.Services;
using Pipeline.Application.Features.Dashboard.DTOs;
using Pipeline.Application.Features.Dashboard.Services;
using Pipeline.Application.Features.Tasks.DTOs;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;

namespace Pipeline.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly PipelineDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAutomationRuleEngine _automationEngine;

    public DashboardService(
        PipelineDbContext dbContext,
        ICurrentUserService currentUserService,
        IAutomationRuleEngine automationEngine)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _automationEngine = automationEngine;
    }

    public async Task<DashboardSummaryDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);

        // Run automation rules to ensure up-to-date next actions
        try
        {
            await _automationEngine.EvaluateRulesForUserAsync(userId, ct);
        }
        catch
        {
            // Do not fail dashboard load if automation engine has a transient warning
        }

        var now = DateTime.UtcNow;
        var today = now.Date;
        var name = user?.DisplayName ?? "Job Seeker";

        string greetingPrefix = now.Hour switch
        {
            < 12 => "Good morning",
            < 18 => "Good afternoon",
            _ => "Good evening"
        };
        var greeting = $"{greetingPrefix}, {name}";

        var staleAfterDays = user?.StaleAfterDays > 0 ? user.StaleAfterDays : 14;

        var activeStatuses = new[]
        {
            ApplicationStatus.Wishlist,
            ApplicationStatus.Applied,
            ApplicationStatus.Screening,
            ApplicationStatus.Interview,
            ApplicationStatus.Assignment
        };

        // 1. Do Today Tasks
        var doTodayTasks = await _dbContext.Tasks
            .Include(t => t.Application)
                .ThenInclude(a => a != null ? a.Company : null)
            .Include(t => t.Contact)
            .Include(t => t.Interview)
            .Where(t => t.UserId == userId && t.CompletedAt == null && t.DueAt != null && t.DueAt.Value.Date <= today)
            .OrderBy(t => t.DueAt)
            .Take(15)
            .Select(t => new TaskItemDto(
                t.Id,
                t.ApplicationId,
                t.Application != null ? t.Application.RoleTitle : null,
                t.Application != null && t.Application.Company != null ? t.Application.Company.Name : null,
                t.ContactId,
                t.Contact != null ? t.Contact.FullName : null,
                t.InterviewId,
                t.Interview != null ? $"{t.Interview.Type} Interview" : null,
                t.Title,
                t.Notes,
                t.DueAt,
                t.CompletedAt,
                t.Source,
                t.AutoRuleKey,
                t.CreatedAt
            ))
            .ToListAsync(ct);

        // 2. Upcoming Interviews (next 7 days)
        var next7Days = now.AddDays(7);
        var rawInterviews = await _dbContext.Interviews
            .Include(i => i.Application)
                .ThenInclude(a => a != null ? a.Company : null)
            .Where(i => i.UserId == userId && i.Status == InterviewStatus.Scheduled && i.ScheduledAt >= now && i.ScheduledAt <= next7Days)
            .OrderBy(i => i.ScheduledAt)
            .Take(10)
            .ToListAsync(ct);

        var upcomingInterviews = rawInterviews.Select(i =>
        {
            var diff = i.ScheduledAt - now;
            string countdown;
            if (i.ScheduledAt.Date == today)
            {
                countdown = $"Today at {i.ScheduledAt:HH:mm}";
            }
            else if (i.ScheduledAt.Date == today.AddDays(1))
            {
                countdown = $"Tomorrow at {i.ScheduledAt:HH:mm}";
            }
            else
            {
                countdown = $"in {(int)Math.Ceiling(diff.TotalDays)} days";
            }

            List<Pipeline.Application.Features.Interviews.DTOs.PrepChecklistItemDto> checklist;
            try
            {
                checklist = string.IsNullOrWhiteSpace(i.PrepChecklist)
                    ? new List<Pipeline.Application.Features.Interviews.DTOs.PrepChecklistItemDto>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<Pipeline.Application.Features.Interviews.DTOs.PrepChecklistItemDto>>(
                        i.PrepChecklist,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                      ?? new List<Pipeline.Application.Features.Interviews.DTOs.PrepChecklistItemDto>();
            }
            catch
            {
                checklist = new List<Pipeline.Application.Features.Interviews.DTOs.PrepChecklistItemDto>();
            }
            var total = checklist.Count;
            var completed = checklist.Count(c => c.Done);
            var percent = total > 0 ? (int)Math.Round((double)completed / total * 100) : 0;

            return new UpcomingInterviewDto(
                i.Id,
                i.ApplicationId,
                i.Application?.RoleTitle ?? "Interview",
                i.Application?.Company?.Name ?? "Company",
                i.Type,
                i.Format,
                i.ScheduledAt,
                countdown,
                total,
                completed,
                percent
            );
        }).ToList();

        // 3. This Week Stats
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        var applicationsSent = await _dbContext.Applications
            .CountAsync(a => a.UserId == userId && (a.AppliedAt >= startOfWeek || (a.Status == ApplicationStatus.Applied && a.StatusChangedAt >= startOfWeek)), ct);

        var responses = await _dbContext.Interactions
            .CountAsync(i => i.UserId == userId && i.Direction == InteractionDirection.Inbound && i.OccurredAt >= startOfWeek, ct);

        var interviewsThisWeek = await _dbContext.Interviews
            .CountAsync(i => i.UserId == userId && i.ScheduledAt >= startOfWeek && i.ScheduledAt <= now, ct);

        var offersThisWeek = await _dbContext.Applications
            .CountAsync(a => a.UserId == userId && a.Status == ApplicationStatus.Offer && a.StatusChangedAt >= startOfWeek, ct);

        var weeklyStats = new DashboardWeeklyStatsDto(
            applicationsSent,
            responses,
            interviewsThisWeek,
            offersThisWeek
        );

        // 4. Stale Applications
        var rawStaleApps = await _dbContext.Applications
            .Include(a => a.Company)
            .Where(a => a.UserId == userId && activeStatuses.Contains(a.Status))
            .ToListAsync(ct);

        var staleApps = rawStaleApps
            .Where(a => (now - a.StatusChangedAt).TotalDays >= staleAfterDays)
            .OrderByDescending(a => (now - a.StatusChangedAt).TotalDays)
            .Take(10)
            .Select(a => new StaleApplicationDto(
                a.Id,
                a.Company?.Name ?? "Company",
                a.RoleTitle,
                a.Status,
                (int)Math.Round((now - a.StatusChangedAt).TotalDays),
                staleAfterDays
            ))
            .ToList();

        // Counts
        var activeApplicationsCount = rawStaleApps.Count;
        var activeOffersCount = await _dbContext.Applications.CountAsync(a => a.UserId == userId && a.Status == ApplicationStatus.Offer, ct);

        return new DashboardSummaryDto(
            greeting,
            user?.SearchStatus ?? SearchStatus.Active,
            user?.TargetRole,
            doTodayTasks,
            upcomingInterviews,
            weeklyStats,
            staleApps,
            activeApplicationsCount,
            activeOffersCount
        );
    }
}
