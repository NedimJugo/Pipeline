using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Interviews.DTOs;
using Pipeline.Application.Features.Interviews.Services;
using Pipeline.Application.Features.Interviews.Templates;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;

namespace Pipeline.Infrastructure.Services;

public class InterviewService : IInterviewService
{
    private readonly PipelineDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public InterviewService(PipelineDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<InterviewListItemDto>> GetInterviewsAsync(InterviewFilterDto? filter = null, CancellationToken ct = default)
    {
        var query = _db.Interviews
            .AsNoTracking()
            .Include(i => i.Application)
                .ThenInclude(a => a!.Company)
            .Include(i => i.InterviewContacts)
            .Include(i => i.Questions)
            .AsQueryable();

        if (filter != null)
        {
            if (filter.ApplicationId.HasValue)
            {
                query = query.Where(i => i.ApplicationId == filter.ApplicationId.Value);
            }
            if (filter.Status.HasValue)
            {
                query = query.Where(i => i.Status == filter.Status.Value);
            }
            if (filter.Type.HasValue)
            {
                query = query.Where(i => i.Type == filter.Type.Value);
            }
            if (filter.UpcomingOnly == true)
            {
                var now = DateTime.UtcNow;
                query = query.Where(i => i.ScheduledAt >= now && i.Status == InterviewStatus.Scheduled);
            }
        }

        var list = await query
            .OrderBy(i => i.ScheduledAt)
            .ToListAsync(ct);

        return list.Select(MapToListItem).ToList();
    }

    public async Task<IReadOnlyList<InterviewListItemDto>> GetInterviewsForApplicationAsync(Guid applicationId, CancellationToken ct = default)
    {
        var list = await _db.Interviews
            .AsNoTracking()
            .Include(i => i.Application)
                .ThenInclude(a => a!.Company)
            .Include(i => i.InterviewContacts)
            .Include(i => i.Questions)
            .Where(i => i.ApplicationId == applicationId)
            .OrderBy(i => i.ScheduledAt)
            .ToListAsync(ct);

        return list.Select(MapToListItem).ToList();
    }

    public async Task<InterviewDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var interview = await _db.Interviews
            .AsNoTracking()
            .Include(i => i.Application)
                .ThenInclude(a => a!.Company)
            .Include(i => i.InterviewContacts)
                .ThenInclude(ic => ic.Contact)
            .Include(i => i.Questions)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (interview == null)
        {
            throw new KeyNotFoundException($"Interview with ID '{id}' was not found.");
        }

        return MapToDetail(interview);
    }

    public async Task<InterviewDetailDto> CreateInterviewAsync(CreateInterviewRequest request, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var app = await _db.Applications
            .Include(a => a.Company)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);

        if (app == null)
        {
            throw new KeyNotFoundException($"Application with ID '{request.ApplicationId}' was not found.");
        }

        var checklistItems = request.CustomChecklist is { Count: > 0 }
            ? request.CustomChecklist
            : DefaultPrepChecklists.GetDefaultChecklist(request.Type);

        var interview = new Interview
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ApplicationId = request.ApplicationId,
            Application = app,
            Type = request.Type,
            Format = request.Format,
            ScheduledAt = request.ScheduledAt == default ? DateTime.UtcNow.AddDays(1) : request.ScheduledAt,
            DurationMinutes = request.DurationMinutes > 0 ? request.DurationMinutes : 45,
            Location = request.Location,
            MeetingLink = request.MeetingLink,
            Status = InterviewStatus.Scheduled,
            PrepNotes = request.PrepNotes,
            PrepChecklist = JsonSerializer.Serialize(checklistItems),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (request.InterviewerContactIds is { Count: > 0 })
        {
            var validContacts = await _db.Contacts
                .Where(c => request.InterviewerContactIds.Contains(c.Id))
                .ToListAsync(ct);

            foreach (var contact in validContacts)
            {
                interview.InterviewContacts.Add(new InterviewContact
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    InterviewId = interview.Id,
                    Interview = interview,
                    ContactId = contact.Id,
                    Contact = contact
                });
            }
        }

        _db.Interviews.Add(interview);
        await _db.SaveChangesAsync(ct);

        return MapToDetail(interview);
    }

    public async Task<InterviewDetailDto> UpdateInterviewAsync(Guid id, UpdateInterviewRequest request, CancellationToken ct = default)
    {
        var interview = await _db.Interviews
            .Include(i => i.Application)
                .ThenInclude(a => a!.Company)
            .Include(i => i.InterviewContacts)
                .ThenInclude(ic => ic.Contact)
            .Include(i => i.Questions)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (interview == null)
        {
            throw new KeyNotFoundException($"Interview with ID '{id}' was not found.");
        }

        interview.Type = request.Type;
        interview.Format = request.Format;
        interview.ScheduledAt = request.ScheduledAt;
        interview.DurationMinutes = request.DurationMinutes;
        interview.Location = request.Location;
        interview.MeetingLink = request.MeetingLink;
        interview.Status = request.Status;
        interview.PrepNotes = request.PrepNotes;
        interview.UpdatedAt = DateTime.UtcNow;

        if (request.InterviewerContactIds != null)
        {
            interview.InterviewContacts.Clear();
            var validContacts = await _db.Contacts
                .Where(c => request.InterviewerContactIds.Contains(c.Id))
                .ToListAsync(ct);

            foreach (var contact in validContacts)
            {
                interview.InterviewContacts.Add(new InterviewContact
                {
                    Id = Guid.NewGuid(),
                    UserId = _currentUserService.UserId ?? Guid.Empty,
                    InterviewId = interview.Id,
                    ContactId = contact.Id,
                    Contact = contact
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        return MapToDetail(interview);
    }

    public async Task<InterviewDetailDto> UpdateDebriefAsync(Guid id, UpdateDebriefRequest request, CancellationToken ct = default)
    {
        var interview = await _db.Interviews
            .Include(i => i.Application)
                .ThenInclude(a => a!.Company)
            .Include(i => i.InterviewContacts)
                .ThenInclude(ic => ic.Contact)
            .Include(i => i.Questions)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (interview == null)
        {
            throw new KeyNotFoundException($"Interview with ID '{id}' was not found.");
        }

        if (request.SelfRating.HasValue && (request.SelfRating.Value < 1 || request.SelfRating.Value > 5))
        {
            throw new ArgumentOutOfRangeException(nameof(request.SelfRating), "Self rating must be between 1 and 5.");
        }

        interview.SelfRating = request.SelfRating;
        interview.WentWell = request.WentWell;
        interview.ToImprove = request.ToImprove;
        interview.ThankYouSent = request.ThankYouSent;
        interview.OutcomeNotes = request.OutcomeNotes;
        interview.UpdatedAt = DateTime.UtcNow;

        // Auto mark as completed if it was in scheduled state
        if (interview.Status == InterviewStatus.Scheduled)
        {
            interview.Status = InterviewStatus.Completed;
        }

        await _db.SaveChangesAsync(ct);
        return MapToDetail(interview);
    }

    public async Task<InterviewDetailDto> UpdatePrepChecklistAsync(Guid id, UpdatePrepChecklistRequest request, CancellationToken ct = default)
    {
        var interview = await _db.Interviews
            .Include(i => i.Application)
                .ThenInclude(a => a!.Company)
            .Include(i => i.InterviewContacts)
                .ThenInclude(ic => ic.Contact)
            .Include(i => i.Questions)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (interview == null)
        {
            throw new KeyNotFoundException($"Interview with ID '{id}' was not found.");
        }

        interview.PrepChecklist = JsonSerializer.Serialize(request.Checklist ?? new List<PrepChecklistItemDto>());
        interview.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return MapToDetail(interview);
    }

    public async Task<InterviewQuestionDto> AddQuestionAsync(Guid id, AddQuestionRequest request, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var interview = await _db.Interviews.FirstOrDefaultAsync(i => i.Id == id, ct);

        if (interview == null)
        {
            throw new KeyNotFoundException($"Interview with ID '{id}' was not found.");
        }

        var question = new InterviewQuestion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InterviewId = id,
            Interview = interview,
            Question = request.Question.Trim(),
            MyAnswer = request.MyAnswer?.Trim(),
            Category = request.Category,
            Difficulty = Math.Clamp(request.Difficulty, 1, 5),
            WasPrepared = request.WasPrepared,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.InterviewQuestions.Add(question);
        interview.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new InterviewQuestionDto(
            question.Id,
            question.Question,
            question.MyAnswer,
            question.Category,
            question.Difficulty,
            question.WasPrepared);
    }

    public async Task DeleteQuestionAsync(Guid interviewId, Guid questionId, CancellationToken ct = default)
    {
        var question = await _db.InterviewQuestions
            .FirstOrDefaultAsync(q => q.Id == questionId && q.InterviewId == interviewId, ct);

        if (question != null)
        {
            _db.InterviewQuestions.Remove(question);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task DeleteInterviewAsync(Guid id, CancellationToken ct = default)
    {
        var interview = await _db.Interviews.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (interview != null)
        {
            _db.Interviews.Remove(interview);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<(string FileName, string Content)> GenerateIcsAsync(Guid id, CancellationToken ct = default)
    {
        var interview = await _db.Interviews
            .AsNoTracking()
            .Include(i => i.Application)
                .ThenInclude(a => a!.Company)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (interview == null)
        {
            throw new KeyNotFoundException($"Interview with ID '{id}' was not found.");
        }

        var startUtc = DateTime.SpecifyKind(interview.ScheduledAt, DateTimeKind.Utc);
        var endUtc = startUtc.AddMinutes(interview.DurationMinutes);
        var nowUtc = DateTime.UtcNow;

        var roleTitle = interview.Application?.RoleTitle ?? "Role";
        var companyName = interview.Application?.Company?.Name ?? "Company";
        var summary = $"{interview.Type} Interview: {roleTitle} at {companyName}";
        var location = interview.MeetingLink ?? interview.Location ?? "Video / Phone";
        
        var description = $"Format: {interview.Format}\\nType: {interview.Type}\\nDuration: {interview.DurationMinutes}m";
        if (!string.IsNullOrEmpty(interview.MeetingLink))
        {
            description += $"\\nMeeting Link: {interview.MeetingLink}";
        }
        if (!string.IsNullOrEmpty(interview.PrepNotes))
        {
            description += $"\\n\\nPrep Notes: {interview.PrepNotes.Replace("\r\n", "\\n").Replace("\n", "\\n")}";
        }

        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//Pipeline Job Search Command Center//EN");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");
        sb.AppendLine("BEGIN:VEVENT");
        sb.AppendLine($"UID:{interview.Id}@pipeline.local");
        sb.AppendLine($"DTSTAMP:{nowUtc:yyyyMMddTHHmmssZ}");
        sb.AppendLine($"DTSTART:{startUtc:yyyyMMddTHHmmssZ}");
        sb.AppendLine($"DTEND:{endUtc:yyyyMMddTHHmmssZ}");
        sb.AppendLine($"SUMMARY:{summary}");
        sb.AppendLine($"DESCRIPTION:{description}");
        sb.AppendLine($"LOCATION:{location}");
        sb.AppendLine("STATUS:CONFIRMED");
        sb.AppendLine("BEGIN:VALARM");
        sb.AppendLine("TRIGGER:-PT15M");
        sb.AppendLine("ACTION:DISPLAY");
        sb.AppendLine($"DESCRIPTION:Interview reminder: {roleTitle} at {companyName}");
        sb.AppendLine("END:VALARM");
        sb.AppendLine("END:VEVENT");
        sb.AppendLine("END:VCALENDAR");

        return ($"interview-{interview.Id}.ics", sb.ToString());
    }

    private static InterviewListItemDto MapToListItem(Interview i)
    {
        return new InterviewListItemDto(
            i.Id,
            i.ApplicationId,
            i.Application?.RoleTitle ?? "Unknown Role",
            i.Application?.Company?.Name ?? "Unknown Company",
            i.Type,
            i.Format,
            i.ScheduledAt,
            i.DurationMinutes,
            i.Status,
            i.Location,
            i.MeetingLink,
            i.InterviewContacts.Count,
            i.Questions.Count,
            i.SelfRating,
            i.CreatedAt);
    }

    private static InterviewDetailDto MapToDetail(Interview i)
    {
        List<PrepChecklistItemDto> checklist;
        try
        {
            checklist = string.IsNullOrWhiteSpace(i.PrepChecklist)
                ? new List<PrepChecklistItemDto>()
                : JsonSerializer.Deserialize<List<PrepChecklistItemDto>>(i.PrepChecklist) ?? new List<PrepChecklistItemDto>();
        }
        catch
        {
            checklist = new List<PrepChecklistItemDto>();
        }

        var interviewers = i.InterviewContacts
            .Where(ic => ic.Contact != null)
            .Select(ic =>
            {
                var c = ic.Contact!;
                var warmth = CalculateWarmth(c.LastContactedAt);
                return new InterviewContactDto(
                    c.Id,
                    c.FullName,
                    c.Role,
                    c.Email,
                    c.LinkedInUrl,
                    warmth);
            })
            .ToList();

        var questions = i.Questions
            .Select(q => new InterviewQuestionDto(
                q.Id,
                q.Question,
                q.MyAnswer,
                q.Category,
                q.Difficulty,
                q.WasPrepared))
            .ToList();

        return new InterviewDetailDto(
            i.Id,
            i.ApplicationId,
            i.Application?.RoleTitle ?? "Unknown Role",
            i.Application?.Company?.Name ?? "Unknown Company",
            i.Type,
            i.Format,
            i.ScheduledAt,
            i.DurationMinutes,
            i.Status,
            i.Location,
            i.MeetingLink,
            i.PrepNotes,
            checklist,
            i.SelfRating,
            i.WentWell,
            i.ToImprove,
            i.ThankYouSent,
            i.OutcomeNotes,
            interviewers,
            questions,
            i.CreatedAt,
            i.UpdatedAt);
    }

    private static ContactWarmth CalculateWarmth(DateTime? lastContactedAt)
    {
        if (!lastContactedAt.HasValue) return ContactWarmth.Cold;
        var days = (DateTime.UtcNow - lastContactedAt.Value).TotalDays;
        return days switch
        {
            < 14 => ContactWarmth.Hot,
            <= 30 => ContactWarmth.Warm,
            <= 60 => ContactWarmth.Cooling,
            _ => ContactWarmth.Cold
        };
    }
}
