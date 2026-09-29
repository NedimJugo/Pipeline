using System;
using System.Collections.Generic;
using Pipeline.Domain.Enums;

namespace Pipeline.Application.Features.Documents.DTOs;

public record DocumentVersionStatsDto(
    Guid VersionId,
    string VersionLabel,
    string DocumentTitle,
    DocumentType DocumentType,
    int SentCount,
    int ReplyCount,
    int InterviewCount,
    int OfferCount,
    double ResponseRate,
    double InterviewRate,
    double OfferRate);

public record DocumentVersionDto(
    Guid Id,
    Guid DocumentId,
    string VersionLabel,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? Notes,
    bool IsDefault,
    DateTime CreatedAt,
    DocumentVersionStatsDto? Stats = null);

public record DocumentDto(
    Guid Id,
    DocumentType Type,
    string Title,
    string? Description,
    Guid? DefaultVersionId,
    string? DefaultVersionLabel,
    int VersionCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<DocumentVersionDto> Versions);

public record CreateDocumentRequest(
    string Title,
    DocumentType Type = DocumentType.CV,
    string? Description = null,
    string? VersionLabel = "v1",
    string? Notes = null);

public record UpdateDocumentRequest(
    string Title,
    DocumentType? Type = null,
    string? Description = null);

public record UploadVersionRequest(
    string VersionLabel,
    string? Notes = null,
    bool IsDefault = false);

public record DocumentStatsSummaryDto(
    int TotalDocuments,
    int TotalVersions,
    List<DocumentVersionStatsDto> CvVersionStats,
    List<DocumentVersionStatsDto> CoverLetterStats);

public record DocumentDownloadDto(
    string Url,
    string FileName,
    string ContentType,
    long SizeBytes);
