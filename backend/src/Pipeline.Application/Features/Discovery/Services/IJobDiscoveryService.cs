using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Features.Applications.DTOs;
using Pipeline.Application.Features.Discovery.DTOs;

namespace Pipeline.Application.Features.Discovery.Services;

public interface IJobDiscoveryService
{
    Task<DiscoveredJobListDto> GetDiscoveredJobsAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        string? search = null,
        Guid? sourceId = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<JobSourceDto>> GetJobSourcesAsync(CancellationToken ct = default);

    Task<ApplicationDetailDto> SaveJobToWishlistAsync(Guid discoveredJobId, CancellationToken ct = default);

    Task<bool> DismissJobAsync(Guid discoveredJobId, CancellationToken ct = default);

    Task<IngestJobsResultDto> IngestJobsAsync(CancellationToken ct = default);
}
