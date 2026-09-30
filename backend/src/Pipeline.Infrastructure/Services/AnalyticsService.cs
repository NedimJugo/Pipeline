using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Analytics.DTOs;
using Pipeline.Application.Features.Analytics.Services;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly PipelineDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public AnalyticsService(PipelineDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(int? days = null, CancellationToken ct = default)
    {
        var funnel = await GetFunnelAsync(days, ct);
        var bySource = await GetBySourceAsync(days, ct);
        var byDoc = await GetByDocumentAsync(days, ct);
        var byWorkMode = await GetByWorkModeAsync(days, ct);
        var durations = await GetStageDurationsAsync(days, ct);
        var velocity = await GetWeeklyVelocityAsync(days, ct);
        var rejections = await GetRejectionStagesAsync(days, ct);
        var insights = await GetInsightsAsync(days, ct);

        var apps = await GetFilteredApplicationsAsync(days, ct);
        var total = apps.Count;
        var active = apps.Count(a => a.Status != ApplicationStatus.Accepted && a.Status != ApplicationStatus.Rejected && a.Status != ApplicationStatus.Ghosted);

        // Overall response rate: applications that reached Screening, Interview, Offer, or Accepted out of those applied
        var appliedOrFurther = apps.Where(a => a.Status != ApplicationStatus.Wishlist).ToList();
        var responses = appliedOrFurther.Count(a => a.Status != ApplicationStatus.Applied);
        var responseRate = appliedOrFurther.Count > 0 ? Math.Round((double)responses / appliedOrFurther.Count * 100, 1) : 0.0;

        var interviews = appliedOrFurther.Count(a => a.Status == ApplicationStatus.Interview || a.Status == ApplicationStatus.Offer || a.Status == ApplicationStatus.Accepted);
        var interviewRate = appliedOrFurther.Count > 0 ? Math.Round((double)interviews / appliedOrFurther.Count * 100, 1) : 0.0;

        return new AnalyticsOverviewDto(
            Funnel: funnel,
            BySource: bySource,
            ByDocument: byDoc,
            ByWorkMode: byWorkMode,
            StageDurations: durations,
            WeeklyVelocity: velocity,
            RejectionStages: rejections,
            Insights: insights,
            TotalApplications: total,
            ActiveApplications: active,
            OverallResponseRate: responseRate,
            OverallInterviewRate: interviewRate,
            DateRangeDays: days);
    }

    public async Task<List<FunnelStageDto>> GetFunnelAsync(int? days = null, CancellationToken ct = default)
    {
        var apps = await GetFilteredApplicationsAsync(days, ct);

        // Stage progress hierarchy
        // Applied: applications that advanced to Applied or higher
        // Screening: advanced to Screening, Interview, Offer, Accepted
        // Interview: advanced to Interview, Offer, Accepted
        // Offer: advanced to Offer, Accepted
        // Accepted: Accepted
        int appliedCount = apps.Count(a => a.Status >= ApplicationStatus.Applied);
        int screeningCount = apps.Count(a => a.Status >= ApplicationStatus.Screening);
        int interviewCount = apps.Count(a => a.Status >= ApplicationStatus.Interview);
        int offerCount = apps.Count(a => a.Status >= ApplicationStatus.Offer);
        int acceptedCount = apps.Count(a => a.Status == ApplicationStatus.Accepted);

        var stages = new List<(string Name, int Count)>
        {
            ("Applied", appliedCount),
            ("Screening", screeningCount),
            ("Interview", interviewCount),
            ("Offer", offerCount),
            ("Accepted", acceptedCount)
        };

        var result = new List<FunnelStageDto>();
        int previousCount = appliedCount;

        foreach (var (name, count) in stages)
        {
            double convFromPrev = previousCount > 0 ? Math.Round((double)count / previousCount * 100, 1) : 0.0;
            double convFromApplied = appliedCount > 0 ? Math.Round((double)count / appliedCount * 100, 1) : 0.0;

            result.Add(new FunnelStageDto(name, count, convFromPrev, convFromApplied));
            previousCount = count;
        }

        return result;
    }

    public async Task<List<BreakdownMetricDto>> GetBySourceAsync(int? days = null, CancellationToken ct = default)
    {
        var apps = await GetFilteredApplicationsAsync(days, ct);

        var grouped = apps
            .GroupBy(a => a.Source.ToString())
            .ToList();

        return ComputeBreakdowns(grouped);
    }

    public async Task<List<BreakdownMetricDto>> GetByDocumentAsync(int? days = null, CancellationToken ct = default)
    {
        var apps = await GetFilteredApplicationsAsync(days, ct);

        var grouped = apps
            .GroupBy(a => a.DocumentVersionCv != null
                ? $"{a.DocumentVersionCv.Document?.Title ?? "CV"} ({a.DocumentVersionCv.VersionLabel})"
                : "No CV Linked")
            .ToList();

        return ComputeBreakdowns(grouped);
    }

    public async Task<List<BreakdownMetricDto>> GetByWorkModeAsync(int? days = null, CancellationToken ct = default)
    {
        var apps = await GetFilteredApplicationsAsync(days, ct);

        var grouped = apps
            .GroupBy(a => a.WorkMode.ToString())
            .ToList();

        return ComputeBreakdowns(grouped);
    }

    public async Task<List<StageDurationDto>> GetStageDurationsAsync(int? days = null, CancellationToken ct = default)
    {
        var cutoff = days.HasValue ? DateTime.UtcNow.AddDays(-days.Value) : (DateTime?)null;

        var q = _dbContext.ApplicationStatusHistories
            .Include(h => h.Application)
            .AsNoTracking()
            .AsQueryable();

        if (cutoff.HasValue)
        {
            q = q.Where(h => h.ChangedAt >= cutoff.Value);
        }

        var histories = await q.ToListAsync(ct);

        // Group by application to compute duration spent in FromStatus
        var appHistories = histories
            .GroupBy(h => h.ApplicationId)
            .ToList();

        var stageDurations = new Dictionary<ApplicationStatus, List<double>>();

        foreach (var group in appHistories)
        {
            var ordered = group.OrderBy(h => h.ChangedAt).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                var h = ordered[i];
                DateTime start = (i == 0 && h.Application != null)
                    ? h.Application.CreatedAt
                    : (i > 0 ? ordered[i - 1].ChangedAt : h.ChangedAt);

                var daysInStage = Math.Max(0, (h.ChangedAt - start).TotalDays);
                if (!stageDurations.ContainsKey(h.FromStatus))
                {
                    stageDurations[h.FromStatus] = new List<double>();
                }
                stageDurations[h.FromStatus].Add(daysInStage);
            }
        }

        var trackedStatuses = new[]
        {
            ApplicationStatus.Wishlist,
            ApplicationStatus.Applied,
            ApplicationStatus.Screening,
            ApplicationStatus.Interview,
            ApplicationStatus.Offer
        };

        var result = new List<StageDurationDto>();

        foreach (var status in trackedStatuses)
        {
            if (!stageDurations.TryGetValue(status, out var list) || list.Count == 0)
            {
                result.Add(new StageDurationDto(status.ToString(), 0, 0, 0));
                continue;
            }

            var sorted = list.OrderBy(d => d).ToList();
            var count = sorted.Count;
            var avg = Math.Round(sorted.Average(), 1);

            double median;
            if (count % 2 == 1)
            {
                median = Math.Round(sorted[count / 2], 1);
            }
            else
            {
                median = Math.Round((sorted[(count / 2) - 1] + sorted[count / 2]) / 2.0, 1);
            }

            result.Add(new StageDurationDto(status.ToString(), avg, median, count));
        }

        return result;
    }

    public async Task<List<WeeklyVelocityDto>> GetWeeklyVelocityAsync(int? days = null, CancellationToken ct = default)
    {
        int weeksCount = days.HasValue ? Math.Max(4, (int)Math.Ceiling(days.Value / 7.0)) : 8;
        var endSunday = DateTime.UtcNow.Date.AddDays(7 - (int)DateTime.UtcNow.DayOfWeek);
        var startMonday = endSunday.AddDays(-weeksCount * 7);

        var apps = await _dbContext.Applications
            .AsNoTracking()
            .Where(a => a.CreatedAt >= startMonday)
            .ToListAsync(ct);

        var interviews = await _dbContext.Interviews
            .AsNoTracking()
            .Where(i => i.ScheduledAt >= startMonday)
            .ToListAsync(ct);

        var result = new List<WeeklyVelocityDto>();

        for (int i = 0; i < weeksCount; i++)
        {
            var weekStart = startMonday.AddDays(i * 7);
            var weekEnd = weekStart.AddDays(7);

            var weekApps = apps.Count(a => a.CreatedAt >= weekStart && a.CreatedAt < weekEnd);
            var weekInterviews = interviews.Count(inv => inv.ScheduledAt >= weekStart && inv.ScheduledAt < weekEnd);
            var label = $"{weekStart:MMM d}";

            result.Add(new WeeklyVelocityDto(label, weekStart, weekApps, weekInterviews));
        }

        return result;
    }

    public async Task<List<InsightCardDto>> GetInsightsAsync(int? days = null, CancellationToken ct = default)
    {
        var apps = await GetFilteredApplicationsAsync(days, ct);
        var insights = new List<InsightCardDto>();

        // Sample Size Check: minimum N >= 5
        if (apps.Count < 5)
        {
            insights.Add(new InsightCardDto(
                Id: "min_sample_size",
                Title: "Unlock Deeper Statistical Insights",
                Description: $"You currently have {apps.Count} application{(apps.Count == 1 ? "" : "s")} logged. Add at least 5 applications to unlock automatic conversion comparisons and pipeline benchmarks.",
                Type: "info",
                Metric: $"{apps.Count} / 5 Logged",
                MinSampleSizeMet: false));

            return insights;
        }

        // 1. Referral vs Other Source Comparison
        var referralApps = apps.Where(a => a.Source == ApplicationSource.Referral).ToList();
        var otherApps = apps.Where(a => a.Source != ApplicationSource.Referral).ToList();

        if (referralApps.Count >= 2 && otherApps.Count >= 3)
        {
            var refInterviewRate = (double)referralApps.Count(a => a.Status >= ApplicationStatus.Interview) / referralApps.Count;
            var otherInterviewRate = (double)otherApps.Count(a => a.Status >= ApplicationStatus.Interview) / otherApps.Count;

            if (otherInterviewRate > 0 && refInterviewRate > otherInterviewRate)
            {
                var multiplier = Math.Round(refInterviewRate / otherInterviewRate, 1);
                insights.Add(new InsightCardDto(
                    Id: "referral_advantage",
                    Title: "Referrals Yield Higher Interview Rates",
                    Description: $"Referral applications convert to interviews {multiplier}x more than job boards and cold applications ({Math.Round(refInterviewRate * 100)}% vs {Math.Round(otherInterviewRate * 100)}%).",
                    Type: "positive",
                    Metric: $"{multiplier}x Higher",
                    MinSampleSizeMet: true));
            }
        }

        // 2. CV Version Comparison
        var cvGroups = apps
            .Where(a => a.DocumentVersionCv != null)
            .GroupBy(a => a.DocumentVersionCv!.VersionLabel)
            .Where(g => g.Count() >= 2)
            .ToList();

        if (cvGroups.Count >= 2)
        {
            var bestCv = cvGroups
                .OrderByDescending(g => (double)g.Count(a => a.Status >= ApplicationStatus.Screening) / g.Count())
                .First();

            var otherCvsCount = apps.Count(a => a.DocumentVersionCv != null && a.DocumentVersionCv.VersionLabel != bestCv.Key);
            var otherCvsResponses = apps.Count(a => a.DocumentVersionCv != null && a.DocumentVersionCv.VersionLabel != bestCv.Key && a.Status >= ApplicationStatus.Screening);

            if (otherCvsCount >= 2)
            {
                var bestRate = Math.Round((double)bestCv.Count(a => a.Status >= ApplicationStatus.Screening) / bestCv.Count() * 100, 1);
                var otherRate = Math.Round((double)otherCvsResponses / otherCvsCount * 100, 1);

                if (bestRate > otherRate)
                {
                    insights.Add(new InsightCardDto(
                        Id: "cv_version_efficiency",
                        Title: $"CV '{bestCv.Key}' Outperforms Other Versions",
                        Description: $"Applications using CV version '{bestCv.Key}' boast a {bestRate}% response rate compared to {otherRate}% for your other CV versions.",
                        Type: "positive",
                        Metric: $"{bestRate}% Response",
                        MinSampleSizeMet: true));
                }
            }
        }

        // 3. Rejection Stage Diagnostic
        var rejections = apps.Where(a => a.Status == ApplicationStatus.Rejected || a.Status == ApplicationStatus.Ghosted).ToList();
        if (rejections.Count >= 3)
        {
            var mostFrequentStage = rejections
                .GroupBy(a => a.RejectionStage ?? "Applied")
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (mostFrequentStage != null)
            {
                insights.Add(new InsightCardDto(
                    Id: "rejection_bottleneck",
                    Title: $"Main Rejection Drop-off: {mostFrequentStage.Key}",
                    Description: $"You experience the most candidate drop-offs during the '{mostFrequentStage.Key}' phase ({mostFrequentStage.Count()} of {rejections.Count} concluded searches).",
                    Type: "warning",
                    Metric: $"{mostFrequentStage.Count()} / {rejections.Count}",
                    MinSampleSizeMet: true));
            }
        }

        // 4. Stale Applications Warning
        var staleCount = apps.Count(a =>
            a.Status != ApplicationStatus.Accepted &&
            a.Status != ApplicationStatus.Rejected &&
            a.Status != ApplicationStatus.Ghosted &&
            a.StatusChangedAt <= DateTime.UtcNow.AddDays(-14));

        if (staleCount > 0)
        {
            insights.Add(new InsightCardDto(
                Id: "stale_applications",
                Title: "Active Inquiries Awaiting Updates",
                Description: $"{staleCount} active application{(staleCount == 1 ? " has" : "s have")} had no status updates for 14+ days. Consider reaching out with a polite follow-up.",
                Type: "info",
                Metric: $"{staleCount} Stale",
                MinSampleSizeMet: true));
        }

        return insights;
    }

    private async Task<List<JobApplication>> GetFilteredApplicationsAsync(int? days, CancellationToken ct)
    {
        var cutoff = days.HasValue ? DateTime.UtcNow.AddDays(-days.Value) : (DateTime?)null;

        var q = _dbContext.Applications
            .Include(a => a.Company)
            .Include(a => a.DocumentVersionCv)
                .ThenInclude(v => v!.Document)
            .AsNoTracking()
            .AsQueryable();

        if (cutoff.HasValue)
        {
            q = q.Where(a => a.CreatedAt >= cutoff.Value);
        }

        return await q.ToListAsync(ct);
    }

    private async Task<List<RejectionStageDto>> GetRejectionStagesAsync(int? days, CancellationToken ct)
    {
        var apps = await GetFilteredApplicationsAsync(days, ct);
        var closedApps = apps.Where(a => a.Status == ApplicationStatus.Rejected || a.Status == ApplicationStatus.Ghosted).ToList();

        if (closedApps.Count == 0) return new List<RejectionStageDto>();

        var grouped = closedApps
            .GroupBy(a => a.RejectionStage ?? (a.Status == ApplicationStatus.Ghosted ? "Ghosted" : "Resume Screen"))
            .Select(g => new RejectionStageDto(
                Stage: g.Key,
                Count: g.Count(),
                Percentage: Math.Round((double)g.Count() / closedApps.Count * 100, 1)))
            .OrderByDescending(r => r.Count)
            .ToList();

        return grouped;
    }

    private static List<BreakdownMetricDto> ComputeBreakdowns(IEnumerable<IGrouping<string, JobApplication>> groups)
    {
        var result = new List<BreakdownMetricDto>();

        foreach (var group in groups)
        {
            var total = group.Count();
            var applied = group.Count(a => a.Status >= ApplicationStatus.Applied);
            var responses = group.Count(a => a.Status >= ApplicationStatus.Screening);
            var interviews = group.Count(a => a.Status >= ApplicationStatus.Interview);
            var offers = group.Count(a => a.Status >= ApplicationStatus.Offer);

            double respRate = total > 0 ? Math.Round((double)responses / total * 100, 1) : 0.0;
            double intRate = total > 0 ? Math.Round((double)interviews / total * 100, 1) : 0.0;
            double offRate = total > 0 ? Math.Round((double)offers / total * 100, 1) : 0.0;

            result.Add(new BreakdownMetricDto(
                GroupKey: group.Key,
                Label: group.Key,
                TotalApplications: total,
                Responses: responses,
                ResponseRate: respRate,
                Interviews: interviews,
                InterviewRate: intRate,
                Offers: offers,
                OfferRate: offRate));
        }

        return result.OrderByDescending(r => r.TotalApplications).ToList();
    }
}
