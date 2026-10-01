using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Users.DTOs;
using Pipeline.Application.Features.Users.Services;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly PipelineDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<User> _userManager;

    public UserService(
        PipelineDbContext dbContext,
        ICurrentUserService currentUserService,
        UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public async Task<UserSettingsProfileDto> GetProfileAsync(CancellationToken ct = default)
    {
        var user = await GetCurrentUserAsync(ct);
        return MapToDto(user);
    }

    public async Task<UserSettingsProfileDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await GetCurrentUserAsync(ct);

        if (request.DisplayName != null) user.DisplayName = request.DisplayName;
        if (request.TargetRole != null) user.TargetRole = request.TargetRole;
        if (request.Seniority != null) user.Seniority = request.Seniority;
        if (request.Location != null) user.Location = request.Location;
        if (request.SalaryExpectationMin.HasValue) user.SalaryExpectationMin = request.SalaryExpectationMin;
        if (request.SalaryExpectationMax.HasValue) user.SalaryExpectationMax = request.SalaryExpectationMax;
        if (!string.IsNullOrWhiteSpace(request.Currency)) user.Currency = request.Currency;
        user.SearchStatus = request.SearchStatus;
        if (!string.IsNullOrWhiteSpace(request.Timezone)) user.Timezone = request.Timezone;
        if (request.OnboardingCompleted.HasValue) user.OnboardingCompleted = request.OnboardingCompleted.Value;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
        return MapToDto(user);
    }

    public async Task<UserSettingsProfileDto> CompleteOnboardingAsync(CancellationToken ct = default)
    {
        var user = await GetCurrentUserAsync(ct);
        user.OnboardingCompleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
        return MapToDto(user);
    }

    public async Task<UserSettingsProfileDto> UpdatePreferencesAsync(UpdatePreferencesRequest request, CancellationToken ct = default)
    {
        var user = await GetCurrentUserAsync(ct);

        if (request.StaleAfterDays.HasValue) user.StaleAfterDays = Math.Clamp(request.StaleAfterDays.Value, 1, 90);
        if (request.NotificationPrefs != null) user.NotificationPrefs = request.NotificationPrefs;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
        return MapToDto(user);
    }

    public async Task<GdprExportDto> ExportGdprDataAsync(CancellationToken ct = default)
    {
        var user = await GetCurrentUserAsync(ct);
        var userId = user.Id;

        var apps = await _dbContext.Applications
            .IgnoreQueryFilters()
            .Where(a => a.UserId == userId)
            .Include(a => a.StatusHistory)
            .Select(a => new
            {
                a.Id,
                a.CompanyId,
                a.RoleTitle,
                a.Status,
                a.Source,
                a.WorkMode,
                a.EmploymentType,
                a.Location,
                a.SalaryMin,
                a.SalaryMax,
                a.Currency,
                a.AppliedAt,
                a.ExcitementRating,
                a.Priority,
                a.JobUrl,
                a.JobDescription,
                a.Notes,
                a.Pros,
                a.Cons,
                a.OfferSalary,
                a.OfferBonus,
                a.OfferBenefits,
                a.OfferDeadline,
                a.OfferNegotiationNotes,
                StatusHistory = a.StatusHistory.Select(h => new
                {
                    h.Id,
                    h.FromStatus,
                    h.ToStatus,
                    h.ChangedAt,
                    h.Note
                }),
                a.CreatedAt,
                a.UpdatedAt
            })
            .ToListAsync(ct);

        var companyIds = apps.Select(a => a.CompanyId).Distinct().ToList();
        var companies = await _dbContext.Companies
            .Where(c => companyIds.Contains(c.Id))
            .ToListAsync(ct);

        var contacts = await _dbContext.Contacts
            .IgnoreQueryFilters()
            .Where(c => c.UserId == userId)
            .Include(c => c.Interactions)
            .ToListAsync(ct);

        var interviews = await _dbContext.Interviews
            .IgnoreQueryFilters()
            .Where(i => i.UserId == userId)
            .Include(i => i.Questions)
            .ToListAsync(ct);

        var tasks = await _dbContext.Tasks
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId)
            .ToListAsync(ct);

        var documents = await _dbContext.Documents
            .IgnoreQueryFilters()
            .Where(d => d.UserId == userId)
            .Include(d => d.Versions)
            .ToListAsync(ct);

        var references = await _dbContext.JobReferences
            .IgnoreQueryFilters()
            .Where(r => r.UserId == userId)
            .ToListAsync(ct);

        var templates = await _dbContext.EmailTemplates
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId)
            .ToListAsync(ct);

        return new GdprExportDto(
            Profile: MapToDto(user),
            Applications: apps,
            Companies: companies,
            Contacts: contacts,
            Interviews: interviews,
            Tasks: tasks,
            Documents: documents,
            References: references,
            Templates: templates,
            ExportedAtUtc: DateTime.UtcNow
        );
    }

    public async Task DeleteAccountAsync(CancellationToken ct = default)
    {
        var user = await GetCurrentUserAsync(ct);
        var userId = user.Id;

        // Hard cascade delete of all user-owned rows
        var tasks = await _dbContext.Tasks.IgnoreQueryFilters().Where(t => t.UserId == userId).ToListAsync(ct);
        _dbContext.Tasks.RemoveRange(tasks);

        var interactions = await _dbContext.Interactions.IgnoreQueryFilters().Where(i => i.UserId == userId).ToListAsync(ct);
        _dbContext.Interactions.RemoveRange(interactions);

        var contacts = await _dbContext.Contacts.IgnoreQueryFilters().Where(c => c.UserId == userId).ToListAsync(ct);
        _dbContext.Contacts.RemoveRange(contacts);

        var questions = await _dbContext.InterviewQuestions.IgnoreQueryFilters().Where(q => q.UserId == userId).ToListAsync(ct);
        _dbContext.InterviewQuestions.RemoveRange(questions);

        var interviews = await _dbContext.Interviews.IgnoreQueryFilters().Where(i => i.UserId == userId).ToListAsync(ct);
        _dbContext.Interviews.RemoveRange(interviews);

        var apps = await _dbContext.Applications.IgnoreQueryFilters().Where(a => a.UserId == userId).ToListAsync(ct);
        var appIds = apps.Select(a => a.Id).ToList();
        var histories = await _dbContext.ApplicationStatusHistories.Where(h => appIds.Contains(h.ApplicationId)).ToListAsync(ct);
        _dbContext.ApplicationStatusHistories.RemoveRange(histories);
        _dbContext.Applications.RemoveRange(apps);

        var docVersions = await _dbContext.DocumentVersions.IgnoreQueryFilters().Where(v => v.UserId == userId).ToListAsync(ct);
        _dbContext.DocumentVersions.RemoveRange(docVersions);

        var docs = await _dbContext.Documents.IgnoreQueryFilters().Where(d => d.UserId == userId).ToListAsync(ct);
        _dbContext.Documents.RemoveRange(docs);

        var appRefs = await _dbContext.ApplicationReferences.IgnoreQueryFilters().Where(ar => ar.UserId == userId).ToListAsync(ct);
        _dbContext.ApplicationReferences.RemoveRange(appRefs);

        var refs = await _dbContext.JobReferences.IgnoreQueryFilters().Where(r => r.UserId == userId).ToListAsync(ct);
        _dbContext.JobReferences.RemoveRange(refs);

        var tmpls = await _dbContext.EmailTemplates.IgnoreQueryFilters().Where(t => t.UserId == userId).ToListAsync(ct);
        _dbContext.EmailTemplates.RemoveRange(tmpls);

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task SeedDemoDataAsync(CancellationToken ct = default)
    {
        var user = await GetCurrentUserAsync(ct);
        var userId = user.Id;
        var now = DateTime.UtcNow;

        user.DisplayName = user.DisplayName ?? "Alex Mercer";
        user.TargetRole = "Staff Software Engineer";
        user.Seniority = "Staff";
        user.Location = "San Francisco, CA (Remote)";
        user.SalaryExpectationMin = 180000;
        user.SalaryExpectationMax = 220000;
        user.SearchStatus = SearchStatus.Active;
        user.OnboardingCompleted = true;
        user.UpdatedAt = now;

        // Create Default Templates if none exist
        if (!await _dbContext.EmailTemplates.AnyAsync(ct))
        {
            _dbContext.EmailTemplates.AddRange(new[]
            {
                new EmailTemplate
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Name = "Post-Interview Thank You",
                    Subject = "Thank you for the conversation - {role} at {company}",
                    Body = "Hi {contact},\n\nThank you for taking the time to speak with me today about the {role} opportunity at {company}. I thoroughly enjoyed learning about the technical challenges your team is solving.\n\nBest regards,\nAlex",
                    Category = EmailTemplateCategory.ThankYou,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new EmailTemplate
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Name = "Follow-up After Applied",
                    Subject = "Following up on my application for {role} at {company}",
                    Body = "Hi {contact},\n\nI hope you're having a productive week. I recently applied for the {role} position at {company} and wanted to briefly reiterate my strong enthusiasm for the role.\n\nBest regards,\nAlex",
                    Category = EmailTemplateCategory.FollowUp,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            });
        }

        // Create 2 Documents
        var doc1 = new Document
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Staff Distributed Systems Resume",
            Type = DocumentType.CV,
            Description = "Tailored for infrastructure & backend engineering",
            CreatedAt = now,
            UpdatedAt = now
        };
        var v1 = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DocumentId = doc1.Id,
            VersionLabel = "v1",
            FileName = "Alex_Mercer_Staff_Resume_v1.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024 * 142,
            FileKey = "demo/resume_v1.pdf",
            IsDefault = true,
            CreatedAt = now
        };
        doc1.Versions.Add(v1);
        _dbContext.Documents.Add(doc1);

        // Create Reference
        var ref1 = new JobReference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FullName = "David Lin",
            Company = "Stripe",
            Relationship = "Former Director of Engineering",
            Email = "david.lin@example.com",
            Phone = "+1 555 234 5678",
            Consent = ReferenceConsent.Agreed,
            Notes = "Can speak to distributed architecture and leadership",
            CreatedAt = now,
            UpdatedAt = now
        };
        _dbContext.JobReferences.Add(ref1);

        // Seed 10 Companies and Applications
        var seedApps = new (string Company, string Role, ApplicationStatus Status, ApplicationSource Source, WorkMode Mode, decimal? Min, decimal? Max, decimal? OfferSalary, decimal? OfferBonus, string? OfferBenefits, DateTime? OfferDeadline)[]
        {
            ("Stripe", "Staff Backend Engineer", ApplicationStatus.Offer, ApplicationSource.Referral, WorkMode.Remote, 190000, 220000, 210000, 35000, "Comprehensive healthcare, $4k learning stipend", now.AddDays(5)),
            ("Datadog", "Lead Infrastructure Engineer", ApplicationStatus.Offer, ApplicationSource.LinkedIn, WorkMode.Hybrid, 180000, 210000, 195000, 25000, "Standard tech benefits, 401k 50% match", now.AddDays(8)),
            ("Vercel", "Principal Frontend Architect", ApplicationStatus.Interview, ApplicationSource.CompanyWebsite, WorkMode.Remote, 195000, 230000, null, null, null, null),
            ("Figma", "Senior Systems Engineer", ApplicationStatus.Interview, ApplicationSource.Referral, WorkMode.Onsite, 175000, 200000, null, null, null, null),
            ("GitHub", "Senior Staff Engineer", ApplicationStatus.Accepted, ApplicationSource.LinkedIn, WorkMode.Remote, 200000, 240000, 225000, 45000, "Full remote setup, unlimited PTO", now.AddDays(-10)),
            ("Notion", "Product Infrastructure Engineer", ApplicationStatus.Screening, ApplicationSource.Referral, WorkMode.Hybrid, 170000, 195000, null, null, null, null),
            ("Linear", "Full-Stack Engineer", ApplicationStatus.Screening, ApplicationSource.LinkedIn, WorkMode.Remote, 165000, 190000, null, null, null, null),
            ("OpenAI", "Applied Research Engineer", ApplicationStatus.Rejected, ApplicationSource.Referral, WorkMode.Hybrid, 210000, 250000, null, null, null, null),
            ("Scale AI", "Platform Tech Lead", ApplicationStatus.Rejected, ApplicationSource.JobBoard, WorkMode.Onsite, 185000, 215000, null, null, null, null),
            ("Snowflake", "Senior Cloud Engineer", ApplicationStatus.Wishlist, ApplicationSource.LinkedIn, WorkMode.Remote, 175000, 205000, null, null, null, null),
        };

        var createdJobApps = new List<JobApplication>();
        foreach (var item in seedApps)
        {
            var company = await _dbContext.Companies.FirstOrDefaultAsync(c => c.Name.ToLower() == item.Company.ToLower(), ct);
            if (company == null)
            {
                company = new Company
                {
                    Id = Guid.NewGuid(),
                    Name = item.Company,
                };
                _dbContext.Companies.Add(company);
            }

            var app = new JobApplication
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CompanyId = company.Id,
                RoleTitle = item.Role,
                Status = item.Status,
                StatusChangedAt = now.AddDays(-Random.Shared.Next(1, 20)),
                Source = item.Source,
                WorkMode = item.Mode,
                EmploymentType = EmploymentType.FullTime,
                SalaryMin = item.Min,
                SalaryMax = item.Max,
                Currency = "USD",
                AppliedAt = item.Status != ApplicationStatus.Wishlist ? now.AddDays(-25) : null,
                OfferSalary = item.OfferSalary,
                OfferBonus = item.OfferBonus,
                OfferBenefits = item.OfferBenefits,
                OfferDeadline = item.OfferDeadline,
                ExcitementRating = 4,
                Priority = 3,
                CreatedAt = now.AddDays(-25),
                UpdatedAt = now
            };
            createdJobApps.Add(app);
            _dbContext.Applications.Add(app);

            _dbContext.ApplicationStatusHistories.Add(new ApplicationStatusHistory
            {
                Id = Guid.NewGuid(),
                ApplicationId = app.Id,
                FromStatus = app.Status,
                ToStatus = app.Status,
                ChangedAt = app.StatusChangedAt,
                Note = "Initial demo placement"
            });
        }

        // Seed Contacts
        var stripeApp = createdJobApps.First(a => a.RoleTitle.Contains("Staff Backend Engineer"));
        var contact1 = new Contact
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyId = stripeApp.CompanyId,
            FullName = "Elena Rostova",
            Role = "Engineering Director",
            Email = "elena.rostova@stripe.com",
            Type = ContactType.HiringManager,
            LastContactedAt = now.AddDays(-3),
            NextFollowUpAt = now.AddDays(2),
            CreatedAt = now,
            UpdatedAt = now
        };
        _dbContext.Contacts.Add(contact1);

        // Seed Interview
        var interview1 = new Interview
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ApplicationId = stripeApp.Id,
            Type = InterviewType.Technical,
            Format = InterviewFormat.Video,
            ScheduledAt = now.AddDays(1).Date.AddHours(15),
            DurationMinutes = 60,
            Location = "Google Meet",
            MeetingLink = "https://meet.google.com/abc-demo-xyz",
            PrepNotes = "Prepare system design for global transaction ledger",
            Status = InterviewStatus.Scheduled,
            CreatedAt = now,
            UpdatedAt = now
        };
        interview1.Questions.Add(new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InterviewId = interview1.Id,
            Question = "How do you achieve idempotency across distributed message consumers?",
            Category = InterviewQuestionCategory.Technical,
            Difficulty = 4,
            WasPrepared = true,
            CreatedAt = now
        });
        _dbContext.Interviews.Add(interview1);

        // Seed Tasks
        _dbContext.Tasks.Add(new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ApplicationId = stripeApp.Id,
            Title = "Review Stripe compensation breakdown and stock options",
            DueAt = now.AddDays(3),
            Source = TaskSource.Manual,
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task<User> GetCurrentUserAsync(CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID '{userId.Value}' was not found.");
        }

        return user;
    }

    private static UserSettingsProfileDto MapToDto(User u) => new(
        Id: u.Id,
        Email: u.Email ?? "",
        DisplayName: u.DisplayName,
        TargetRole: u.TargetRole,
        Seniority: u.Seniority,
        Location: u.Location,
        SalaryExpectationMin: u.SalaryExpectationMin,
        SalaryExpectationMax: u.SalaryExpectationMax,
        Currency: u.Currency,
        SearchStatus: u.SearchStatus,
        Timezone: u.Timezone,
        StaleAfterDays: u.StaleAfterDays,
        OnboardingCompleted: u.OnboardingCompleted,
        NotificationPrefs: u.NotificationPrefs,
        CreatedAt: u.CreatedAt
    );

    public async Task SeedNedimDataAsync(CancellationToken ct = default)
    {
        var user = await GetCurrentUserAsync(ct);
        await NedimDataSeeder.SeedAsync(_dbContext, _userManager, user.Id, ct);
    }
}
