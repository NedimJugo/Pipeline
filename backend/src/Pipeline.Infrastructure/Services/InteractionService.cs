using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Interactions.DTOs;
using Pipeline.Application.Features.Interactions.Services;
using Pipeline.Domain.Entities;
using Pipeline.Infrastructure.Persistence;

namespace Pipeline.Infrastructure.Services;

public class InteractionService : IInteractionService
{
    private readonly PipelineDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public InteractionService(
        PipelineDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<InteractionDto> LogInteractionAsync(LogInteractionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Summary))
        {
            throw new ArgumentException("Interaction summary is required.", nameof(request.Summary));
        }

        var userId = _currentUserService.UserId ?? Guid.Empty;
        var occurredAt = request.OccurredAt ?? DateTime.UtcNow;

        Contact? contact = null;
        if (request.ContactId.HasValue && request.ContactId.Value != Guid.Empty)
        {
            contact = await _dbContext.Contacts.FirstOrDefaultAsync(c => c.Id == request.ContactId.Value, ct);
            if (contact == null)
            {
                throw new KeyNotFoundException($"Contact with ID '{request.ContactId.Value}' was not found.");
            }
        }

        if (request.ApplicationId.HasValue && request.ApplicationId.Value != Guid.Empty)
        {
            var appExists = await _dbContext.Applications.AnyAsync(a => a.Id == request.ApplicationId.Value, ct);
            if (!appExists)
            {
                throw new KeyNotFoundException($"Application with ID '{request.ApplicationId.Value}' was not found.");
            }
        }

        var interaction = new Interaction
        {
            UserId = userId,
            ContactId = request.ContactId,
            ApplicationId = request.ApplicationId,
            Channel = request.Channel,
            Direction = request.Direction,
            OccurredAt = occurredAt,
            Summary = request.Summary.Trim(),
            SentContent = request.SentContent?.Trim(),
            AttachmentDocumentId = request.AttachmentDocumentId,
            FollowUpRequired = request.FollowUpRequired,
            FollowUpDueAt = request.FollowUpDueAt
        };

        _dbContext.Interactions.Add(interaction);

        // Update contact last contacted & follow-up if applicable
        if (contact != null)
        {
            if (!contact.LastContactedAt.HasValue || occurredAt >= contact.LastContactedAt.Value)
            {
                contact.LastContactedAt = occurredAt;
            }

            if (request.FollowUpRequired && request.FollowUpDueAt.HasValue)
            {
                contact.NextFollowUpAt = request.FollowUpDueAt.Value;
            }

            contact.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);

        return await GetByIdAsync(interaction.Id, ct);
    }

    public async Task<InteractionDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var interaction = await _dbContext.Interactions
            .Include(i => i.Contact)
            .Include(i => i.Application)
                .ThenInclude(a => a!.Company)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (interaction == null)
        {
            throw new KeyNotFoundException($"Interaction with ID '{id}' was not found.");
        }

        return MapToDto(interaction);
    }

    public async Task<IReadOnlyList<InteractionDto>> GetInteractionsForContactAsync(Guid contactId, CancellationToken ct = default)
    {
        var list = await _dbContext.Interactions
            .Include(i => i.Contact)
            .Include(i => i.Application)
                .ThenInclude(a => a!.Company)
            .Where(i => i.ContactId == contactId)
            .OrderByDescending(i => i.OccurredAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return list.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<InteractionDto>> GetInteractionsForApplicationAsync(Guid applicationId, CancellationToken ct = default)
    {
        var list = await _dbContext.Interactions
            .Include(i => i.Contact)
            .Include(i => i.Application)
                .ThenInclude(a => a!.Company)
            .Where(i => i.ApplicationId == applicationId)
            .OrderByDescending(i => i.OccurredAt)
            .AsNoTracking()
            .ToListAsync(ct);

        return list.Select(MapToDto).ToList();
    }

    public async Task DeleteInteractionAsync(Guid id, CancellationToken ct = default)
    {
        var interaction = await _dbContext.Interactions.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (interaction == null)
        {
            throw new KeyNotFoundException($"Interaction with ID '{id}' was not found.");
        }

        _dbContext.Interactions.Remove(interaction);
        await _dbContext.SaveChangesAsync(ct);
    }

    private static InteractionDto MapToDto(Interaction i)
    {
        return new InteractionDto(
            Id: i.Id,
            ContactId: i.ContactId,
            ContactName: i.Contact?.FullName,
            ApplicationId: i.ApplicationId,
            ApplicationRole: i.Application?.RoleTitle,
            CompanyName: i.Application?.Company?.Name,
            Channel: i.Channel,
            Direction: i.Direction,
            OccurredAt: i.OccurredAt,
            Summary: i.Summary,
            SentContent: i.SentContent,
            AttachmentDocumentId: i.AttachmentDocumentId,
            FollowUpRequired: i.FollowUpRequired,
            FollowUpDueAt: i.FollowUpDueAt,
            CreatedAt: i.CreatedAt);
    }
}
