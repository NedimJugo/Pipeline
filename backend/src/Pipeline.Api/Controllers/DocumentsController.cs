using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Features.Documents.DTOs;
using Pipeline.Application.Features.Documents.Services;
using Pipeline.Domain.Enums;

namespace Pipeline.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDocuments([FromQuery] DocumentType? type, CancellationToken ct)
    {
        var docs = await _documentService.GetDocumentsAsync(type, ct);
        return Ok(docs);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var stats = await _documentService.GetStatsSummaryAsync(ct);
        return Ok(stats);
    }

    [HttpGet("version-stats")]
    public async Task<IActionResult> GetVersionStats([FromQuery] DocumentType? type, CancellationToken ct)
    {
        var stats = await _documentService.GetVersionStatsAsync(type, ct);
        return Ok(stats);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var doc = await _documentService.GetByIdAsync(id, ct);
        return Ok(doc);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDocumentRequest request, CancellationToken ct)
    {
        var created = await _documentService.CreateDocumentAsync(request, null, null, null, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("with-file")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateWithFile(
        [FromForm] string title,
        [FromForm] DocumentType type,
        [FromForm] string? description,
        [FromForm] string? versionLabel,
        [FromForm] string? notes,
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("A file must be uploaded.");
        }

        var request = new CreateDocumentRequest(title, type, description, versionLabel ?? "v1", notes);

        using var stream = file.OpenReadStream();
        var created = await _documentService.CreateDocumentAsync(request, stream, file.FileName, file.ContentType, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentRequest request, CancellationToken ct)
    {
        var updated = await _documentService.UpdateDocumentAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _documentService.DeleteDocumentAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/versions")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadVersion(
        Guid id,
        [FromForm] string versionLabel,
        [FromForm] string? notes,
        [FromForm] bool isDefault,
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("A file must be uploaded.");
        }

        var request = new UploadVersionRequest(versionLabel, notes, isDefault);

        using var stream = file.OpenReadStream();
        var version = await _documentService.UploadVersionAsync(id, request, stream, file.FileName, file.ContentType, ct);

        return Ok(version);
    }
}
