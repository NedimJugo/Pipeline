using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Features.Offers.DTOs;

namespace Pipeline.Application.Features.Offers.Services;

public interface IOfferService
{
    Task<OfferComparisonItemDto> UpdateOfferDetailsAsync(Guid applicationId, UpdateOfferDetailsRequest request, CancellationToken ct = default);
    Task<OfferComparisonViewDto> GetOfferComparisonAsync(List<Guid>? applicationIds = null, CancellationToken ct = default);
    Task<List<OfferComparisonItemDto>> GetAvailableOffersAsync(CancellationToken ct = default);
}
