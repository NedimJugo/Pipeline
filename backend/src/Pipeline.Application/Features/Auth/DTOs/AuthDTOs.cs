using System;

namespace Pipeline.Application.Features.Auth.DTOs;

public record RegisterRequest(
    string Email,
    string Password,
    string? DisplayName = null);

public record LoginRequest(
    string Email,
    string Password);

public record RefreshTokenRequest(
    string? RefreshToken = null);

public record UserProfileDto(
    Guid Id,
    string Email,
    string? DisplayName,
    string? TargetRole,
    bool OnboardingCompleted);

public record AuthResult(
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserProfileDto User);
