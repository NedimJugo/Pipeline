using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Auth.DTOs;
using Pipeline.Application.Features.Auth.Services;
using Pipeline.Application.Features.Auth.Validators;

namespace Pipeline.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;
    private const string RefreshTokenCookieName = "pipeline_refresh_token";

    public AuthController(IAuthService authService, ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var validator = new RegisterRequestValidator();
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var result = await _authService.RegisterAsync(request, ct);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);

        return Ok(new
        {
            accessToken = result.AccessToken,
            user = result.User
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);

        return Ok(new
        {
            accessToken = result.AccessToken,
            user = result.User
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? bodyRequest, CancellationToken ct)
    {
        var rawToken = Request.Cookies[RefreshTokenCookieName] ?? bodyRequest?.RefreshToken;
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "No refresh token provided.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var result = await _authService.RefreshTokenAsync(rawToken, ct);
        SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);

        return Ok(new
        {
            accessToken = result.AccessToken,
            user = result.User
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var rawToken = Request.Cookies[RefreshTokenCookieName];
        if (!string.IsNullOrWhiteSpace(rawToken))
        {
            await _authService.RevokeRefreshTokenAsync(rawToken, ct);
        }

        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        });

        return Ok(new { message = "Logged out successfully." });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        if (_currentUserService.UserId == null)
        {
            return Unauthorized();
        }

        var user = await _authService.GetCurrentUserProfileAsync(_currentUserService.UserId.Value, ct);
        return Ok(user);
    }

    private void SetRefreshTokenCookie(string token, DateTime expiresAt)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = expiresAt
        };

        Response.Cookies.Append(RefreshTokenCookieName, token, cookieOptions);
    }
}
