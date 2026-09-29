using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Features.Interactions.DTOs;

namespace Pipeline.Application.Features.Interactions.Services;

public interface IInteractionService
{
    Task<InteractionDto> LogInteractionAsync(LogInteractionRequest request, CancellationToken ct = default);
    Task<InteractionDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<InteractionDto>> GetInteractionsForContactAsync(Guid contactId, CancellationToken ct = default);
    Task<IReadOnlyList<InteractionDto>> GetInteractionsForApplicationAsync(Guid applicationId, CancellationToken ct = default);
    Task DeleteInteractionAsync(Guid id, CancellationToken ct = default);
}
