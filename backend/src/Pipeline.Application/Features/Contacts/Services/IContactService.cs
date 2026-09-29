using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Features.Contacts.DTOs;

namespace Pipeline.Application.Features.Contacts.Services;

public interface IContactService
{
    Task<IReadOnlyList<ContactListItemDto>> GetContactsAsync(ContactFilterDto? filter = null, CancellationToken ct = default);
    Task<ContactDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ContactDetailDto> CreateContactAsync(CreateContactRequest request, CancellationToken ct = default);
    Task<ContactDetailDto> UpdateContactAsync(Guid id, UpdateContactRequest request, CancellationToken ct = default);
    Task DeleteContactAsync(Guid id, CancellationToken ct = default);
    Task LinkApplicationContactAsync(Guid contactId, LinkApplicationContactRequest request, CancellationToken ct = default);
    Task UnlinkApplicationContactAsync(Guid contactId, Guid applicationId, CancellationToken ct = default);
    Task<IReadOnlyList<LinkedApplicationDto>> GetApplicationsForContactAsync(Guid contactId, CancellationToken ct = default);
    Task<IReadOnlyList<ContactListItemDto>> GetContactsForApplicationAsync(Guid applicationId, CancellationToken ct = default);
}
