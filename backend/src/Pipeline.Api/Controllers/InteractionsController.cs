using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Features.Interactions.DTOs;
using Pipeline.Application.Features.Interactions.Services;

namespace Pipeline.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InteractionsController : ControllerBase
{
    private readonly IInteractionService _interactionService;

    public InteractionsController(IInteractionService interactionService)
    {
        _interactionService = interactionService;
    }

    [HttpPost]
    public async Task<IActionResult> LogInteraction([FromBody] LogInteractionRequest request, CancellationToken ct)
    {
        var interaction = await _interactionService.LogInteractionAsync(request, ct);
        return Ok(interaction);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var interaction = await _interactionService.GetByIdAsync(id, ct);
        return Ok(interaction);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _interactionService.DeleteInteractionAsync(id, ct);
        return NoContent();
    }
}
