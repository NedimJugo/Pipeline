using System;
using System.Collections.Generic;
using Pipeline.Domain.Enums;

namespace Pipeline.Application.Features.Users.DTOs;

public record UserSettingsProfileDto(
    Guid Id,
    string Email,
    string? DisplayName,
    string? TargetRole,
    string? Seniority,
    string? Location,
    decimal? SalaryExpectationMin,
    decimal? SalaryExpectationMax,
    string Currency,
    SearchStatus SearchStatus,
    string Timezone,
    int StaleAfterDays,
    bool OnboardingCompleted,
    string NotificationPrefs,
    DateTime CreatedAt);

public record UpdateProfileRequest(
    string? DisplayName = null,
    string? TargetRole = null,
    string? Seniority = null,
    string? Location = null,
    decimal? SalaryExpectationMin = null,
    decimal? SalaryExpectationMax = null,
    string Currency = "USD",
    SearchStatus SearchStatus = SearchStatus.Active,
    string Timezone = "UTC",
    bool? OnboardingCompleted = null);

public record UpdatePreferencesRequest(
    int? StaleAfterDays = null,
    string? NotificationPrefs = null);

public record CsvImportError(int Row, string Field, string Message);

public record CsvImportResultDto(
    int TotalProcessed,
    int CreatedCount,
    int UpdatedCount,
    int FailedCount,
    List<CsvImportError> Errors);

public record GdprExportDto(
    UserSettingsProfileDto Profile,
    object Applications,
    object Companies,
    object Contacts,
    object Interviews,
    object Tasks,
    object Documents,
    object References,
    object Templates,
    DateTime ExportedAtUtc);
