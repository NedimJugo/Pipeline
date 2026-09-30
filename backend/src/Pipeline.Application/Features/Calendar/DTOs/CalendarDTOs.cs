using System;

namespace Pipeline.Application.Features.Calendar.DTOs;

public enum CalendarEventType
{
    Interview,
    Task,
    OfferDeadline,
    FollowUp
}

public record CalendarEventDto(
    string Id,
    string Title,
    CalendarEventType Type,
    DateTime StartAt,
    DateTime? EndAt,
    bool IsAllDay,
    string? Description,
    string? Location,
    Guid? ApplicationId,
    string? RoleTitle,
    string? CompanyName,
    Guid? ContactId,
    string? ContactName,
    string? Status,
    string? Url
);

public record CalendarFeedUrlDto(
    string FeedUrl,
    string WebcalUrl,
    string Token
);
