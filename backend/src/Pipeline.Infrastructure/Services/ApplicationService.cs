using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Applications.DTOs;
using Pipeline.Application.Features.Applications.Services;
using Pipeline.Application.Features.Companies.Services;
using Pipeline.Application.Features.Users.DTOs;
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

        if (request.AppliedAt.HasValue && request.AppliedAt.Value > DateTime.UtcNow.AddDays(1))
        {
            throw new ArgumentException("Application date cannot be in the future.");
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
            DocumentVersionCvId = request.DocumentVersionCvId,
            DocumentVersionCoverId = request.DocumentVersionCoverId,
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

        if (request.AppliedAt.HasValue && request.AppliedAt.Value > DateTime.UtcNow.AddDays(1))
        {
            throw new ArgumentException("Application date cannot be in the future.");
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
        if (request.OfferBonus.HasValue) app.OfferBonus = request.OfferBonus;
        if (request.OfferBenefits != null) app.OfferBenefits = request.OfferBenefits;
        if (request.OfferDeadline.HasValue) app.OfferDeadline = request.OfferDeadline;
        if (request.OfferNegotiationNotes != null) app.OfferNegotiationNotes = request.OfferNegotiationNotes;
        if (request.ClosedReason != null) app.ClosedReason = request.ClosedReason;
        if (request.RejectionStage != null) app.RejectionStage = request.RejectionStage;
        if (request.LessonsLearned != null) app.LessonsLearned = request.LessonsLearned;
        if (request.DocumentVersionCvId.HasValue) app.DocumentVersionCvId = request.DocumentVersionCvId.Value == Guid.Empty ? null : request.DocumentVersionCvId.Value;
        if (request.DocumentVersionCoverId.HasValue) app.DocumentVersionCoverId = request.DocumentVersionCoverId.Value == Guid.Empty ? null : request.DocumentVersionCoverId.Value;

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
            OfferBonus: a.OfferBonus,
            OfferBenefits: a.OfferBenefits,
            OfferDeadline: a.OfferDeadline,
            OfferNegotiationNotes: a.OfferNegotiationNotes,
            CreatedAt: a.CreatedAt,
            UpdatedAt: a.UpdatedAt);

    public async Task<byte[]> ExportApplicationsCsvAsync(CancellationToken ct = default)
    {
        var apps = await _dbContext.Applications
            .Include(a => a.Company)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("CompanyName,RoleTitle,Status,Source,WorkMode,EmploymentType,Location,SalaryMin,SalaryMax,Currency,AppliedAt,ExcitementRating,Priority,JobUrl,Notes");

        foreach (var a in apps)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(a.Company?.Name ?? ""),
                EscapeCsv(a.RoleTitle),
                EscapeCsv(a.Status.ToString()),
                EscapeCsv(a.Source.ToString()),
                EscapeCsv(a.WorkMode.ToString()),
                EscapeCsv(a.EmploymentType.ToString()),
                EscapeCsv(a.Location ?? ""),
                a.SalaryMin?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
                a.SalaryMax?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
                EscapeCsv(a.Currency),
                a.AppliedAt?.ToString("yyyy-MM-dd") ?? "",
                a.ExcitementRating.ToString(),
                a.Priority.ToString(),
                EscapeCsv(a.JobUrl ?? ""),
                EscapeCsv(a.Notes ?? "")
            ));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<CsvImportResultDto> ImportApplicationsCsvAsync(Stream csvStream, CancellationToken ct = default)
    {
        using var reader = new StreamReader(csvStream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync(ct);
        var rows = ParseCsv(content);

        if (rows.Count == 0)
        {
            return new CsvImportResultDto(0, 0, 0, 0, new List<CsvImportError>());
        }

        var headerRow = rows[0];
        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headerRow.Count; i++)
        {
            var clean = headerRow[i].Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant();
            if (!headerMap.ContainsKey(clean))
            {
                headerMap[clean] = i;
            }
        }

        string GetCell(List<string> row, params string[] names)
        {
            foreach (var n in names)
            {
                var clean = n.Replace(" ", "").Replace("_", "").ToLowerInvariant();
                if (headerMap.TryGetValue(clean, out var idx) && idx < row.Count)
                {
                    return row[idx].Trim();
                }
            }
            return "";
        }

        var userId = _currentUserService.UserId ?? Guid.Empty;
        int totalProcessed = 0;
        int createdCount = 0;
        int updatedCount = 0;
        int failedCount = 0;
        var errors = new List<CsvImportError>();

        for (int r = 1; r < rows.Count; r++)
        {
            var row = rows[r];
            if (row.All(string.IsNullOrWhiteSpace)) continue;

            totalProcessed++;
            var companyName = GetCell(row, "companyname", "company");
            var roleTitle = GetCell(row, "roletitle", "role", "jobtitle", "title");

            if (string.IsNullOrWhiteSpace(roleTitle))
            {
                failedCount++;
                errors.Add(new CsvImportError(r, "RoleTitle", "RoleTitle is required."));
                continue;
            }
            if (string.IsNullOrWhiteSpace(companyName))
            {
                failedCount++;
                errors.Add(new CsvImportError(r, "CompanyName", "CompanyName is required."));
                continue;
            }

            try
            {
                var company = await _companyService.GetOrCreateCompanyAsync(companyName, null, ct);

                var statusStr = GetCell(row, "status");
                var status = Enum.TryParse<ApplicationStatus>(statusStr, true, out var parsedStatus)
                    ? parsedStatus : ApplicationStatus.Wishlist;

                var sourceStr = GetCell(row, "source");
                var source = Enum.TryParse<ApplicationSource>(sourceStr, true, out var parsedSource)
                    ? parsedSource : ApplicationSource.Other;

                var workModeStr = GetCell(row, "workmode");
                var workMode = Enum.TryParse<WorkMode>(workModeStr, true, out var parsedMode)
                    ? parsedMode : WorkMode.Remote;

                var empStr = GetCell(row, "employmenttype", "type");
                var employmentType = Enum.TryParse<EmploymentType>(empStr, true, out var parsedEmp)
                    ? parsedEmp : EmploymentType.FullTime;

                var location = GetCell(row, "location");
                var jobUrl = GetCell(row, "joburl", "url", "link");
                var notes = GetCell(row, "notes", "note", "description");

                decimal? salaryMin = decimal.TryParse(GetCell(row, "salarymin", "salary"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var sMin) ? sMin : null;
                decimal? salaryMax = decimal.TryParse(GetCell(row, "salarymax"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var sMax) ? sMax : null;
                var currency = GetCell(row, "currency");
                if (string.IsNullOrWhiteSpace(currency)) currency = "USD";

                DateTime? appliedAt = DateTime.TryParse(GetCell(row, "appliedat", "dateapplied"), out var appDate) ? DateTime.SpecifyKind(appDate, DateTimeKind.Utc) : null;
                int excitement = int.TryParse(GetCell(row, "excitementrating", "excitement"), out var exc) ? Math.Clamp(exc, 1, 5) : 3;
                int priority = int.TryParse(GetCell(row, "priority"), out var prio) ? Math.Clamp(prio, 1, 3) : 2;

                var existing = await _dbContext.Applications
                    .FirstOrDefaultAsync(a => a.CompanyId == company.Id && a.RoleTitle.ToLower() == roleTitle.ToLower(), ct);

                if (existing != null)
                {
                    existing.Status = status;
                    existing.Source = source;
                    existing.WorkMode = workMode;
                    existing.EmploymentType = employmentType;
                    if (!string.IsNullOrWhiteSpace(location)) existing.Location = location;
                    if (!string.IsNullOrWhiteSpace(jobUrl)) existing.JobUrl = jobUrl;
                    if (!string.IsNullOrWhiteSpace(notes)) existing.Notes = notes;
                    if (salaryMin.HasValue) existing.SalaryMin = salaryMin;
                    if (salaryMax.HasValue) existing.SalaryMax = salaryMax;
                    existing.Currency = currency;
                    if (appliedAt.HasValue) existing.AppliedAt = appliedAt;
                    existing.ExcitementRating = excitement;
                    existing.Priority = priority;
                    existing.UpdatedAt = DateTime.UtcNow;
                    updatedCount++;
                }
                else
                {
                    var now = DateTime.UtcNow;
                    var newApp = new JobApplication
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        CompanyId = company.Id,
                        RoleTitle = roleTitle,
                        Status = status,
                        StatusChangedAt = now,
                        Source = source,
                        WorkMode = workMode,
                        EmploymentType = employmentType,
                        Location = string.IsNullOrWhiteSpace(location) ? null : location,
                        JobUrl = string.IsNullOrWhiteSpace(jobUrl) ? null : jobUrl,
                        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
                        SalaryMin = salaryMin,
                        SalaryMax = salaryMax,
                        Currency = currency,
                        AppliedAt = appliedAt ?? (status != ApplicationStatus.Wishlist ? now : null),
                        ExcitementRating = excitement,
                        Priority = priority,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _dbContext.Applications.Add(newApp);

                    var history = new ApplicationStatusHistory
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = newApp.Id,
                        FromStatus = status,
                        ToStatus = status,
                        ChangedAt = now,
                        Note = "Imported from CSV"
                    };
                    _dbContext.ApplicationStatusHistories.Add(history);
                    createdCount++;
                }
            }
            catch (Exception ex)
            {
                failedCount++;
                errors.Add(new CsvImportError(r, "Row", ex.Message));
            }
        }

        await _dbContext.SaveChangesAsync(ct);
        return new CsvImportResultDto(totalProcessed, createdCount, updatedCount, failedCount, errors);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    private static List<List<string>> ParseCsv(string text)
    {
        var result = new List<List<string>>();
        if (string.IsNullOrEmpty(text)) return result;

        var currentRow = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;
        int i = 0;

        while (i < text.Length)
        {
            char c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    currentRow.Add(currentField.ToString());
                    currentField.Clear();
                }
                else if (c == '\r')
                {
                    if (i + 1 < text.Length && text[i + 1] == '\n') i++;
                    currentRow.Add(currentField.ToString());
                    currentField.Clear();
                    result.Add(currentRow);
                    currentRow = new List<string>();
                }
                else if (c == '\n')
                {
                    currentRow.Add(currentField.ToString());
                    currentField.Clear();
                    result.Add(currentRow);
                    currentRow = new List<string>();
                }
                else
                {
                    currentField.Append(c);
                }
            }
            i++;
        }

        if (currentField.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentField.ToString());
            result.Add(currentRow);
        }

        return result;
    }
}
