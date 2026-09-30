using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Features.Users.DTOs;

namespace Pipeline.Application.Features.Users.Services;

public interface IUserService
{
    Task<UserSettingsProfileDto> GetProfileAsync(CancellationToken ct = default);
    Task<UserSettingsProfileDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);
    Task<UserSettingsProfileDto> UpdatePreferencesAsync(UpdatePreferencesRequest request, CancellationToken ct = default);
    Task<GdprExportDto> ExportGdprDataAsync(CancellationToken ct = default);
    Task DeleteAccountAsync(CancellationToken ct = default);
    Task SeedDemoDataAsync(CancellationToken ct = default);
}
