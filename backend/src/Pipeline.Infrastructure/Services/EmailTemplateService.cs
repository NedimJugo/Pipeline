using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Templates.DTOs;
using Pipeline.Application.Features.Templates.Services;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;

namespace Pipeline.Infrastructure.Services;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly PipelineDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public EmailTemplateService(PipelineDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<EmailTemplateDto>> GetTemplatesAsync(EmailTemplateCategory? category = null, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        await EnsureDefaultTemplatesSeededAsync(userId, ct);

        var query = _dbContext.EmailTemplates
            .Where(t => t.UserId == userId)
            .AsNoTracking();

        if (category.HasValue)
        {
            query = query.Where(t => t.Category == category.Value);
        }

        return await query
            .OrderByDescending(t => t.IsSystem)
            .ThenBy(t => t.Name)
            .Select(t => new EmailTemplateDto(
                t.Id,
                t.Name,
                t.Subject,
                t.Body,
                t.Category,
                t.IsSystem,
                t.CreatedAt
            ))
            .ToListAsync(ct);
    }

    public async Task<EmailTemplateDto?> GetTemplateByIdAsync(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var template = await _dbContext.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

        if (template == null) return null;

        return new EmailTemplateDto(
            template.Id,
            template.Name,
            template.Subject,
            template.Body,
            template.Category,
            template.IsSystem,
            template.CreatedAt);
    }

    public async Task<EmailTemplateDto> CreateTemplateAsync(CreateEmailTemplateRequest request, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var template = new EmailTemplate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Subject = request.Subject,
            Body = request.Body,
            Category = request.Category,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.EmailTemplates.Add(template);
        await _dbContext.SaveChangesAsync(ct);

        return new EmailTemplateDto(
            template.Id,
            template.Name,
            template.Subject,
            template.Body,
            template.Category,
            template.IsSystem,
            template.CreatedAt);
    }

    public async Task<EmailTemplateDto> UpdateTemplateAsync(Guid id, UpdateEmailTemplateRequest request, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var template = await _dbContext.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

        if (template == null)
        {
            throw new KeyNotFoundException($"Template with ID {id} not found.");
        }

        template.Name = request.Name;
        template.Subject = request.Subject;
        template.Body = request.Body;
        template.Category = request.Category;
        template.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        return new EmailTemplateDto(
            template.Id,
            template.Name,
            template.Subject,
            template.Body,
            template.Category,
            template.IsSystem,
            template.CreatedAt);
    }

    public async Task DeleteTemplateAsync(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var template = await _dbContext.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

        if (template != null)
        {
            _dbContext.EmailTemplates.Remove(template);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task<RenderedEmailTemplateDto> RenderTemplateAsync(Guid id, RenderEmailTemplateRequest request, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var template = await _dbContext.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

        if (template == null)
        {
            throw new KeyNotFoundException($"Template with ID {id} not found.");
        }

        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        var myName = user?.DisplayName ?? "Job Seeker";

        string companyName = "the company";
        string roleTitle = "the role";
        string contactName = "there";

        if (request.ApplicationId.HasValue)
        {
            var app = await _dbContext.Applications
                .Include(a => a.Company)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.ApplicationId.Value && a.UserId == userId, ct);

            if (app != null)
            {
                roleTitle = app.RoleTitle;
                if (app.Company != null && !string.IsNullOrWhiteSpace(app.Company.Name))
                {
                    companyName = app.Company.Name;
                }
            }
        }

        if (request.ContactId.HasValue)
        {
            var contact = await _dbContext.Contacts
                .Include(c => c.Company)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.ContactId.Value && c.UserId == userId, ct);

            if (contact != null)
            {
                contactName = contact.FullName;
                if (companyName == "the company" && contact.Company != null && !string.IsNullOrWhiteSpace(contact.Company.Name))
                {
                    companyName = contact.Company.Name;
                }
            }
        }

        var renderedSubject = ReplacePlaceholders(template.Subject, contactName, companyName, roleTitle, myName);
        var renderedBody = ReplacePlaceholders(template.Body, contactName, companyName, roleTitle, myName);

        return new RenderedEmailTemplateDto(renderedSubject, renderedBody);
    }

    public async Task EnsureDefaultTemplatesSeededAsync(Guid userId, CancellationToken ct = default)
    {
        var count = await _dbContext.EmailTemplates.CountAsync(t => t.UserId == userId, ct);
        if (count > 0) return;

        var defaults = GetDefaultTemplates(userId);
        _dbContext.EmailTemplates.AddRange(defaults);
        await _dbContext.SaveChangesAsync(ct);
    }

    private static string ReplacePlaceholders(string text, string contactName, string company, string role, string myName)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        return text
            .Replace("{{contactName}}", contactName, StringComparison.OrdinalIgnoreCase)
            .Replace("{{company}}", company, StringComparison.OrdinalIgnoreCase)
            .Replace("{{role}}", role, StringComparison.OrdinalIgnoreCase)
            .Replace("{{myName}}", myName, StringComparison.OrdinalIgnoreCase);
    }

    private static List<EmailTemplate> GetDefaultTemplates(Guid userId)
    {
        var now = DateTime.UtcNow;
        return new List<EmailTemplate>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Follow-up After Application",
                Category = EmailTemplateCategory.FollowUp,
                Subject = "Following up on my application for {{role}} at {{company}}",
                Body = "Hi {{contactName}},\n\nI recently submitted my application for the {{role}} position at {{company}} and wanted to reiterate my strong enthusiasm for the role. With my background in building and delivering impactful solutions, I would welcome the opportunity to discuss how I can contribute to {{company}}'s mission.\n\nThank you for your time and consideration!\n\nBest regards,\n{{myName}}",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Thank-You Note After Interview",
                Category = EmailTemplateCategory.ThankYou,
                Subject = "Thank you - {{role}} Interview with {{company}}",
                Body = "Hi {{contactName}},\n\nThank you so much for taking the time to speak with me today about the {{role}} opportunity at {{company}}. I really enjoyed our conversation and learning more about the team's upcoming initiatives.\n\nOur discussion reaffirmed my excitement about joining {{company}}. Please let me know if there are any additional details or materials I can provide.\n\nWarm regards,\n{{myName}}",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Post-Interview Status Check",
                Category = EmailTemplateCategory.FollowUp,
                Subject = "Checking in regarding {{role}} interview at {{company}}",
                Body = "Hi {{contactName}},\n\nI hope you are having a productive week! I am following up on our recent interview regarding the {{role}} role at {{company}}. I remain very interested in the position and was wondering if there are any updates regarding next steps in the process.\n\nLooking forward to hearing from you.\n\nBest,\n{{myName}}",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Networking Check-in",
                Category = EmailTemplateCategory.FollowUp,
                Subject = "Catching up / Quick hello",
                Body = "Hi {{contactName}},\n\nI hope all is well with you and the team at {{company}}! I wanted to reach out, say hello, and see how things are going on your end. I have been following recent developments at {{company}} with great interest.\n\nIf you have a quick 10-15 minutes in the coming weeks, I would love to catch up.\n\nBest regards,\n{{myName}}",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Offer Negotiation / Question",
                Category = EmailTemplateCategory.Negotiation,
                Subject = "{{role}} Offer - Discussion regarding terms at {{company}}",
                Body = "Hi {{contactName}},\n\nThank you very much for extending the offer for the {{role}} position at {{company}}. I am thrilled about the prospect of joining the team.\n\nBefore making a final decision, I would appreciate the opportunity to discuss a few details regarding the compensation package and start date. When would be a convenient time for a brief call?\n\nThank you again for this exciting opportunity.\n\nSincerely,\n{{myName}}",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "Withdrawing Candidacy",
                Category = EmailTemplateCategory.Withdraw,
                Subject = "Withdrawing my application - {{role}} at {{company}}",
                Body = "Hi {{contactName}},\n\nThank you very much for your time, consideration, and the opportunity to interview for the {{role}} role at {{company}}.\n\nI am writing to let you know that I have accepted another opportunity that closely aligns with my current career focus, so I would like to withdraw my candidacy for this position. I truly enjoyed connecting with your team and hope our paths cross again in the future.\n\nBest wishes,\n{{myName}}",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        };
    }
}
