using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Settings.DTOs;
using Pipeline.Application.Features.Settings.Services;
using Pipeline.Infrastructure.Persistence;
using Pipeline.Infrastructure.Services;
using Xunit;

namespace Pipeline.Tests.Unit;

public class IntegrationServiceTests
{
    private (PipelineDbContext dbContext, Mock<ICurrentUserService> currentUserMock, IConfiguration configuration, Guid userId) CreateTestContext()
    {
        var userId = Guid.NewGuid();
        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(s => s.UserId).Returns(userId);
        currentUserMock.Setup(s => s.IsAuthenticated).Returns(true);

        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new PipelineDbContext(options, currentUserMock.Object);

        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Smtp:Host", "mailpit.local"},
            {"Smtp:Port", "1025"},
            {"Smtp:From", "system@pipeline.local"},
            {"Google:ClientId", "google-env-client-id"},
            {"Storage:Provider", "Local"}
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        return (dbContext, currentUserMock, configuration, userId);
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsConfigurationFallback_WhenNoCustomSettingsExist()
    {
        // Arrange
        var (dbContext, currentUserMock, configuration, _) = CreateTestContext();
        var service = new IntegrationService(dbContext, currentUserMock.Object, configuration);

        // Act
        var result = await service.GetSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.UseCustomSmtp.Should().BeFalse();
        result.IsEnvFallbackSmtp.Should().BeTrue();
        result.SmtpHost.Should().Be("mailpit.local");
        result.SmtpPort.Should().Be(1025);
        result.SmtpFrom.Should().Be("system@pipeline.local");
        result.HasSmtpPassword.Should().BeFalse();
        result.StorageProvider.Should().Be("Local");
    }

    [Fact]
    public async Task UpdateSettingsAsync_PersistsCustomValues_AndMasksPassword()
    {
        // Arrange
        var (dbContext, currentUserMock, configuration, _) = CreateTestContext();
        var service = new IntegrationService(dbContext, currentUserMock.Object, configuration);

        var updateRequest = new UpdateIntegrationSettingsRequest(
            UseCustomSmtp: true,
            SmtpHost: "smtp.gmail.com",
            SmtpPort: 587,
            SmtpUser: "user@gmail.com",
            SmtpPassword: "secret-app-password",
            SmtpFrom: "user@gmail.com",
            UseCustomGoogle: true,
            GoogleClientId: "my-custom-google-id",
            GoogleClientSecret: "my-custom-google-secret",
            StorageProvider: "S3"
        );

        // Act
        var updated = await service.UpdateSettingsAsync(updateRequest);

        // Assert
        updated.Should().NotBeNull();
        updated.UseCustomSmtp.Should().BeTrue();
        updated.IsEnvFallbackSmtp.Should().BeFalse();
        updated.SmtpHost.Should().Be("smtp.gmail.com");
        updated.SmtpPort.Should().Be(587);
        updated.SmtpUser.Should().Be("user@gmail.com");
        updated.HasSmtpPassword.Should().BeTrue();
        updated.UseCustomGoogle.Should().BeTrue();
        updated.GoogleClientId.Should().Be("my-custom-google-id");
        updated.HasGoogleClientSecret.Should().BeTrue();
        updated.StorageProvider.Should().Be("S3");

        // Verify that subsequent fetch preserves custom settings without leaking raw password
        var fetched = await service.GetSettingsAsync();
        fetched.UseCustomSmtp.Should().BeTrue();
        fetched.HasSmtpPassword.Should().BeTrue();
        fetched.SmtpHost.Should().Be("smtp.gmail.com");
    }
}
