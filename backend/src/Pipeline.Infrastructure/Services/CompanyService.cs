using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Companies.DTOs;
using Pipeline.Application.Features.Companies.Services;
using Pipeline.Domain.Entities;
using Pipeline.Infrastructure.Persistence;

namespace Pipeline.Infrastructure.Services;

public class CompanyService : ICompanyService
{
    private readonly PipelineDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CompanyService(PipelineDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<CompanyDto>> SearchCompaniesAsync(string? query, CancellationToken ct = default)
    {
        var dbQuery = _dbContext.Companies.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLower();
            dbQuery = dbQuery.Where(c => c.Name.ToLower().Contains(q) || (c.Industry != null && c.Industry.ToLower().Contains(q)));
        }

        var companies = await dbQuery
            .OrderBy(c => c.Name)
            .Take(20)
            .Select(c => MapToDto(c))
            .ToListAsync(ct);

        return companies;
    }

    public async Task<CompanyDto> GetOrCreateCompanyAsync(string name, string? website = null, CancellationToken ct = default)
    {
        var trimmedName = name.Trim();
        var existing = await _dbContext.Companies
            .FirstOrDefaultAsync(c => c.Name.ToLower() == trimmedName.ToLower(), ct);

        if (existing != null)
        {
            if (string.IsNullOrWhiteSpace(existing.Website) && !string.IsNullOrWhiteSpace(website))
            {
                existing.Website = website.Trim();
                await _dbContext.SaveChangesAsync(ct);
            }
            return MapToDto(existing);
        }

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            Website = website?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Companies.Add(company);
        await _dbContext.SaveChangesAsync(ct);

        return MapToDto(company);
    }

    public async Task<CompanyDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var company = await _dbContext.Companies.FindAsync(new object[] { id }, ct);
        return company != null ? MapToDto(company) : null;
    }

    public async Task<CompanyDto> CreateAsync(CreateCompanyRequest request, CancellationToken ct = default)
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Website = request.Website?.Trim(),
            Industry = request.Industry?.Trim(),
            Size = request.Size?.Trim(),
            Location = request.Location?.Trim(),
            Notes = request.Notes?.Trim(),
            LinkedInUrl = request.LinkedInUrl?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Companies.Add(company);
        await _dbContext.SaveChangesAsync(ct);

        return MapToDto(company);
    }

    public async Task<CompanyDto> UpdateAsync(Guid id, UpdateCompanyRequest request, CancellationToken ct = default)
    {
        var company = await _dbContext.Companies.FindAsync(new object[] { id }, ct);
        if (company == null)
        {
            throw new KeyNotFoundException($"Company with ID '{id}' was not found.");
        }

        company.Name = request.Name.Trim();
        company.Website = request.Website?.Trim();
        company.Industry = request.Industry?.Trim();
        company.Size = request.Size?.Trim();
        company.Location = request.Location?.Trim();
        company.Notes = request.Notes?.Trim();
        company.LinkedInUrl = request.LinkedInUrl?.Trim();
        company.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        return MapToDto(company);
    }

    private static CompanyDto MapToDto(Company c) =>
        new(
            Id: c.Id,
            Name: c.Name,
            Website: c.Website,
            Industry: c.Industry,
            Size: c.Size,
            Location: c.Location,
            Notes: c.Notes,
            LinkedInUrl: c.LinkedInUrl);
}
