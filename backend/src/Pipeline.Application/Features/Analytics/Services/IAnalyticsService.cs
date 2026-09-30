using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Features.Analytics.DTOs;

namespace Pipeline.Application.Features.Analytics.Services;

public interface IAnalyticsService
{
    Task<AnalyticsOverviewDto> GetOverviewAsync(int? days = null, CancellationToken ct = default);
    Task<List<FunnelStageDto>> GetFunnelAsync(int? days = null, CancellationToken ct = default);
    Task<List<BreakdownMetricDto>> GetBySourceAsync(int? days = null, CancellationToken ct = default);
    Task<List<BreakdownMetricDto>> GetByDocumentAsync(int? days = null, CancellationToken ct = default);
    Task<List<BreakdownMetricDto>> GetByWorkModeAsync(int? days = null, CancellationToken ct = default);
    Task<List<StageDurationDto>> GetStageDurationsAsync(int? days = null, CancellationToken ct = default);
    Task<List<WeeklyVelocityDto>> GetWeeklyVelocityAsync(int? days = null, CancellationToken ct = default);
    Task<List<InsightCardDto>> GetInsightsAsync(int? days = null, CancellationToken ct = default);
}
