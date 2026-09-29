using System;
using System.Collections.Generic;
using Pipeline.Application.Features.Interactions.DTOs;
using Pipeline.Domain.Enums;

namespace Pipeline.Application.Features.Contacts.DTOs;

public record ContactListItemDto(
    Guid Id,
    string FullName,
    string? Role,
    Guid? CompanyId,
    string? CompanyName,
    string? Email,
    string? Phone,
    string? LinkedInUrl,
    ContactType Type,
    ContactWarmth Warmth,
    int? DaysSinceLastContact,
    DateTime? LastContactedAt,
    DateTime? NextFollowUpAt,
    int LinkedApplicationsCount,
    DateTime CreatedAt);

public record ContactDetailDto(
    Guid Id,
    string FullName,
    string? Role,
    Guid? CompanyId,
    string? CompanyName,
    string? Email,
    string? Phone,
    string? LinkedInUrl,
    ContactType Type,
    ContactWarmth Warmth,
    int? DaysSinceLastContact,
    DateTime? LastContactedAt,
    DateTime? NextFollowUpAt,
    string? Notes,
    IReadOnlyList<LinkedApplicationDto> LinkedApplications,
    IReadOnlyList<InteractionDto> RecentInteractions,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record LinkedApplicationDto(
    Guid ApplicationId,
    string RoleTitle,
    string CompanyName,
    ApplicationStatus Status,
    string? RoleInProcess);

public record CreateContactRequest(
    string FullName,
    Guid? CompanyId = null,
    string? CompanyName = null,
    string? Role = null,
    string? Email = null,
    string? Phone = null,
    string? LinkedInUrl = null,
    ContactType Type = ContactType.Recruiter,
    string? Notes = null,
    Guid? ApplicationId = null,
    string? RoleInProcess = null);

public record UpdateContactRequest(
    string FullName,
    Guid? CompanyId = null,
    string? CompanyName = null,
    string? Role = null,
    string? Email = null,
    string? Phone = null,
    string? LinkedInUrl = null,
    ContactType Type = ContactType.Recruiter,
    string? Notes = null,
    DateTime? NextFollowUpAt = null);

public record ContactFilterDto(
    string? Search = null,
    Guid? CompanyId = null,
    ContactType? Type = null,
    ContactWarmth? Warmth = null);

public record LinkApplicationContactRequest(
    Guid ApplicationId,
    string? RoleInProcess = null);
