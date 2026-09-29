using System;
using System.Collections.Generic;
using Pipeline.Domain.Enums;

namespace Pipeline.Application.Features.Interviews.DTOs;

public record PrepChecklistItemDto(string Text, bool Done = false);

public record InterviewContactDto(
    Guid ContactId,
    string FullName,
    string? Role,
    string? Email,
    string? LinkedInUrl,
    ContactWarmth Warmth);

public record InterviewQuestionDto(
    Guid Id,
    string Question,
    string? MyAnswer,
    InterviewQuestionCategory Category,
    int Difficulty,
    bool WasPrepared);

public record InterviewListItemDto(
    Guid Id,
    Guid ApplicationId,
    string RoleTitle,
    string CompanyName,
    InterviewType Type,
    InterviewFormat Format,
    DateTime ScheduledAt,
    int DurationMinutes,
    InterviewStatus Status,
    string? Location,
    string? MeetingLink,
    int InterviewersCount,
    int QuestionsCount,
    int? SelfRating,
    DateTime CreatedAt);

public record InterviewDetailDto(
    Guid Id,
    Guid ApplicationId,
    string RoleTitle,
    string CompanyName,
    InterviewType Type,
    InterviewFormat Format,
    DateTime ScheduledAt,
    int DurationMinutes,
    InterviewStatus Status,
    string? Location,
    string? MeetingLink,
    string? PrepNotes,
    IReadOnlyList<PrepChecklistItemDto> PrepChecklist,
    int? SelfRating,
    string? WentWell,
    string? ToImprove,
    bool ThankYouSent,
    string? OutcomeNotes,
    IReadOnlyList<InterviewContactDto> Interviewers,
    IReadOnlyList<InterviewQuestionDto> Questions,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateInterviewRequest(
    Guid ApplicationId,
    InterviewType Type = InterviewType.Technical,
    InterviewFormat Format = InterviewFormat.Video,
    DateTime ScheduledAt = default,
    int DurationMinutes = 45,
    string? Location = null,
    string? MeetingLink = null,
    List<Guid>? InterviewerContactIds = null,
    string? PrepNotes = null,
    List<PrepChecklistItemDto>? CustomChecklist = null);

public record UpdateInterviewRequest(
    InterviewType Type,
    InterviewFormat Format,
    DateTime ScheduledAt,
    int DurationMinutes,
    string? Location = null,
    string? MeetingLink = null,
    InterviewStatus Status = InterviewStatus.Scheduled,
    List<Guid>? InterviewerContactIds = null,
    string? PrepNotes = null);

public record UpdateDebriefRequest(
    int? SelfRating,
    string? WentWell,
    string? ToImprove,
    bool ThankYouSent,
    string? OutcomeNotes);

public record UpdatePrepChecklistRequest(
    List<PrepChecklistItemDto> Checklist);

public record AddQuestionRequest(
    string Question,
    string? MyAnswer = null,
    InterviewQuestionCategory Category = InterviewQuestionCategory.Behavioral,
    int Difficulty = 3,
    bool WasPrepared = true);

public record UpdateQuestionRequest(
    string Question,
    string? MyAnswer = null,
    InterviewQuestionCategory Category = InterviewQuestionCategory.Behavioral,
    int Difficulty = 3,
    bool WasPrepared = true);

public record InterviewFilterDto(
    Guid? ApplicationId = null,
    InterviewStatus? Status = null,
    InterviewType? Type = null,
    bool? UpcomingOnly = null);
