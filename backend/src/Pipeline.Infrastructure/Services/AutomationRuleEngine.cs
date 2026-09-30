using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Features.Automation.Services;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;

namespace Pipeline.Infrastructure.Services;

public class AutomationRuleEngine : IAutomationRuleEngine
{
    private readonly PipelineDbContext _dbContext;

    public AutomationRuleEngine(PipelineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> EvaluateRulesForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return 0;

        var staleAfterDays = user.StaleAfterDays > 0 ? user.StaleAfterDays : 14;
        var now = DateTime.UtcNow;

        // Load existing auto rule keys to ensure idempotency
        var existingKeys = await _dbContext.Tasks
            .Where(t => t.UserId == userId && t.AutoRuleKey != null)
            .Select(t => t.AutoRuleKey!)
            .ToListAsync(ct);

        var existingKeySet = new HashSet<string>(existingKeys, StringComparer.OrdinalIgnoreCase);
        var tasksToCreate = new List<TaskItem>();

        var activeStatuses = new[]
        {
            ApplicationStatus.Wishlist,
            ApplicationStatus.Applied,
            ApplicationStatus.Screening,
            ApplicationStatus.Interview,
            ApplicationStatus.Assignment
        };

        // 1. follow_up_after_apply & 2. stale_application & 6. offer_deadline
        var applications = await _dbContext.Applications
            .Include(a => a.Company)
            .Where(a => a.UserId == userId)
            .ToListAsync(ct);

        foreach (var app in applications)
        {
            var companyName = app.Company?.Name ?? "the company";

            // Rule 1: follow_up_after_apply
            if (app.Status == ApplicationStatus.Applied)
            {
                var appliedTime = app.AppliedAt ?? app.StatusChangedAt;
                if ((now - appliedTime).TotalDays >= 7)
                {
                    var key = $"follow_up_after_apply:{app.Id}";
                    if (!existingKeySet.Contains(key))
                    {
                        tasksToCreate.Add(new TaskItem
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            ApplicationId = app.Id,
                            Title = $"Follow up on {app.RoleTitle} at {companyName}",
                            Notes = "Applied over 7 days ago with no status update.",
                            DueAt = now.Date.AddDays(1).AddHours(12),
                            Source = TaskSource.Auto,
                            AutoRuleKey = key,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                        existingKeySet.Add(key);
                    }
                }
            }

            // Rule 2: stale_application
            if (activeStatuses.Contains(app.Status))
            {
                if ((now - app.StatusChangedAt).TotalDays >= staleAfterDays)
                {
                    var key = $"stale_application:{app.Id}";
                    if (!existingKeySet.Contains(key))
                    {
                        tasksToCreate.Add(new TaskItem
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            ApplicationId = app.Id,
                            Title = $"No news from {companyName}, follow up or mark ghosted",
                            Notes = $"No activity for {Math.Round((now - app.StatusChangedAt).TotalDays)} days (threshold: {staleAfterDays} days).",
                            DueAt = now.Date.AddHours(17),
                            Source = TaskSource.Auto,
                            AutoRuleKey = key,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                        existingKeySet.Add(key);
                    }
                }
            }

            // Rule 6: offer_deadline
            if (app.Status == ApplicationStatus.Offer && app.OfferDeadline.HasValue)
            {
                var deadline = app.OfferDeadline.Value;
                var daysUntil = (deadline - now).TotalDays;
                if (daysUntil >= 0 && daysUntil <= 3)
                {
                    var key = $"offer_deadline:{app.Id}";
                    if (!existingKeySet.Contains(key))
                    {
                        tasksToCreate.Add(new TaskItem
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            ApplicationId = app.Id,
                            Title = $"Respond to offer from {companyName}",
                            Notes = $"Offer deadline is on {deadline:yyyy-MM-dd}.",
                            DueAt = deadline,
                            Source = TaskSource.Auto,
                            AutoRuleKey = key,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                        existingKeySet.Add(key);
                    }
                }
            }
        }

        // 3. thank_you, 4. post_interview_follow_up, 5. prep_reminder
        var interviews = await _dbContext.Interviews
            .Include(i => i.Application)
                .ThenInclude(a => a != null ? a.Company : null)
            .Where(i => i.UserId == userId)
            .ToListAsync(ct);

        foreach (var interview in interviews)
        {
            var companyName = interview.Application?.Company?.Name ?? "the company";

            // Rule 3: thank_you
            if (interview.Status == InterviewStatus.Completed && !interview.ThankYouSent)
            {
                var key = $"thank_you:{interview.Id}";
                if (!existingKeySet.Contains(key))
                {
                    tasksToCreate.Add(new TaskItem
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        ApplicationId = interview.ApplicationId,
                        InterviewId = interview.Id,
                        Title = "Send thank-you to interviewers",
                        Notes = $"Interview completed on {interview.ScheduledAt:yyyy-MM-dd HH:mm}.",
                        DueAt = interview.ScheduledAt.AddHours(24),
                        Source = TaskSource.Auto,
                        AutoRuleKey = key,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                    existingKeySet.Add(key);
                }
            }

            // Rule 4: post_interview_follow_up
            if (interview.Status == InterviewStatus.Completed && (now - interview.ScheduledAt).TotalDays >= 5)
            {
                if (interview.Application != null && activeStatuses.Contains(interview.Application.Status))
                {
                    var key = $"post_interview_follow_up:{interview.Id}";
                    if (!existingKeySet.Contains(key))
                    {
                        tasksToCreate.Add(new TaskItem
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            ApplicationId = interview.ApplicationId,
                            InterviewId = interview.Id,
                            Title = $"Ask {companyName} about next steps",
                            Notes = "5+ days since completed interview with no status update.",
                            DueAt = now.Date.AddDays(1).AddHours(12),
                            Source = TaskSource.Auto,
                            AutoRuleKey = key,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                        existingKeySet.Add(key);
                    }
                }
            }

            // Rule 5: prep_reminder
            if (interview.Status == InterviewStatus.Scheduled)
            {
                var hoursUntil = (interview.ScheduledAt - now).TotalHours;
                if (hoursUntil > 0 && hoursUntil <= 48)
                {
                    List<Pipeline.Application.Features.Interviews.DTOs.PrepChecklistItemDto> checklist;
                    try
                    {
                        checklist = string.IsNullOrWhiteSpace(interview.PrepChecklist)
                            ? new List<Pipeline.Application.Features.Interviews.DTOs.PrepChecklistItemDto>()
                            : System.Text.Json.JsonSerializer.Deserialize<List<Pipeline.Application.Features.Interviews.DTOs.PrepChecklistItemDto>>(
                                interview.PrepChecklist,
                                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                              ?? new List<Pipeline.Application.Features.Interviews.DTOs.PrepChecklistItemDto>();
                    }
                    catch
                    {
                        checklist = new List<Pipeline.Application.Features.Interviews.DTOs.PrepChecklistItemDto>();
                    }

                    bool isPrepIncomplete = checklist.Count == 0 || checklist.Any(c => !c.Done);

                    if (isPrepIncomplete)
                    {
                        var key = $"prep_reminder:{interview.Id}";
                        if (!existingKeySet.Contains(key))
                        {
                            tasksToCreate.Add(new TaskItem
                            {
                                Id = Guid.NewGuid(),
                                UserId = userId,
                                ApplicationId = interview.ApplicationId,
                                InterviewId = interview.Id,
                                Title = "Finish interview prep",
                                Notes = $"{interview.Type} interview scheduled for {interview.ScheduledAt:yyyy-MM-dd HH:mm}. Checklist is incomplete.",
                                DueAt = interview.ScheduledAt.AddHours(-2),
                                Source = TaskSource.Auto,
                                AutoRuleKey = key,
                                CreatedAt = now,
                                UpdatedAt = now
                            });
                            existingKeySet.Add(key);
                        }
                    }
                }
            }
        }

        // Rule 7: contact_follow_up
        var interactions = await _dbContext.Interactions
            .Include(i => i.Contact)
            .Where(i => i.UserId == userId && i.FollowUpRequired && i.FollowUpDueAt.HasValue)
            .ToListAsync(ct);

        foreach (var interaction in interactions)
        {
            var key = $"contact_follow_up:{interaction.Id}";
            if (!existingKeySet.Contains(key))
            {
                var contactName = interaction.Contact?.FullName ?? "contact";
                tasksToCreate.Add(new TaskItem
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ApplicationId = interaction.ApplicationId,
                    ContactId = interaction.ContactId,
                    Title = $"Follow up with {contactName}",
                    Notes = $"Logged interaction requested follow-up: {interaction.Summary}",
                    DueAt = interaction.FollowUpDueAt,
                    Source = TaskSource.Auto,
                    AutoRuleKey = key,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                existingKeySet.Add(key);
            }
        }

        // Rule 8: cold_contact
        var appContacts = await _dbContext.ApplicationContacts
            .Include(ac => ac.Application)
            .Include(ac => ac.Contact)
            .Where(ac => ac.Application != null && ac.Application.UserId == userId && activeStatuses.Contains(ac.Application.Status))
            .ToListAsync(ct);

        foreach (var ac in appContacts)
        {
            if (ac.Contact != null)
            {
                bool isCold = !ac.Contact.LastContactedAt.HasValue 
                    || (now - ac.Contact.LastContactedAt.Value).TotalDays >= 21;

                if (isCold)
                {
                    var key = $"cold_contact:{ac.ApplicationId}:{ac.ContactId}";
                    if (!existingKeySet.Contains(key))
                    {
                        tasksToCreate.Add(new TaskItem
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            ApplicationId = ac.ApplicationId,
                            ContactId = ac.ContactId,
                            Title = $"Reach out to {ac.Contact.FullName}",
                            Notes = $"Contact is linked to active application for {ac.Application?.RoleTitle}, but has not been contacted in 21+ days.",
                            DueAt = now.Date.AddDays(1).AddHours(12),
                            Source = TaskSource.Auto,
                            AutoRuleKey = key,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                        existingKeySet.Add(key);
                    }
                }
            }
        }

        if (tasksToCreate.Count > 0)
        {
            _dbContext.Tasks.AddRange(tasksToCreate);
            await _dbContext.SaveChangesAsync(ct);
        }

        return tasksToCreate.Count;
    }

    public async Task<int> EvaluateAllActiveUsersAsync(CancellationToken ct = default)
    {
        var users = await _dbContext.Users.AsNoTracking().Select(u => u.Id).ToListAsync(ct);
        int totalCreated = 0;
        foreach (var userId in users)
        {
            try
            {
                totalCreated += await EvaluateRulesForUserAsync(userId, ct);
            }
            catch
            {
                // Continue with next user
            }
        }
        return totalCreated;
    }
}
