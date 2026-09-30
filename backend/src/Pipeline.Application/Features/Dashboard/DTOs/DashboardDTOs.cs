using System;
using System.Collections.Generic;
using Pipeline.Application.Features.Tasks.DTOs;
using Pipeline.Domain.Enums;

namespace Pipeline.Application.Features.Dashboard.DTOs;

public record UpcomingInterviewDto(
    Guid Id,
    Guid ApplicationId,
    string RoleTitle,
    string CompanyName,
    InterviewType Type,
    InterviewFormat Format,
    DateTime ScheduledAt,
    string CountdownText,
    int PrepChecklistTotal,
    int PrepChecklistCompleted,
    int PrepProgressPercent);

public record DashboardWeeklyStatsDto(
    int ApplicationsSent,
    int Responses,
    int Interviews,
    int Offers);

public record StaleApplicationDto(
    Guid Id,
    string CompanyName,
    string RoleTitle,
    ApplicationStatus Status,
    int DaysSinceUpdate,
    int StaleAfterDays);

public record DashboardSummaryDto(
    string Greeting,
    SearchStatus SearchStatus,
    string? TargetRole,
    IReadOnlyList<TaskItemDto> DoToday,
    IReadOnlyList<UpcomingInterviewDto> UpcomingInterviews,
    DashboardWeeklyStatsDto ThisWeekStats,
    IReadOnlyList<StaleApplicationDto> StaleApplications,
    int ActiveApplicationsCount,
    int ActiveOffersCount);
