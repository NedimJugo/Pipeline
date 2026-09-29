using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Companies.Services;
using Pipeline.Application.Features.Contacts.DTOs;
using Pipeline.Application.Features.Contacts.Services;
using Pipeline.Application.Features.Interactions.DTOs;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;

namespace Pipeline.Infrastructure.Services;

public class ContactService : IContactService
{
    private readonly PipelineDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICompanyService _companyService;

    public ContactService(
        PipelineDbContext dbContext,
        ICurrentUserService currentUserService,
        ICompanyService companyService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _companyService = companyService;
    }

    public static (ContactWarmth warmth, int? daysSince) CalculateWarmth(DateTime? lastContactedAt, DateTime now)
    {
        if (!lastContactedAt.HasValue)
        {
            return (ContactWarmth.Cold, null);
        }

        var days = Math.Max(0, (now.Date - lastContactedAt.Value.Date).Days);
        ContactWarmth warmth = days switch
        {
            < 14 => ContactWarmth.Hot,
            <= 30 => ContactWarmth.Warm,
            <= 60 => ContactWarmth.Cooling,
            _ => ContactWarmth.Cold
        };

        return (warmth, days);
    }

    public async Task<IReadOnlyList<ContactListItemDto>> GetContactsAsync(ContactFilterDto? filter = null, CancellationToken ct = default)
    {
        var query = _dbContext.Contacts
            .Include(c => c.Company)
            .Include(c => c.ApplicationContacts)
            .AsNoTracking();

        if (filter != null)
        {
            if (filter.CompanyId.HasValue)
            {
                query = query.Where(c => c.CompanyId == filter.CompanyId.Value);
            }
            if (filter.Type.HasValue)
            {
                query = query.Where(c => c.Type == filter.Type.Value);
            }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim().ToLower();
                query = query.Where(c => c.FullName.ToLower().Contains(s)
                    || (c.Role != null && c.Role.ToLower().Contains(s))
                    || (c.Email != null && c.Email.ToLower().Contains(s))
                    || (c.Company != null && c.Company.Name.ToLower().Contains(s)));
            }
        }

        var list = await query.OrderBy(c => c.FullName).ToListAsync(ct);
        var now = DateTime.UtcNow;

        var dtos = list.Select(c =>
        {
            var (warmth, daysSince) = CalculateWarmth(c.LastContactedAt, now);
            return new ContactListItemDto(
                Id: c.Id,
                FullName: c.FullName,
                Role: c.Role,
                CompanyId: c.CompanyId,
                CompanyName: c.Company?.Name,
                Email: c.Email,
                Phone: c.Phone,
                LinkedInUrl: c.LinkedInUrl,
                Type: c.Type,
                Warmth: warmth,
                DaysSinceLastContact: daysSince,
                LastContactedAt: c.LastContactedAt,
                NextFollowUpAt: c.NextFollowUpAt,
                LinkedApplicationsCount: c.ApplicationContacts.Count,
                CreatedAt: c.CreatedAt);
        });

        if (filter?.Warmth.HasValue == true)
        {
            dtos = dtos.Where(c => c.Warmth == filter.Warmth.Value);
        }

        return dtos.ToList();
    }

    public async Task<ContactDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var contact = await _dbContext.Contacts
            .Include(c => c.Company)
            .Include(c => c.ApplicationContacts)
                .ThenInclude(ac => ac.Application)
                    .ThenInclude(a => a!.Company)
            .Include(c => c.Interactions.OrderByDescending(i => i.OccurredAt).Take(10))
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (contact == null)
        {
            throw new KeyNotFoundException($"Contact with ID '{id}' was not found.");
        }

        return MapToDetailDto(contact);
    }

    public async Task<ContactDetailDto> CreateContactAsync(CreateContactRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new ArgumentException("Contact full name is required.", nameof(request.FullName));
        }

        var userId = _currentUserService.UserId ?? Guid.Empty;

        Guid? companyId = request.CompanyId;
        if (!companyId.HasValue && !string.IsNullOrWhiteSpace(request.CompanyName))
        {
            var company = await _companyService.GetOrCreateCompanyAsync(request.CompanyName, null, ct);
            companyId = company.Id;
        }

        var contact = new Contact
        {
            UserId = userId,
            CompanyId = companyId,
            FullName = request.FullName.Trim(),
            Role = request.Role?.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            LinkedInUrl = request.LinkedInUrl?.Trim(),
            Type = request.Type,
            Notes = request.Notes?.Trim()
        };

        _dbContext.Contacts.Add(contact);
        await _dbContext.SaveChangesAsync(ct);

        if (request.ApplicationId.HasValue && request.ApplicationId.Value != Guid.Empty)
        {
            var appContact = new ApplicationContact
            {
                UserId = userId,
                ContactId = contact.Id,
                ApplicationId = request.ApplicationId.Value,
                RoleInProcess = request.RoleInProcess?.Trim()
            };
            _dbContext.ApplicationContacts.Add(appContact);
            await _dbContext.SaveChangesAsync(ct);
        }

        return await GetByIdAsync(contact.Id, ct);
    }

    public async Task<ContactDetailDto> UpdateContactAsync(Guid id, UpdateContactRequest request, CancellationToken ct = default)
    {
        var contact = await _dbContext.Contacts
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (contact == null)
        {
            throw new KeyNotFoundException($"Contact with ID '{id}' was not found.");
        }

        Guid? companyId = request.CompanyId;
        if (!companyId.HasValue && !string.IsNullOrWhiteSpace(request.CompanyName))
        {
            var company = await _companyService.GetOrCreateCompanyAsync(request.CompanyName, null, ct);
            companyId = company.Id;
        }

        contact.FullName = request.FullName.Trim();
        contact.CompanyId = companyId;
        contact.Role = request.Role?.Trim();
        contact.Email = request.Email?.Trim();
        contact.Phone = request.Phone?.Trim();
        contact.LinkedInUrl = request.LinkedInUrl?.Trim();
        contact.Type = request.Type;
        contact.Notes = request.Notes?.Trim();
        contact.NextFollowUpAt = request.NextFollowUpAt;
        contact.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        return await GetByIdAsync(contact.Id, ct);
    }

    public async Task DeleteContactAsync(Guid id, CancellationToken ct = default)
    {
        var contact = await _dbContext.Contacts.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (contact == null)
        {
            throw new KeyNotFoundException($"Contact with ID '{id}' was not found.");
        }

        contact.DeletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task LinkApplicationContactAsync(Guid contactId, LinkApplicationContactRequest request, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var contact = await _dbContext.Contacts.FirstOrDefaultAsync(c => c.Id == contactId, ct);
        if (contact == null)
        {
            throw new KeyNotFoundException($"Contact with ID '{contactId}' was not found.");
        }

        var app = await _dbContext.Applications.FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);
        if (app == null)
        {
            throw new KeyNotFoundException($"Application with ID '{request.ApplicationId}' was not found.");
        }

        var existing = await _dbContext.ApplicationContacts
            .FirstOrDefaultAsync(ac => ac.ContactId == contactId && ac.ApplicationId == request.ApplicationId, ct);

        if (existing != null)
        {
            existing.RoleInProcess = request.RoleInProcess?.Trim();
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _dbContext.ApplicationContacts.Add(new ApplicationContact
            {
                UserId = userId,
                ContactId = contactId,
                ApplicationId = request.ApplicationId,
                RoleInProcess = request.RoleInProcess?.Trim()
            });
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UnlinkApplicationContactAsync(Guid contactId, Guid applicationId, CancellationToken ct = default)
    {
        var link = await _dbContext.ApplicationContacts
            .FirstOrDefaultAsync(ac => ac.ContactId == contactId && ac.ApplicationId == applicationId, ct);

        if (link != null)
        {
            _dbContext.ApplicationContacts.Remove(link);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<LinkedApplicationDto>> GetApplicationsForContactAsync(Guid contactId, CancellationToken ct = default)
    {
        var links = await _dbContext.ApplicationContacts
            .Include(ac => ac.Application)
                .ThenInclude(a => a!.Company)
            .Where(ac => ac.ContactId == contactId)
            .AsNoTracking()
            .ToListAsync(ct);

        return links.Select(ac => new LinkedApplicationDto(
            ApplicationId: ac.ApplicationId,
            RoleTitle: ac.Application?.RoleTitle ?? "Unknown Role",
            CompanyName: ac.Application?.Company?.Name ?? "Unknown Company",
            Status: ac.Application?.Status ?? ApplicationStatus.Wishlist,
            RoleInProcess: ac.RoleInProcess)).ToList();
    }

    public async Task<IReadOnlyList<ContactListItemDto>> GetContactsForApplicationAsync(Guid applicationId, CancellationToken ct = default)
    {
        var links = await _dbContext.ApplicationContacts
            .Include(ac => ac.Contact)
                .ThenInclude(c => c!.Company)
            .Where(ac => ac.ApplicationId == applicationId)
            .AsNoTracking()
            .ToListAsync(ct);

        var now = DateTime.UtcNow;

        return links.Where(ac => ac.Contact != null).Select(ac =>
        {
            var c = ac.Contact!;
            var (warmth, daysSince) = CalculateWarmth(c.LastContactedAt, now);
            return new ContactListItemDto(
                Id: c.Id,
                FullName: c.FullName,
                Role: ac.RoleInProcess ?? c.Role,
                CompanyId: c.CompanyId,
                CompanyName: c.Company?.Name,
                Email: c.Email,
                Phone: c.Phone,
                LinkedInUrl: c.LinkedInUrl,
                Type: c.Type,
                Warmth: warmth,
                DaysSinceLastContact: daysSince,
                LastContactedAt: c.LastContactedAt,
                NextFollowUpAt: c.NextFollowUpAt,
                LinkedApplicationsCount: 1,
                CreatedAt: c.CreatedAt);
        }).ToList();
    }

    private ContactDetailDto MapToDetailDto(Contact contact)
    {
        var now = DateTime.UtcNow;
        var (warmth, daysSince) = CalculateWarmth(contact.LastContactedAt, now);

        var linkedApps = contact.ApplicationContacts.Select(ac => new LinkedApplicationDto(
            ApplicationId: ac.ApplicationId,
            RoleTitle: ac.Application?.RoleTitle ?? "Unknown Role",
            CompanyName: ac.Application?.Company?.Name ?? "Unknown Company",
            Status: ac.Application?.Status ?? ApplicationStatus.Wishlist,
            RoleInProcess: ac.RoleInProcess)).ToList();

        var recentInteractions = contact.Interactions.Select(i => new InteractionDto(
            Id: i.Id,
            ContactId: i.ContactId,
            ContactName: contact.FullName,
            ApplicationId: i.ApplicationId,
            ApplicationRole: null,
            CompanyName: null,
            Channel: i.Channel,
            Direction: i.Direction,
            OccurredAt: i.OccurredAt,
            Summary: i.Summary,
            SentContent: i.SentContent,
            AttachmentDocumentId: i.AttachmentDocumentId,
            FollowUpRequired: i.FollowUpRequired,
            FollowUpDueAt: i.FollowUpDueAt,
            CreatedAt: i.CreatedAt)).ToList();

        return new ContactDetailDto(
            Id: contact.Id,
            FullName: contact.FullName,
            Role: contact.Role,
            CompanyId: contact.CompanyId,
            CompanyName: contact.Company?.Name,
            Email: contact.Email,
            Phone: contact.Phone,
            LinkedInUrl: contact.LinkedInUrl,
            Type: contact.Type,
            Warmth: warmth,
            DaysSinceLastContact: daysSince,
            LastContactedAt: contact.LastContactedAt,
            NextFollowUpAt: contact.NextFollowUpAt,
            Notes: contact.Notes,
            LinkedApplications: linkedApps,
            RecentInteractions: recentInteractions,
            CreatedAt: contact.CreatedAt,
            UpdatedAt: contact.UpdatedAt);
    }
}
