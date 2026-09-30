using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pipeline.Domain.Entities;

namespace Pipeline.Application.Features.Discovery.Services;

public record RawJob(
    string ExternalId,
    string Title,
    string CompanyName,
    string? Location,
    string Url,
    string? Description,
    DateTimeOffset? PostedAt,
    string[] Tags
);

public interface IJobSourceConnector
{
    string SourceKey { get; }
    Task<IReadOnlyList<RawJob>> FetchAsync(JobSource source, CancellationToken ct);
}
