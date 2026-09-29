using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Applications.DTOs;
using Pipeline.Application.Features.Applications.Services;
using Pipeline.Application.Features.Companies.Services;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Infrastructure.Services;

public class ApplicationService : IApplicationService
{
    private readonly PipelineDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICompanyService _companyService;

    public ApplicationService(
        PipelineDbContext dbContext,
        ICurrentUserService currentUserService,
        ICompanyService companyService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _companyService = companyService;
    }

    public async Task<IReadOnlyList<ApplicationListItemDto>> GetApplicationsAsync(
        ApplicationFilterDto? filter = null,
        CancellationToken ct = default)
    {
        var query = _dbContext.Applications
            .Include(a => a.Company)
            .Include(a => a.Interviews.Where(i => i.ScheduledAt >= DateTime.UtcNow && i.Status == InterviewStatus.Scheduled))
            .AsNoTracking();

        if (filter != null)
        {
            if (filter.Status.HasValue)
            {
                query = query.Where(a => a.Status == filter.Status.Value);
            }
            if (filter.Source.HasValue)
            {
                query = query.Where(a => a.Source == filter.Source.Value);
            }
            if (filter.WorkMode.HasValue)
            {
                query = query.Where(a => a.WorkMode == filter.WorkMode.Value);
            }
            if (filter.Priority.HasValue)
            {
                query = query.Where(a => a.Priority == filter.Priority.Value);
            }
            if (filter.Favorite.HasValue)
            {
                query = query.Where(a => a.Favorite == filter.Favorite.Value);
            }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim().ToLower();
                query = query.Where(a => a.RoleTitle.ToLower().Contains(search) 
                    || (a.Company != null && a.Company.Name.ToLower().Contains(search))
                    || (a.JobDescription != null && a.JobDescription.ToLower().Contains(search))
                    || (a.Notes != null && a.Notes.ToLower().Contains(search)));
            }
        }

        var list = await query
            .OrderByDescending(a => a.StatusChangedAt)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;

        return list.Select(a =>
        {
            var nextInterview = a.Interviews
                .OrderBy(i => i.ScheduledAt)
                .Select(i => (DateTime?)i.ScheduledAt)
                .FirstOrDefault();

            var daysInStage = Math.Max(0, (now - a.StatusChangedAt).Days);

            return new ApplicationListItemDto(
                Id: a.Id,
                CompanyId: a.CompanyId,
                CompanyName: a.Company?.Name ?? "Unknown Company",
                RoleTitle: a.RoleTitle,
                JobUrl: a.JobUrl,
                Source: a.Source,
                SourceDetail: a.SourceDetail,
                Status: a.Status,
                StatusChangedAt: a.StatusChangedAt,
                AppliedAt: a.AppliedAt,
                WorkMode: a.WorkMode,
                EmploymentType: a.EmploymentType,
                Location: a.Location,
                SalaryMin: a.SalaryMin,
                SalaryMax: a.SalaryMax,
                Currency: a.Currency,
                Priority: a.Priority,
                Favorite: a.Favorite,
                ExcitementRating: a.ExcitementRating,
                DaysInStage: daysInStage,
                NextInterviewDate: nextInterview,
                ClosedReason: a.ClosedReason,
                RejectionStage: a.RejectionStage);
        }).ToList();
    }

    public async Task<ApplicationDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var app = await _dbContext.Applications
            .Include(a => a.Company)
            .Include(a => a.DocumentVersionCv)
            .Include(a => a.DocumentVersionCover)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (app == null)
        {
            throw new KeyNotFoundException($"Application with ID '{id}' was not found.");
        }

        return MapToDetailDto(app);
    }

    public async Task<ApplicationDetailDto> CreateApplicationAsync(CreateApplicationRequest request, CancellationToken ct = default)
    {
        Guid companyId;
        if (request.CompanyId.HasValue && request.CompanyId.Value != Guid.Empty)
        {
            companyId = request.CompanyId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(request.CompanyName))
        {
            var company = await _companyService.GetOrCreateCompanyAsync(request.CompanyName, null, ct);
            companyId = company.Id;
        }
        else
        {
            throw new ArgumentException("Either CompanyId or CompanyName must be provided.");
        }

        var now = DateTime.UtcNow;
        var application = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _currentUserService.UserId ?? Guid.Empty,
            CompanyId = companyId,
            RoleTitle = request.RoleTitle.Trim(),
            JobUrl = request.JobUrl?.Trim(),
            Source = request.Source,
            SourceDetail = request.SourceDetail?.Trim(),
            Status = request.Status,
            StatusChangedAt = now,
            AppliedAt = request.AppliedAt ?? (request.Status == ApplicationStatus.Applied ? now : null),
            WorkMode = request.WorkMode,
            EmploymentType = request.EmploymentType,
            Location = request.Location?.Trim(),
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax,
            Currency = request.Currency,
            JobDescription = request.JobDescription,
            Notes = request.Notes,
            Pros = request.Pros,
            Cons = request.Cons,
            Priority = request.Priority,
            Favorite = request.Favorite,
            ExcitementRating = request.ExcitementRating,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Applications.Add(application);

        // Record initial status history
        var history = new ApplicationStatusHistory
        {
            Id = Guid.NewGuid(),
            UserId = application.UserId,
            ApplicationId = application.Id,
            FromStatus = request.Status,
            ToStatus = request.Status,
            ChangedAt = now,
            Note = "Application added to pipeline"
        };
        _dbContext.ApplicationStatusHistories.Add(history);

        await _dbContext.SaveChangesAsync(ct);

        return await GetByIdAsync(application.Id, ct);
    }

    public async Task<ApplicationDetailDto> UpdateApplicationAsync(Guid id, UpdateApplicationRequest request, CancellationToken ct = default)
    {
        var app = await _dbContext.Applications
            .Include(a => a.Company)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (app == null)
        {
            throw new KeyNotFoundException($"Application with ID '{id}' was not found.");
        }

        if (request.CompanyId.HasValue && request.CompanyId.Value != Guid.Empty)
        {
            app.CompanyId = request.CompanyId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(request.CompanyName))
        {
            var comp = await _companyService.GetOrCreateCompanyAsync(request.CompanyName, null, ct);
            app.CompanyId = comp.Id;
        }

        app.RoleTitle = request.RoleTitle.Trim();
        app.JobUrl = request.JobUrl?.Trim();
        app.Source = request.Source;
        app.SourceDetail = request.SourceDetail?.Trim();
        app.AppliedAt = request.AppliedAt;
        app.WorkMode = request.WorkMode;
        app.EmploymentType = request.EmploymentType;
        app.Location = request.Location?.Trim();
        app.SalaryMin = request.SalaryMin;
        app.SalaryMax = request.SalaryMax;
        app.Currency = request.Currency;
        app.JobDescription = request.JobDescription;
        app.Notes = request.Notes;
        app.Pros = request.Pros;
        app.Cons = request.Cons;
        app.Priority = request.Priority;
        app.Favorite = request.Favorite;
        app.ExcitementRating = request.ExcitementRating;

        if (request.OfferSalary.HasValue) app.OfferSalary = request.OfferSalary;
        if (request.OfferBenefits != null) app.OfferBenefits = request.OfferBenefits;
        if (request.OfferDeadline.HasValue) app.OfferDeadline = request.OfferDeadline;
        if (request.ClosedReason != null) app.ClosedReason = request.ClosedReason;
        if (request.RejectionStage != null) app.RejectionStage = request.RejectionStage;
        if (request.LessonsLearned != null) app.LessonsLearned = request.LessonsLearned;

        app.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        return await GetByIdAsync(app.Id, ct);
    }

    public async Task<ApplicationDetailDto> UpdateStatusAsync(Guid id, UpdateStatusRequest request, CancellationToken ct = default)
    {
        var app = await _dbContext.Applications
            .Include(a => a.Company)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (app == null)
        {
            throw new KeyNotFoundException($"Application with ID '{id}' was not found.");
        }

        var oldStatus = app.Status;
        if (oldStatus != request.Status)
        {
            var now = DateTime.UtcNow;
            app.Status = request.Status;
            app.StatusChangedAt = now;
            app.UpdatedAt = now;

            if (request.Status == ApplicationStatus.Applied && app.AppliedAt == null)
            {
                app.AppliedAt = now;
            }

            if (!string.IsNullOrWhiteSpace(request.ClosedReason))
                app.ClosedReason = request.ClosedReason;
            if (!string.IsNullOrWhiteSpace(request.RejectionStage))
                app.RejectionStage = request.RejectionStage;
            if (!string.IsNullOrWhiteSpace(request.LessonsLearned))
                app.LessonsLearned = request.LessonsLearned;

            var history = new ApplicationStatusHistory
            {
                Id = Guid.NewGuid(),
                UserId = app.UserId,
                ApplicationId = app.Id,
                FromStatus = oldStatus,
                ToStatus = request.Status,
                ChangedAt = now,
                Note = request.Note
            };
            _dbContext.ApplicationStatusHistories.Add(history);

            await _dbContext.SaveChangesAsync(ct);
        }

        return await GetByIdAsync(app.Id, ct);
    }

    public async Task DeleteApplicationAsync(Guid id, CancellationToken ct = default)
    {
        var app = await _dbContext.Applications.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (app != null)
        {
            app.DeletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task<ApplicationDetailDto> DuplicateApplicationAsync(Guid id, CancellationToken ct = default)
    {
        var original = await _dbContext.Applications
            .Include(a => a.Company)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (original == null)
        {
            throw new KeyNotFoundException($"Application with ID '{id}' was not found.");
        }

        var now = DateTime.UtcNow;
        var duplicate = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _currentUserService.UserId ?? Guid.Empty,
            CompanyId = original.CompanyId,
            RoleTitle = $"[Copy] {original.RoleTitle}",
            JobUrl = original.JobUrl,
            Source = original.Source,
            SourceDetail = original.SourceDetail,
            Status = ApplicationStatus.Wishlist,
            StatusChangedAt = now,
            AppliedAt = null,
            WorkMode = original.WorkMode,
            EmploymentType = original.EmploymentType,
            Location = original.Location,
            SalaryMin = original.SalaryMin,
            SalaryMax = original.SalaryMax,
            Currency = original.Currency,
            JobDescription = original.JobDescription,
            Notes = original.Notes,
            Pros = original.Pros,
            Cons = original.Cons,
            Priority = original.Priority,
            Favorite = false,
            ExcitementRating = original.ExcitementRating,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Applications.Add(duplicate);

        var history = new ApplicationStatusHistory
        {
            Id = Guid.NewGuid(),
            UserId = duplicate.UserId,
            ApplicationId = duplicate.Id,
            FromStatus = ApplicationStatus.Wishlist,
            ToStatus = ApplicationStatus.Wishlist,
            ChangedAt = now,
            Note = "Duplicated from previous application"
        };
        _dbContext.ApplicationStatusHistories.Add(history);

        await _dbContext.SaveChangesAsync(ct);

        return await GetByIdAsync(duplicate.Id, ct);
    }

    public async Task<IReadOnlyList<ApplicationTimelineItemDto>> GetTimelineAsync(Guid id, CancellationToken ct = default)
    {
        var items = new List<ApplicationTimelineItemDto>();

        // Status history
        var statusHistories = await _dbContext.ApplicationStatusHistories
            .Where(h => h.ApplicationId == id)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(ct);

        foreach (var h in statusHistories)
        {
            items.Add(new ApplicationTimelineItemDto(
                Id: h.Id,
                Type: "StatusChange",
                Title: $"Stage changed to {h.ToStatus}",
                Description: h.Note,
                Timestamp: h.ChangedAt,
                Metadata: new Dictionary<string, string?>
                {
                    ["from"] = h.FromStatus.ToString(),
                    ["to"] = h.ToStatus.ToString()
                }));
        }

        // Interactions
        var interactions = await _dbContext.Interactions
            .Where(i => i.ApplicationId == id)
            .OrderByDescending(i => i.OccurredAt)
            .ToListAsync(ct);

        foreach (var i in interactions)
        {
            items.Add(new ApplicationTimelineItemDto(
                Id: i.Id,
                Type: "Interaction",
                Title: $"{i.Direction} via {i.Channel}",
                Description: i.Summary,
                Timestamp: i.OccurredAt));
        }

        // Interviews
        var interviews = await _dbContext.Interviews
            .Where(i => i.ApplicationId == id)
            .OrderByDescending(i => i.ScheduledAt)
            .ToListAsync(ct);

        foreach (var inv in interviews)
        {
            items.Add(new ApplicationTimelineItemDto(
                Id: inv.Id,
                Type: "Interview",
                Title: $"{inv.Type} Interview ({inv.Format})",
                Description: inv.OutcomeNotes ?? inv.PrepNotes ?? $"Status: {inv.Status}",
                Timestamp: inv.ScheduledAt));
        }

        // Tasks
        var tasks = await _dbContext.Tasks
            .Where(t => t.ApplicationId == id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        foreach (var t in tasks)
        {
            items.Add(new ApplicationTimelineItemDto(
                Id: t.Id,
                Type: "Task",
                Title: t.Title,
                Description: t.CompletedAt.HasValue ? $"Completed at {t.CompletedAt.Value:g}" : "Pending",
                Timestamp: t.CreatedAt));
        }

        return items.OrderByDescending(i => i.Timestamp).ToList();
    }

    private static ApplicationDetailDto MapToDetailDto(JobApplication a) =>
        new(
            Id: a.Id,
            CompanyId: a.CompanyId,
            CompanyName: a.Company?.Name ?? "Unknown Company",
            CompanyWebsite: a.Company?.Website,
            RoleTitle: a.RoleTitle,
            JobUrl: a.JobUrl,
            Source: a.Source,
            SourceDetail: a.SourceDetail,
            Status: a.Status,
            StatusChangedAt: a.StatusChangedAt,
            AppliedAt: a.AppliedAt,
            WorkMode: a.WorkMode,
            EmploymentType: a.EmploymentType,
            Location: a.Location,
            SalaryMin: a.SalaryMin,
            SalaryMax: a.SalaryMax,
            Currency: a.Currency,
            JobDescription: a.JobDescription,
            Notes: a.Notes,
            Pros: a.Pros,
            Cons: a.Cons,
            Priority: a.Priority,
            Favorite: a.Favorite,
            ExcitementRating: a.ExcitementRating,
            DaysInStage: Math.Max(0, (DateTime.UtcNow - a.StatusChangedAt).Days),
            DocumentVersionCvId: a.DocumentVersionCvId,
            DocumentVersionCvLabel: a.DocumentVersionCv?.VersionLabel,
            DocumentVersionCoverId: a.DocumentVersionCoverId,
            DocumentVersionCoverLabel: a.DocumentVersionCover?.VersionLabel,
            ClosedReason: a.ClosedReason,
            RejectionStage: a.RejectionStage,
            LessonsLearned: a.LessonsLearned,
            OfferSalary: a.OfferSalary,
            OfferBenefits: a.OfferBenefits,
            OfferDeadline: a.OfferDeadline,
            CreatedAt: a.CreatedAt,
            UpdatedAt: a.UpdatedAt);
}
