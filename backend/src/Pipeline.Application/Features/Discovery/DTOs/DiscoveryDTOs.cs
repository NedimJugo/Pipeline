using System;
using System.Collections.Generic;

namespace Pipeline.Application.Features.Discovery.DTOs;

public record DiscoveredJobDto(
    Guid Id,
    Guid? SourceId,
    string SourceName,
    string ExternalId,
    string Title,
    string CompanyName,
    string? Location,
    string Url,
    string? Description,
    DateTimeOffset? PostedAt,
    IReadOnlyList<string> Tags,
    string Status,
    DateTime FetchedAt
);

public record DiscoveredJobListDto(
    IReadOnlyList<DiscoveredJobDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record JobSourceDto(
    Guid Id,
    string Name,
    string Type,
    string? BaseUrl,
    bool Enabled,
    DateTime? LastRunAt
);

public record IngestJobsResultDto(
    int TotalFetched,
    int NewJobsStored,
    int DuplicatesSkipped
);
