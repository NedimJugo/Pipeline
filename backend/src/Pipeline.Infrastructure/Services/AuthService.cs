using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Features.Auth.DTOs;
using Pipeline.Application.Features.Auth.Services;
using Pipeline.Domain.Entities;
using Pipeline.Infrastructure.Persistence;

namespace Pipeline.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly PipelineDbContext _dbContext;
    private readonly JwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        UserManager<User> userManager,
        PipelineDbContext dbContext,
        JwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("A user with this email address already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            UserName = request.Email.Trim().ToLowerInvariant(),
            DisplayName = request.DisplayName?.Trim() ?? request.Email.Split('@')[0],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Registration failed: {errors}");
        }

        return await GenerateAuthResultAsync(user, ct);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return await GenerateAuthResultAsync(user, ct);
    }

    public async Task<AuthResult> RefreshTokenAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            throw new UnauthorizedAccessException("Refresh token is required.");
        }

        var tokenHash = HashToken(rawRefreshToken);
        var existingToken = await _dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (existingToken == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (existingToken.IsRevoked)
        {
            // Possible token reuse detected! Revoke all tokens for this user for security.
            var allUserTokens = await _dbContext.RefreshTokens
                .IgnoreQueryFilters()
                .Where(t => t.UserId == existingToken.UserId && t.RevokedAt == null)
                .ToListAsync(ct);

            foreach (var token in allUserTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("Refresh token reuse detected. Access revoked.");
        }

        if (existingToken.IsExpired)
        {
            throw new UnauthorizedAccessException("Refresh token has expired.");
        }

        var user = await _userManager.FindByIdAsync(existingToken.UserId.ToString());
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        // Rotate token: revoke current and issue replacement
        var newRawRefreshToken = GenerateSecureToken();
        var newTokenHash = HashToken(newRawRefreshToken);
        var newExpiresAt = DateTime.UtcNow.AddDays(30);

        existingToken.RevokedAt = DateTime.UtcNow;
        existingToken.ReplacedByTokenHash = newTokenHash;

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newTokenHash,
            ExpiresAt = newExpiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync(ct);

        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user);

        return new AuthResult(
            AccessToken: newAccessToken,
            RefreshToken: newRawRefreshToken,
            RefreshTokenExpiresAt: newExpiresAt,
            User: MapToProfile(user));
    }

    public async Task RevokeRefreshTokenAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken)) return;

        var tokenHash = HashToken(rawRefreshToken);
        var token = await _dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (token != null && !token.IsRevoked)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task<UserProfileDto> GetCurrentUserProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        return MapToProfile(user);
    }

    private async Task<AuthResult> GenerateAuthResultAsync(User user, CancellationToken ct)
    {
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var rawRefreshToken = GenerateSecureToken();
        var tokenHash = HashToken(rawRefreshToken);
        var expiresAt = DateTime.UtcNow.AddDays(30);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(ct);

        return new AuthResult(
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            RefreshTokenExpiresAt: expiresAt,
            User: MapToProfile(user));
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static UserProfileDto MapToProfile(User user) =>
        new(
            Id: user.Id,
            Email: user.Email ?? string.Empty,
            DisplayName: user.DisplayName,
            TargetRole: user.TargetRole,
            OnboardingCompleted: user.OnboardingCompleted);
}
