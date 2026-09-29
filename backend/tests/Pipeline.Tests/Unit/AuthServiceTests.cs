using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Auth.DTOs;
using Pipeline.Application.Features.Auth.Services;
using Pipeline.Application.Features.Auth.Validators;
using Pipeline.Domain.Entities;
using Pipeline.Infrastructure.Persistence;
using Pipeline.Infrastructure.Services;
using Xunit;

namespace Pipeline.Tests.Unit;

public class AuthServiceTests
{
    [Theory]
    [InlineData("short", false)] // < 10 chars
    [InlineData("ninechars", false)] // 9 chars
    [InlineData("tenchars!!", true)] // 10 chars
    [InlineData("ValidPassword123!", true)] // > 10 chars
    public void RegisterValidator_ValidatesPasswordMinimum10Characters(string password, bool expectedValid)
    {
        var validator = new RegisterRequestValidator();
        var result = validator.Validate(new RegisterRequest("user@example.com", password, "User"));

        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("invalid-email", false)]
    [InlineData("", false)]
    [InlineData("valid@pipeline.local", true)]
    public void RegisterValidator_ValidatesEmailFormat(string email, bool expectedValid)
    {
        var validator = new RegisterRequestValidator();
        var result = validator.Validate(new RegisterRequest(email, "TenCharacters123!", "User"));

        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesUserAndReturnsTokens()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        var dbContext = new PipelineDbContext(options, currentUserServiceMock.Object);

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:Secret"]).Returns("very-secure-jwt-secret-key-that-is-at-least-32-chars-long!");
        configMock.Setup(c => c["Jwt:Issuer"]).Returns("pipeline");
        configMock.Setup(c => c["Jwt:Audience"]).Returns("pipeline-web");

        var jwtGenerator = new JwtTokenGenerator(configMock.Object);

        var userStoreMock = new Mock<IUserStore<User>>();
        var userManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, new PasswordHasher<User>(), null!, null!, null!, null!, null!, null!);

        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        userManager.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var authService = new AuthService(userManager.Object, dbContext, jwtGenerator);

        // Act
        var result = await authService.RegisterAsync(new RegisterRequest("test@pipeline.local", "StrongPassword123!", "Test User"));

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.User.Email.Should().Be("test@pipeline.local");
        result.User.DisplayName.Should().Be("Test User");
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_RotatesAndInvalidatesPreviousToken()
    {
        // Arrange
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "refresh@pipeline.local",
            UserName = "refresh@pipeline.local"
        };

        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.UserId).Returns(testUser.Id);
        var dbContext = new PipelineDbContext(options, currentUserServiceMock.Object);

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:Secret"]).Returns("very-secure-jwt-secret-key-that-is-at-least-32-chars-long!");
        configMock.Setup(c => c["Jwt:Issuer"]).Returns("pipeline");
        configMock.Setup(c => c["Jwt:Audience"]).Returns("pipeline-web");

        var jwtGenerator = new JwtTokenGenerator(configMock.Object);

        var userStoreMock = new Mock<IUserStore<User>>();
        var userManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, new PasswordHasher<User>(), null!, null!, null!, null!, null!, null!);

        userManager.Setup(m => m.FindByIdAsync(testUser.Id.ToString()))
            .ReturnsAsync(testUser);

        var authService = new AuthService(userManager.Object, dbContext, jwtGenerator);

        // Seed an active refresh token
        var initialRawToken = Guid.NewGuid().ToString("N");
        var tokenHash = AuthService.HashToken(initialRawToken);

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = testUser.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        // Act - Rotate token
        var refreshResult = await authService.RefreshTokenAsync(initialRawToken);

        // Assert
        refreshResult.Should().NotBeNull();
        refreshResult.RefreshToken.Should().NotBe(initialRawToken);

        // Verify old token is marked revoked
        var oldTokenInDb = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        oldTokenInDb.Should().NotBeNull();
        oldTokenInDb!.IsRevoked.Should().BeTrue();
    }
}
