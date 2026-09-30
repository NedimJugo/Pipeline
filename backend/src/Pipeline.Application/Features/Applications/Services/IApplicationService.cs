using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Features.Applications.DTOs;

namespace Pipeline.Application.Features.Applications.Services;

public interface IApplicationService
{
    Task<IReadOnlyList<ApplicationListItemDto>> GetApplicationsAsync(ApplicationFilterDto? filter = null, CancellationToken ct = default);
    Task<ApplicationDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApplicationDetailDto> CreateApplicationAsync(CreateApplicationRequest request, CancellationToken ct = default);
    Task<ApplicationDetailDto> UpdateApplicationAsync(Guid id, UpdateApplicationRequest request, CancellationToken ct = default);
    Task<ApplicationDetailDto> UpdateStatusAsync(Guid id, UpdateStatusRequest request, CancellationToken ct = default);
    Task DeleteApplicationAsync(Guid id, CancellationToken ct = default);
    Task<ApplicationDetailDto> DuplicateApplicationAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationTimelineItemDto>> GetTimelineAsync(Guid id, CancellationToken ct = default);
    Task<byte[]> ExportApplicationsCsvAsync(CancellationToken ct = default);
    Task<Pipeline.Application.Features.Users.DTOs.CsvImportResultDto> ImportApplicationsCsvAsync(System.IO.Stream csvStream, CancellationToken ct = default);
}
