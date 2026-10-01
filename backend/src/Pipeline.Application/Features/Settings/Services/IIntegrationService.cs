using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Features.Settings.DTOs;

namespace Pipeline.Application.Features.Settings.Services;

public interface IIntegrationService
{
    Task<IntegrationSettingsDto> GetSettingsAsync(CancellationToken ct = default);
    Task<IntegrationSettingsDto> UpdateSettingsAsync(UpdateIntegrationSettingsRequest request, CancellationToken ct = default);
    Task<TestEmailResultDto> SendTestEmailAsync(TestEmailRequest request, CancellationToken ct = default);
}
