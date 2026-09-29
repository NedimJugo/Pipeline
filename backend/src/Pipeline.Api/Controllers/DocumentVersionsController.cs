using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Features.Documents.Services;

namespace Pipeline.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/document-versions")]
public class DocumentVersionsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentVersionsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> GetDownloadUrl(Guid id, [FromQuery] int? expiryMinutes, CancellationToken ct)
    {
        var expiry = expiryMinutes.HasValue ? TimeSpan.FromMinutes(expiryMinutes.Value) : TimeSpan.FromMinutes(60);
        var download = await _documentService.GetDownloadUrlAsync(id, expiry, ct);
        return Ok(download);
    }

    [HttpGet("{id:guid}/stream")]
    public async Task<IActionResult> StreamVersion(Guid id, CancellationToken ct)
    {
        var downloadResult = await _documentService.DownloadVersionStreamAsync(id, ct);
        return File(downloadResult.Stream, downloadResult.ContentType, downloadResult.FileName, enableRangeProcessing: true);
    }

    [HttpGet("download-stream")]
    public async Task<IActionResult> StreamByKey([FromQuery] string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest("File key must be provided.");
        }

        var downloadResult = await _documentService.DownloadStreamByKeyAsync(key, ct);
        return File(downloadResult.Stream, downloadResult.ContentType, downloadResult.FileName, enableRangeProcessing: true);
    }

    [HttpPut("{id:guid}/default")]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken ct)
    {
        var updated = await _documentService.SetDefaultVersionAsync(id, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _documentService.DeleteVersionAsync(id, ct);
        return NoContent();
    }
}
