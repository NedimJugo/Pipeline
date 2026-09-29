using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Documents.DTOs;
using Pipeline.Domain.Enums;

namespace Pipeline.Application.Features.Documents.Services;

public interface IDocumentService
{
    Task<List<DocumentDto>> GetDocumentsAsync(DocumentType? type = null, CancellationToken ct = default);
    Task<DocumentDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<DocumentDto> CreateDocumentAsync(CreateDocumentRequest request, Stream? initialFileStream = null, string? fileName = null, string? contentType = null, CancellationToken ct = default);
    Task<DocumentDto> UpdateDocumentAsync(Guid id, UpdateDocumentRequest request, CancellationToken ct = default);
    Task DeleteDocumentAsync(Guid id, CancellationToken ct = default);

    Task<DocumentVersionDto> UploadVersionAsync(Guid documentId, UploadVersionRequest request, Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task<DocumentVersionDto> SetDefaultVersionAsync(Guid versionId, CancellationToken ct = default);
    Task DeleteVersionAsync(Guid versionId, CancellationToken ct = default);

    Task<DocumentDownloadDto> GetDownloadUrlAsync(Guid versionId, TimeSpan? expiry = null, CancellationToken ct = default);
    Task<FileDownloadResult> DownloadVersionStreamAsync(Guid versionId, CancellationToken ct = default);
    Task<FileDownloadResult> DownloadStreamByKeyAsync(string fileKey, CancellationToken ct = default);

    Task<DocumentStatsSummaryDto> GetStatsSummaryAsync(CancellationToken ct = default);
    Task<List<DocumentVersionStatsDto>> GetVersionStatsAsync(DocumentType? type = null, CancellationToken ct = default);
}
