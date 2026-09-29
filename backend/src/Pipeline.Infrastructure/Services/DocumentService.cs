using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Documents.DTOs;
using Pipeline.Application.Features.Documents.Services;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;

namespace Pipeline.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly PipelineDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;

    public DocumentService(
        PipelineDbContext dbContext,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
    }

    public async Task<List<DocumentDto>> GetDocumentsAsync(DocumentType? type = null, CancellationToken ct = default)
    {
        var query = _dbContext.Documents
            .Include(d => d.Versions)
            .AsNoTracking();

        if (type.HasValue)
        {
            query = query.Where(d => d.Type == type.Value);
        }

        var docs = await query
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync(ct);

        var statsList = await GetVersionStatsAsync(type, ct);
        var statsDict = statsList.ToDictionary(s => s.VersionId);

        return docs.Select(d => MapToDto(d, statsDict)).ToList();
    }

    public async Task<DocumentDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _dbContext.Documents
            .Include(d => d.Versions)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (doc == null)
        {
            throw new KeyNotFoundException($"Document with ID '{id}' was not found.");
        }

        var statsList = await GetVersionStatsAsync(doc.Type, ct);
        var statsDict = statsList.ToDictionary(s => s.VersionId);

        return MapToDto(doc, statsDict);
    }

    public async Task<DocumentDto> CreateDocumentAsync(
        CreateDocumentRequest request,
        Stream? initialFileStream = null,
        string? fileName = null,
        string? contentType = null,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;

        var document = new Document
        {
            Id = Guid.NewGuid(),
            UserId = currentUserId,
            Title = request.Title.Trim(),
            Type = request.Type,
            Description = request.Description?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        if (initialFileStream != null && !string.IsNullOrWhiteSpace(fileName))
        {
            var fileKey = await _fileStorage.UploadAsync(
                initialFileStream,
                fileName,
                contentType ?? "application/pdf",
                ct);

            var version = new DocumentVersion
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                DocumentId = document.Id,
                VersionLabel = string.IsNullOrWhiteSpace(request.VersionLabel) ? "v1" : request.VersionLabel.Trim(),
                FileKey = fileKey,
                FileName = fileName,
                ContentType = contentType ?? "application/pdf",
                SizeBytes = initialFileStream.CanSeek ? initialFileStream.Length : 0,
                Notes = request.Notes?.Trim(),
                IsDefault = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            document.Versions.Add(version);
            _dbContext.DocumentVersions.Add(version);
        }

        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync(ct);

        return await GetByIdAsync(document.Id, ct);
    }

    public async Task<DocumentDto> UpdateDocumentAsync(Guid id, UpdateDocumentRequest request, CancellationToken ct = default)
    {
        var doc = await _dbContext.Documents
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (doc == null)
        {
            throw new KeyNotFoundException($"Document with ID '{id}' was not found.");
        }

        doc.Title = request.Title.Trim();
        if (request.Type.HasValue) doc.Type = request.Type.Value;
        if (request.Description != null) doc.Description = request.Description.Trim();
        doc.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        return await GetByIdAsync(doc.Id, ct);
    }

    public async Task DeleteDocumentAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _dbContext.Documents
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (doc == null)
        {
            throw new KeyNotFoundException($"Document with ID '{id}' was not found.");
        }

        doc.DeletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<DocumentVersionDto> UploadVersionAsync(
        Guid documentId,
        UploadVersionRequest request,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        var doc = await _dbContext.Documents
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);

        if (doc == null)
        {
            throw new KeyNotFoundException($"Document with ID '{documentId}' was not found.");
        }

        var now = DateTime.UtcNow;
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;

        var fileKey = await _fileStorage.UploadAsync(fileStream, fileName, contentType, ct);

        bool isDefault = request.IsDefault || !doc.Versions.Any();
        if (isDefault)
        {
            foreach (var existing in doc.Versions)
            {
                existing.IsDefault = false;
            }
        }

        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            UserId = currentUserId,
            DocumentId = doc.Id,
            VersionLabel = request.VersionLabel.Trim(),
            FileKey = fileKey,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = fileStream.CanSeek ? fileStream.Length : 0,
            Notes = request.Notes?.Trim(),
            IsDefault = isDefault,
            CreatedAt = now,
            UpdatedAt = now
        };

        doc.Versions.Add(version);
        _dbContext.DocumentVersions.Add(version);
        doc.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(ct);

        return new DocumentVersionDto(
            version.Id,
            version.DocumentId,
            version.VersionLabel,
            version.FileName,
            version.ContentType,
            version.SizeBytes,
            version.Notes,
            version.IsDefault,
            version.CreatedAt);
    }

    public async Task<DocumentVersionDto> SetDefaultVersionAsync(Guid versionId, CancellationToken ct = default)
    {
        var version = await _dbContext.DocumentVersions
            .Include(v => v.Document)
            .ThenInclude(d => d!.Versions)
            .FirstOrDefaultAsync(v => v.Id == versionId, ct);

        if (version == null || version.Document == null)
        {
            throw new KeyNotFoundException($"Document version with ID '{versionId}' was not found.");
        }

        foreach (var v in version.Document.Versions)
        {
            v.IsDefault = (v.Id == versionId);
        }

        version.Document.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        return new DocumentVersionDto(
            version.Id,
            version.DocumentId,
            version.VersionLabel,
            version.FileName,
            version.ContentType,
            version.SizeBytes,
            version.Notes,
            version.IsDefault,
            version.CreatedAt);
    }

    public async Task DeleteVersionAsync(Guid versionId, CancellationToken ct = default)
    {
        var version = await _dbContext.DocumentVersions
            .Include(v => v.Document)
            .ThenInclude(d => d!.Versions)
            .FirstOrDefaultAsync(v => v.Id == versionId, ct);

        if (version == null)
        {
            throw new KeyNotFoundException($"Document version with ID '{versionId}' was not found.");
        }

        // Unlink applications pointing to this version
        var apps = await _dbContext.Applications
            .Where(a => a.DocumentVersionCvId == versionId || a.DocumentVersionCoverId == versionId)
            .ToListAsync(ct);

        foreach (var app in apps)
        {
            if (app.DocumentVersionCvId == versionId) app.DocumentVersionCvId = null;
            if (app.DocumentVersionCoverId == versionId) app.DocumentVersionCoverId = null;
        }

        // Delete from physical storage
        await _fileStorage.DeleteAsync(version.FileKey, ct);

        var doc = version.Document;
        _dbContext.DocumentVersions.Remove(version);

        // If this version was default, promote newest remaining version
        if (version.IsDefault && doc != null)
        {
            var remaining = doc.Versions.Where(v => v.Id != versionId).OrderByDescending(v => v.CreatedAt).FirstOrDefault();
            if (remaining != null)
            {
                remaining.IsDefault = true;
            }
        }

        if (doc != null)
        {
            doc.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<DocumentDownloadDto> GetDownloadUrlAsync(Guid versionId, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var version = await _dbContext.DocumentVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == versionId, ct);

        if (version == null)
        {
            throw new KeyNotFoundException($"Document version with ID '{versionId}' was not found.");
        }

        var url = await _fileStorage.GetPresignedDownloadUrlAsync(
            version.FileKey,
            version.FileName,
            expiry ?? TimeSpan.FromMinutes(60),
            ct);

        return new DocumentDownloadDto(url, version.FileName, version.ContentType, version.SizeBytes);
    }

    public async Task<FileDownloadResult> DownloadVersionStreamAsync(Guid versionId, CancellationToken ct = default)
    {
        var version = await _dbContext.DocumentVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == versionId, ct);

        if (version == null)
        {
            throw new KeyNotFoundException($"Document version with ID '{versionId}' was not found.");
        }

        return await _fileStorage.DownloadAsync(version.FileKey, ct);
    }

    public Task<FileDownloadResult> DownloadStreamByKeyAsync(string fileKey, CancellationToken ct = default)
    {
        return _fileStorage.DownloadAsync(fileKey, ct);
    }

    public async Task<DocumentStatsSummaryDto> GetStatsSummaryAsync(CancellationToken ct = default)
    {
        var allStats = await GetVersionStatsAsync(null, ct);
        var totalDocs = await _dbContext.Documents.CountAsync(ct);
        var totalVersions = await _dbContext.DocumentVersions.CountAsync(ct);

        return new DocumentStatsSummaryDto(
            TotalDocuments: totalDocs,
            TotalVersions: totalVersions,
            CvVersionStats: allStats.Where(s => s.DocumentType == DocumentType.CV).OrderByDescending(s => s.InterviewRate).ToList(),
            CoverLetterStats: allStats.Where(s => s.DocumentType == DocumentType.CoverLetter).OrderByDescending(s => s.InterviewRate).ToList());
    }

    public async Task<List<DocumentVersionStatsDto>> GetVersionStatsAsync(DocumentType? type = null, CancellationToken ct = default)
    {
        var versionsQuery = _dbContext.DocumentVersions
            .Include(v => v.Document)
            .AsNoTracking();

        if (type.HasValue)
        {
            versionsQuery = versionsQuery.Where(v => v.Document != null && v.Document.Type == type.Value);
        }

        var versions = await versionsQuery.ToListAsync(ct);

        var apps = await _dbContext.Applications
            .Include(a => a.Interviews)
            .Include(a => a.Interactions)
            .AsNoTracking()
            .ToListAsync(ct);

        var results = new List<DocumentVersionStatsDto>();

        foreach (var v in versions)
        {
            var linkedApps = apps.Where(a => a.DocumentVersionCvId == v.Id || a.DocumentVersionCoverId == v.Id).ToList();
            int sentCount = linkedApps.Count;
            int replyCount = linkedApps.Count(a =>
                a.Status == ApplicationStatus.Screening ||
                a.Status == ApplicationStatus.Interview ||
                a.Status == ApplicationStatus.Assignment ||
                a.Status == ApplicationStatus.Offer ||
                a.Status == ApplicationStatus.Accepted ||
                a.Interactions.Any() ||
                a.Interviews.Any());

            int interviewCount = linkedApps.Count(a =>
                a.Status == ApplicationStatus.Interview ||
                a.Status == ApplicationStatus.Assignment ||
                a.Status == ApplicationStatus.Offer ||
                a.Status == ApplicationStatus.Accepted ||
                a.Interviews.Any());

            int offerCount = linkedApps.Count(a =>
                a.Status == ApplicationStatus.Offer ||
                a.Status == ApplicationStatus.Accepted ||
                a.OfferSalary.HasValue);

            double responseRate = sentCount > 0 ? Math.Round((double)replyCount / sentCount * 100.0, 1) : 0.0;
            double interviewRate = sentCount > 0 ? Math.Round((double)interviewCount / sentCount * 100.0, 1) : 0.0;
            double offerRate = sentCount > 0 ? Math.Round((double)offerCount / sentCount * 100.0, 1) : 0.0;

            results.Add(new DocumentVersionStatsDto(
                VersionId: v.Id,
                VersionLabel: v.VersionLabel,
                DocumentTitle: v.Document?.Title ?? "Untitled Document",
                DocumentType: v.Document?.Type ?? DocumentType.CV,
                SentCount: sentCount,
                ReplyCount: replyCount,
                InterviewCount: interviewCount,
                OfferCount: offerCount,
                ResponseRate: responseRate,
                InterviewRate: interviewRate,
                OfferRate: offerRate));
        }

        return results.OrderByDescending(r => r.SentCount).ThenByDescending(r => r.InterviewRate).ToList();
    }

    private static DocumentDto MapToDto(Document d, Dictionary<Guid, DocumentVersionStatsDto> statsDict)
    {
        var versions = d.Versions
            .OrderByDescending(v => v.IsDefault)
            .ThenByDescending(v => v.CreatedAt)
            .Select(v => new DocumentVersionDto(
                Id: v.Id,
                DocumentId: v.DocumentId,
                VersionLabel: v.VersionLabel,
                FileName: v.FileName,
                ContentType: v.ContentType,
                SizeBytes: v.SizeBytes,
                Notes: v.Notes,
                IsDefault: v.IsDefault,
                CreatedAt: v.CreatedAt,
                Stats: statsDict.TryGetValue(v.Id, out var s) ? s : null))
            .ToList();

        var defaultVersion = versions.FirstOrDefault(v => v.IsDefault);

        return new DocumentDto(
            Id: d.Id,
            Type: d.Type,
            Title: d.Title,
            Description: d.Description,
            DefaultVersionId: defaultVersion?.Id,
            DefaultVersionLabel: defaultVersion?.VersionLabel,
            VersionCount: versions.Count,
            CreatedAt: d.CreatedAt,
            UpdatedAt: d.UpdatedAt,
            Versions: versions);
    }
}
