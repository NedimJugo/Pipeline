using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Features.Templates.DTOs;
using Pipeline.Application.Features.Templates.Services;
using Pipeline.Domain.Enums;

namespace Pipeline.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TemplatesController : ControllerBase
{
    private readonly IEmailTemplateService _templateService;

    public TemplatesController(IEmailTemplateService templateService)
    {
        _templateService = templateService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTemplates([FromQuery] EmailTemplateCategory? category, CancellationToken ct)
    {
        var templates = await _templateService.GetTemplatesAsync(category, ct);
        return Ok(templates);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var template = await _templateService.GetTemplateByIdAsync(id, ct);
        if (template == null) return NotFound();
        return Ok(template);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmailTemplateRequest request, CancellationToken ct)
    {
        var created = await _templateService.CreateTemplateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmailTemplateRequest request, CancellationToken ct)
    {
        var updated = await _templateService.UpdateTemplateAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _templateService.DeleteTemplateAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/render")]
    public async Task<IActionResult> Render(Guid id, [FromBody] RenderEmailTemplateRequest request, CancellationToken ct)
    {
        var rendered = await _templateService.RenderTemplateAsync(id, request, ct);
        return Ok(rendered);
    }
}
