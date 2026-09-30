using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Features.Dashboard.DTOs;

namespace Pipeline.Application.Features.Dashboard.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetDashboardAsync(CancellationToken ct = default);
}
