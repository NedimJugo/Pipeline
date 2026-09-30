using System;
using System.Collections.Generic;

namespace Pipeline.Application.Features.Analytics.DTOs;

public record FunnelStageDto(
    string Stage,
    int Count,
    double ConversionFromPrevious,
    double ConversionFromApplied
);

public record BreakdownMetricDto(
    string GroupKey,
    string Label,
    int TotalApplications,
    int Responses,
    double ResponseRate,
    int Interviews,
    double InterviewRate,
    int Offers,
    double OfferRate
);

public record StageDurationDto(
    string Stage,
    double AverageDays,
    double MedianDays,
    int SampleCount
);

public record WeeklyVelocityDto(
    string WeekLabel,
    DateTime WeekStartDate,
    int ApplicationsCount,
    int InterviewsCount
);

public record RejectionStageDto(
    string Stage,
    int Count,
    double Percentage
);

public record InsightCardDto(
    string Id,
    string Title,
    string Description,
    string Type, // "positive", "warning", "info"
    string? Metric,
    bool MinSampleSizeMet
);

public record AnalyticsOverviewDto(
    List<FunnelStageDto> Funnel,
    List<BreakdownMetricDto> BySource,
    List<BreakdownMetricDto> ByDocument,
    List<BreakdownMetricDto> ByWorkMode,
    List<StageDurationDto> StageDurations,
    List<WeeklyVelocityDto> WeeklyVelocity,
    List<RejectionStageDto> RejectionStages,
    List<InsightCardDto> Insights,
    int TotalApplications,
    int ActiveApplications,
    double OverallResponseRate,
    double OverallInterviewRate,
    int? DateRangeDays
);
