using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Offers.DTOs;
using Pipeline.Application.Features.Offers.Services;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Infrastructure.Services;

public class OfferService : IOfferService
{
    private readonly PipelineDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    private static readonly List<string> DefaultCriteria = new()
    {
        "Compensation",
        "Work-Life & Remote",
        "Career Growth",
        "Benefits & Perks",
        "Culture & Team"
    };

    public OfferService(PipelineDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<OfferComparisonItemDto> UpdateOfferDetailsAsync(Guid applicationId, UpdateOfferDetailsRequest request, CancellationToken ct = default)
    {
        var app = await _dbContext.Applications
            .Include(a => a.Company)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

        if (app == null)
        {
            throw new KeyNotFoundException($"Application with ID '{applicationId}' was not found.");
        }

        if (request.OfferSalary.HasValue) app.OfferSalary = request.OfferSalary.Value;
        if (request.OfferBonus.HasValue) app.OfferBonus = request.OfferBonus.Value;
        if (request.OfferBenefits != null) app.OfferBenefits = request.OfferBenefits;
        if (request.OfferDeadline.HasValue) app.OfferDeadline = request.OfferDeadline.Value;
        if (request.OfferNegotiationNotes != null) app.OfferNegotiationNotes = request.OfferNegotiationNotes;

        app.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        return MapToItemDto(app);
    }

    public async Task<List<OfferComparisonItemDto>> GetAvailableOffersAsync(CancellationToken ct = default)
    {
        var apps = await _dbContext.Applications
            .Include(a => a.Company)
            .AsNoTracking()
            .Where(a => a.Status == ApplicationStatus.Offer || a.OfferSalary != null || a.OfferDeadline != null)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        return apps
            .OrderByDescending(a => (a.OfferSalary ?? 0) + (a.OfferBonus ?? 0))
            .Select(MapToItemDto)
            .ToList();
    }

    public async Task<OfferComparisonViewDto> GetOfferComparisonAsync(List<Guid>? applicationIds = null, CancellationToken ct = default)
    {
        List<JobApplication> apps;

        if (applicationIds != null && applicationIds.Count > 0)
        {
            apps = await _dbContext.Applications
                .Include(a => a.Company)
                .AsNoTracking()
                .Where(a => applicationIds.Contains(a.Id))
                .ToListAsync(ct);
        }
        else
        {
            // Default to offers with Offer status (up to 4)
            var candidates = await _dbContext.Applications
                .Include(a => a.Company)
                .AsNoTracking()
                .Where(a => a.Status == ApplicationStatus.Offer || a.OfferSalary != null)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(ct);

            apps = candidates
                .OrderByDescending(a => (a.OfferSalary ?? 0) + (a.OfferBonus ?? 0))
                .Take(4)
                .ToList();
        }

        var items = apps.Select(MapToItemDto).ToList();

        return new OfferComparisonViewDto(
            Offers: items,
            AvailableCriteria: DefaultCriteria);
    }

    private static OfferComparisonItemDto MapToItemDto(JobApplication a)
    {
        var salary = a.OfferSalary ?? a.SalaryMax ?? a.SalaryMin;
        var bonus = a.OfferBonus ?? 0;
        var total = (salary ?? 0) + bonus;

        int? daysUntilDeadline = null;
        if (a.OfferDeadline.HasValue)
        {
            var diff = (a.OfferDeadline.Value.Date - DateTime.UtcNow.Date).TotalDays;
            daysUntilDeadline = (int)diff;
        }

        return new OfferComparisonItemDto(
            ApplicationId: a.Id,
            RoleTitle: a.RoleTitle,
            CompanyId: a.CompanyId,
            CompanyName: a.Company?.Name ?? "Unknown Company",
            CompanyWebsite: a.Company?.Website,
            Status: a.Status,
            WorkMode: a.WorkMode,
            EmploymentType: a.EmploymentType,
            Location: a.Location,
            OfferSalary: a.OfferSalary,
            OfferBonus: a.OfferBonus,
            TotalCompensation: total,
            Currency: a.Currency,
            OfferBenefits: a.OfferBenefits,
            OfferDeadline: a.OfferDeadline,
            DaysUntilDeadline: daysUntilDeadline,
            OfferNegotiationNotes: a.OfferNegotiationNotes,
            Pros: a.Pros,
            Cons: a.Cons,
            ExcitementRating: a.ExcitementRating,
            Priority: a.Priority);
    }
}
