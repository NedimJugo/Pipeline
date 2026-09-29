using System;
using Pipeline.Domain.Enums;

namespace Pipeline.Application.Features.Interactions.DTOs;

public record InteractionDto(
    Guid Id,
    Guid? ContactId,
    string? ContactName,
    Guid? ApplicationId,
    string? ApplicationRole,
    string? CompanyName,
    InteractionChannel Channel,
    InteractionDirection Direction,
    DateTime OccurredAt,
    string Summary,
    string? SentContent,
    Guid? AttachmentDocumentId,
    bool FollowUpRequired,
    DateTime? FollowUpDueAt,
    DateTime CreatedAt);

public record LogInteractionRequest(
    string Summary,
    Guid? ContactId = null,
    Guid? ApplicationId = null,
    InteractionChannel Channel = InteractionChannel.Email,
    InteractionDirection Direction = InteractionDirection.Outbound,
    DateTime? OccurredAt = null,
    string? SentContent = null,
    Guid? AttachmentDocumentId = null,
    bool FollowUpRequired = false,
    DateTime? FollowUpDueAt = null);
