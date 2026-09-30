using System;
using System.Collections.Generic;
using Pipeline.Domain.Enums;

namespace Pipeline.Application.Features.Offers.DTOs;

public record UpdateOfferDetailsRequest(
    decimal? OfferSalary = null,
    decimal? OfferBonus = null,
    string? OfferBenefits = null,
    DateTime? OfferDeadline = null,
    string? OfferNegotiationNotes = null);

public record OfferComparisonItemDto(
    Guid ApplicationId,
    string RoleTitle,
    Guid CompanyId,
    string CompanyName,
    string? CompanyWebsite,
    ApplicationStatus Status,
    WorkMode WorkMode,
    EmploymentType EmploymentType,
    string? Location,
    decimal? OfferSalary,
    decimal? OfferBonus,
    decimal TotalCompensation,
    string Currency,
    string? OfferBenefits,
    DateTime? OfferDeadline,
    int? DaysUntilDeadline,
    string? OfferNegotiationNotes,
    string? Pros,
    string? Cons,
    int ExcitementRating,
    int Priority);

public record OfferComparisonViewDto(
    List<OfferComparisonItemDto> Offers,
    List<string> AvailableCriteria);
