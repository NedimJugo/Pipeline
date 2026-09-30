using System;
using System.Collections.Generic;
using Pipeline.Domain.Enums;

namespace Pipeline.Application.Features.References.DTOs;

public record ReferenceListItemDto(
    Guid Id,
    string FullName,
    string Relationship,
    string? Email,
    string? Phone,
    string? Company,
    ReferenceConsent Consent,
    string? Notes,
    DateTime? LastNotifiedAt,
    int SharedCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ApplicationReferenceDto(
    Guid Id,
    Guid ApplicationId,
    string RoleTitle,
    string CompanyName,
    DateTime SharedAt,
    string? Outcome);

public record ReferenceDetailDto(
    Guid Id,
    string FullName,
    string Relationship,
    string? Email,
    string? Phone,
    string? Company,
    ReferenceConsent Consent,
    string? Notes,
    DateTime? LastNotifiedAt,
    List<ApplicationReferenceDto> SharedApplications,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateReferenceRequest(
    string FullName,
    string Relationship,
    string? Email = null,
    string? Phone = null,
    string? Company = null,
    ReferenceConsent Consent = ReferenceConsent.NotAsked,
    string? Notes = null);

public record UpdateReferenceRequest(
    string FullName,
    string Relationship,
    string? Email = null,
    string? Phone = null,
    string? Company = null,
    ReferenceConsent Consent = ReferenceConsent.NotAsked,
    string? Notes = null);

public record ShareReferenceRequest(
    Guid ApplicationId,
    string? Outcome = null,
    bool OverrideConsentWarning = false);

public record ShareReferenceResponse(
    bool Success,
    bool WarningTriggered,
    string? WarningMessage,
    ApplicationReferenceDto? SharedRecord);
