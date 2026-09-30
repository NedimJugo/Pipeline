using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.References.DTOs;
using Pipeline.Application.Features.References.Services;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;

namespace Pipeline.Infrastructure.Services;

public class ReferenceService : IReferenceService
{
    private readonly PipelineDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public ReferenceService(PipelineDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<ReferenceListItemDto>> GetReferencesAsync(string? search = null, ReferenceConsent? consent = null, CancellationToken ct = default)
    {
        var q = _dbContext.JobReferences
            .Include(r => r.ApplicationReferences)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(r => r.FullName.ToLower().Contains(term)
                || (r.Company != null && r.Company.ToLower().Contains(term))
                || r.Relationship.ToLower().Contains(term)
                || (r.Notes != null && r.Notes.ToLower().Contains(term)));
        }

        if (consent.HasValue)
        {
            q = q.Where(r => r.Consent == consent.Value);
        }

        var list = await q.OrderBy(r => r.FullName).ToListAsync(ct);

        return list.Select(r => new ReferenceListItemDto(
            Id: r.Id,
            FullName: r.FullName,
            Relationship: r.Relationship,
            Email: r.Email,
            Phone: r.Phone,
            Company: r.Company,
            Consent: r.Consent,
            Notes: r.Notes,
            LastNotifiedAt: r.LastNotifiedAt,
            SharedCount: r.ApplicationReferences.Count,
            CreatedAt: r.CreatedAt,
            UpdatedAt: r.UpdatedAt)).ToList();
    }

    public async Task<ReferenceDetailDto> GetReferenceByIdAsync(Guid id, CancellationToken ct = default)
    {
        var r = await _dbContext.JobReferences
            .Include(r => r.ApplicationReferences)
                .ThenInclude(ar => ar.Application)
                    .ThenInclude(a => a!.Company)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (r == null)
        {
            throw new KeyNotFoundException($"Reference with ID '{id}' was not found.");
        }

        var sharedList = r.ApplicationReferences
            .OrderByDescending(ar => ar.SharedAt)
            .Select(ar => new ApplicationReferenceDto(
                Id: ar.Id,
                ApplicationId: ar.ApplicationId,
                RoleTitle: ar.Application?.RoleTitle ?? "Unknown Role",
                CompanyName: ar.Application?.Company?.Name ?? "Unknown Company",
                SharedAt: ar.SharedAt,
                Outcome: ar.Outcome))
            .ToList();

        return new ReferenceDetailDto(
            Id: r.Id,
            FullName: r.FullName,
            Relationship: r.Relationship,
            Email: r.Email,
            Phone: r.Phone,
            Company: r.Company,
            Consent: r.Consent,
            Notes: r.Notes,
            LastNotifiedAt: r.LastNotifiedAt,
            SharedApplications: sharedList,
            CreatedAt: r.CreatedAt,
            UpdatedAt: r.UpdatedAt);
    }

    public async Task<ReferenceDetailDto> CreateReferenceAsync(CreateReferenceRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Full name is required.", nameof(request.FullName));
        if (string.IsNullOrWhiteSpace(request.Relationship))
            throw new ArgumentException("Relationship is required.", nameof(request.Relationship));

        var userId = _currentUserService.UserId ?? Guid.Empty;

        var reference = new JobReference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FullName = request.FullName.Trim(),
            Relationship = request.Relationship.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            Company = request.Company?.Trim(),
            Consent = request.Consent,
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _dbContext.JobReferences.Add(reference);
        await _dbContext.SaveChangesAsync(ct);

        return await GetReferenceByIdAsync(reference.Id, ct);
    }

    public async Task<ReferenceDetailDto> UpdateReferenceAsync(Guid id, UpdateReferenceRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Full name is required.", nameof(request.FullName));
        if (string.IsNullOrWhiteSpace(request.Relationship))
            throw new ArgumentException("Relationship is required.", nameof(request.Relationship));

        var reference = await _dbContext.JobReferences
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (reference == null)
        {
            throw new KeyNotFoundException($"Reference with ID '{id}' was not found.");
        }

        reference.FullName = request.FullName.Trim();
        reference.Relationship = request.Relationship.Trim();
        reference.Email = request.Email?.Trim();
        reference.Phone = request.Phone?.Trim();
        reference.Company = request.Company?.Trim();
        reference.Consent = request.Consent;
        reference.Notes = request.Notes?.Trim();
        reference.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        return await GetReferenceByIdAsync(reference.Id, ct);
    }

    public async Task DeleteReferenceAsync(Guid id, CancellationToken ct = default)
    {
        var reference = await _dbContext.JobReferences
            .Include(r => r.ApplicationReferences)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (reference == null)
        {
            throw new KeyNotFoundException($"Reference with ID '{id}' was not found.");
        }

        _dbContext.ApplicationReferences.RemoveRange(reference.ApplicationReferences);
        _dbContext.JobReferences.Remove(reference);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<ShareReferenceResponse> ShareReferenceAsync(Guid referenceId, ShareReferenceRequest request, CancellationToken ct = default)
    {
        var reference = await _dbContext.JobReferences
            .Include(r => r.ApplicationReferences)
            .FirstOrDefaultAsync(r => r.Id == referenceId, ct);

        if (reference == null)
        {
            throw new KeyNotFoundException($"Reference with ID '{referenceId}' was not found.");
        }

        var application = await _dbContext.Applications
            .Include(a => a.Company)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);

        if (application == null)
        {
            throw new KeyNotFoundException($"Application with ID '{request.ApplicationId}' was not found.");
        }

        // Enforce Consent Rule
        if (reference.Consent != ReferenceConsent.Agreed && !request.OverrideConsentWarning)
        {
            return new ShareReferenceResponse(
                Success: false,
                WarningTriggered: true,
                WarningMessage: $"Consent for '{reference.FullName}' is currently marked as '{reference.Consent}'. It is recommended to secure agreement before sharing. You can override if you have verbal or offline permission.",
                SharedRecord: null);
        }

        var existing = await _dbContext.ApplicationReferences
            .FirstOrDefaultAsync(ar => ar.ReferenceId == referenceId && ar.ApplicationId == request.ApplicationId, ct);

        ApplicationReference appRef;
        if (existing != null)
        {
            existing.SharedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(request.Outcome))
            {
                existing.Outcome = request.Outcome.Trim();
            }
            appRef = existing;
        }
        else
        {
            appRef = new ApplicationReference
            {
                Id = Guid.NewGuid(),
                UserId = _currentUserService.UserId ?? Guid.Empty,
                ReferenceId = referenceId,
                ApplicationId = request.ApplicationId,
                SharedAt = DateTime.UtcNow,
                Outcome = request.Outcome?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _dbContext.ApplicationReferences.Add(appRef);
        }

        await _dbContext.SaveChangesAsync(ct);

        var dto = new ApplicationReferenceDto(
            Id: appRef.Id,
            ApplicationId: application.Id,
            RoleTitle: application.RoleTitle,
            CompanyName: application.Company?.Name ?? "Unknown Company",
            SharedAt: appRef.SharedAt,
            Outcome: appRef.Outcome);

        return new ShareReferenceResponse(
            Success: true,
            WarningTriggered: false,
            WarningMessage: null,
            SharedRecord: dto);
    }

    public async Task RemoveSharedReferenceAsync(Guid referenceId, Guid applicationReferenceId, CancellationToken ct = default)
    {
        var appRef = await _dbContext.ApplicationReferences
            .FirstOrDefaultAsync(ar => ar.ReferenceId == referenceId && ar.Id == applicationReferenceId, ct);

        if (appRef != null)
        {
            _dbContext.ApplicationReferences.Remove(appRef);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task RecordNotificationAsync(Guid referenceId, CancellationToken ct = default)
    {
        var reference = await _dbContext.JobReferences
            .FirstOrDefaultAsync(r => r.Id == referenceId, ct);

        if (reference == null)
        {
            throw new KeyNotFoundException($"Reference with ID '{referenceId}' was not found.");
        }

        reference.LastNotifiedAt = DateTime.UtcNow;
        reference.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<List<ApplicationReferenceDto>> GetApplicationReferencesAsync(Guid applicationId, CancellationToken ct = default)
    {
        var appRefs = await _dbContext.ApplicationReferences
            .Include(ar => ar.Reference)
            .Include(ar => ar.Application)
                .ThenInclude(a => a!.Company)
            .Where(ar => ar.ApplicationId == applicationId)
            .OrderByDescending(ar => ar.SharedAt)
            .ToListAsync(ct);

        return appRefs.Select(ar => new ApplicationReferenceDto(
            Id: ar.Id,
            ApplicationId: ar.ApplicationId,
            RoleTitle: ar.Application?.RoleTitle ?? "Unknown Role",
            CompanyName: ar.Application?.Company?.Name ?? "Unknown Company",
            SharedAt: ar.SharedAt,
            Outcome: ar.Outcome)).ToList();
    }
}
