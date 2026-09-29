using System;

namespace Pipeline.Application.Features.Companies.DTOs;

public record CompanyDto(
    Guid Id,
    string Name,
    string? Website,
    string? Industry,
    string? Size,
    string? Location,
    string? Notes,
    string? LinkedInUrl);

public record CreateCompanyRequest(
    string Name,
    string? Website = null,
    string? Industry = null,
    string? Size = null,
    string? Location = null,
    string? Notes = null,
    string? LinkedInUrl = null);

public record UpdateCompanyRequest(
    string Name,
    string? Website = null,
    string? Industry = null,
    string? Size = null,
    string? Location = null,
    string? Notes = null,
    string? LinkedInUrl = null);
