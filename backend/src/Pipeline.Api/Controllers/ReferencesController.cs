using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Features.References.DTOs;
using Pipeline.Application.Features.References.Services;
using Pipeline.Domain.Enums;

namespace Pipeline.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReferencesController : ControllerBase
{
    private readonly IReferenceService _referenceService;

    public ReferencesController(IReferenceService referenceService)
    {
        _referenceService = referenceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetReferences(
        [FromQuery] string? search,
        [FromQuery] ReferenceConsent? consent,
        CancellationToken ct)
    {
        var result = await _referenceService.GetReferencesAsync(search, consent, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _referenceService.GetReferenceByIdAsync(id, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReferenceRequest request, CancellationToken ct)
    {
        var result = await _referenceService.CreateReferenceAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReferenceRequest request, CancellationToken ct)
    {
        var result = await _referenceService.UpdateReferenceAsync(id, request, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _referenceService.DeleteReferenceAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/share")]
    public async Task<IActionResult> Share(Guid id, [FromBody] ShareReferenceRequest request, CancellationToken ct)
    {
        var response = await _referenceService.ShareReferenceAsync(id, request, ct);
        return Ok(response);
    }

    [HttpDelete("{id:guid}/share/{appRefId:guid}")]
    public async Task<IActionResult> RemoveShared(Guid id, Guid appRefId, CancellationToken ct)
    {
        await _referenceService.RemoveSharedReferenceAsync(id, appRefId, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/notify")]
    public async Task<IActionResult> RecordNotification(Guid id, CancellationToken ct)
    {
        await _referenceService.RecordNotificationAsync(id, ct);
        return NoContent();
    }

    [HttpGet("application/{applicationId:guid}")]
    public async Task<IActionResult> GetApplicationReferences(Guid applicationId, CancellationToken ct)
    {
        var result = await _referenceService.GetApplicationReferencesAsync(applicationId, ct);
        return Ok(result);
    }
}
