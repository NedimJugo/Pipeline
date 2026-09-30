using System;
using System.Collections.Generic;
using Pipeline.Domain.Enums;

namespace Pipeline.Application.Features.Tasks.DTOs;

public record TaskItemDto(
    Guid Id,
    Guid? ApplicationId,
    string? ApplicationTitle,
    string? CompanyName,
    Guid? ContactId,
    string? ContactName,
    Guid? InterviewId,
    string? InterviewTitle,
    string Title,
    string? Notes,
    DateTime? DueAt,
    DateTime? CompletedAt,
    TaskSource Source,
    string? AutoRuleKey,
    DateTime CreatedAt);

public record CreateTaskRequest(
    string Title,
    string? Notes = null,
    DateTime? DueAt = null,
    Guid? ApplicationId = null,
    Guid? ContactId = null,
    Guid? InterviewId = null,
    TaskSource Source = TaskSource.Manual,
    string? AutoRuleKey = null);

public record UpdateTaskRequest(
    string Title,
    string? Notes = null,
    DateTime? DueAt = null,
    Guid? ApplicationId = null,
    Guid? ContactId = null,
    Guid? InterviewId = null);

public record SnoozeTaskRequest(int Days = 1);

public record TaskFilterParams(
    string? View = null, // "today", "upcoming", "overdue", "done", "all"
    TaskSource? Source = null, // Manual, Auto
    string? Search = null,
    int Page = 1,
    int PageSize = 50);

public record PagedTasksResult(
    IReadOnlyList<TaskItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
