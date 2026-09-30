using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Features.Offers.DTOs;
using Pipeline.Application.Features.Offers.Services;

namespace Pipeline.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OffersController : ControllerBase
{
    private readonly IOfferService _offerService;

    public OffersController(IOfferService offerService)
    {
        _offerService = offerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOffers(CancellationToken ct)
    {
        var result = await _offerService.GetAvailableOffersAsync(ct);
        return Ok(result);
    }

    [HttpGet("compare")]
    public async Task<IActionResult> CompareOffers([FromQuery] List<Guid>? ids, CancellationToken ct)
    {
        var result = await _offerService.GetOfferComparisonAsync(ids, ct);
        return Ok(result);
    }

    [HttpPut("application/{applicationId:guid}")]
    public async Task<IActionResult> UpdateOfferDetails(Guid applicationId, [FromBody] UpdateOfferDetailsRequest request, CancellationToken ct)
    {
        var result = await _offerService.UpdateOfferDetailsAsync(applicationId, request, ct);
        return Ok(result);
    }
}
