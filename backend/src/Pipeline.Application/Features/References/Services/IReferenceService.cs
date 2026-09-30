using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Features.References.DTOs;
using Pipeline.Domain.Enums;

namespace Pipeline.Application.Features.References.Services;

public interface IReferenceService
{
    Task<List<ReferenceListItemDto>> GetReferencesAsync(string? search = null, ReferenceConsent? consent = null, CancellationToken ct = default);
    Task<ReferenceDetailDto> GetReferenceByIdAsync(Guid id, CancellationToken ct = default);
    Task<ReferenceDetailDto> CreateReferenceAsync(CreateReferenceRequest request, CancellationToken ct = default);
    Task<ReferenceDetailDto> UpdateReferenceAsync(Guid id, UpdateReferenceRequest request, CancellationToken ct = default);
    Task DeleteReferenceAsync(Guid id, CancellationToken ct = default);
    Task<ShareReferenceResponse> ShareReferenceAsync(Guid referenceId, ShareReferenceRequest request, CancellationToken ct = default);
    Task RemoveSharedReferenceAsync(Guid referenceId, Guid applicationReferenceId, CancellationToken ct = default);
    Task RecordNotificationAsync(Guid referenceId, CancellationToken ct = default);
    Task<List<ApplicationReferenceDto>> GetApplicationReferencesAsync(Guid applicationId, CancellationToken ct = default);
}
