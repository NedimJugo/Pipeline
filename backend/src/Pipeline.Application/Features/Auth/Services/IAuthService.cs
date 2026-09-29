using System;
using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Features.Auth.DTOs;

namespace Pipeline.Application.Features.Auth.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResult> RefreshTokenAsync(string rawRefreshToken, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(string rawRefreshToken, CancellationToken ct = default);
    Task<UserProfileDto> GetCurrentUserProfileAsync(Guid userId, CancellationToken ct = default);
}
