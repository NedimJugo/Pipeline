using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Applications.DTOs;
using Pipeline.Application.Features.Applications.Services;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Pipeline.Infrastructure.Services;
using Xunit;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Tests.Unit;

public class ApplicationServiceTests
{
    [Fact]
    public async Task CreateApplication_RecordsInitialStatusHistory()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);

        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new PipelineDbContext(options, currentUserServiceMock.Object);
        var companyService = new CompanyService(dbContext, currentUserServiceMock.Object);
        var applicationService = new ApplicationService(dbContext, currentUserServiceMock.Object, companyService);

        var request = new CreateApplicationRequest(
            RoleTitle: "Senior Cloud Architect",
            CompanyName: "Acme Cloud",
            Status: ApplicationStatus.Applied,
            WorkMode: WorkMode.Remote);

        // Act
        var created = await applicationService.CreateApplicationAsync(request);

        // Assert
        created.Should().NotBeNull();
        created.RoleTitle.Should().Be("Senior Cloud Architect");
        created.Status.Should().Be(ApplicationStatus.Applied);
        created.CompanyName.Should().Be("Acme Cloud");

        // Verify history in DB
        var history = await dbContext.ApplicationStatusHistories
            .Where(h => h.ApplicationId == created.Id)
            .ToListAsync();

        history.Should().HaveCount(1);
        history[0].ToStatus.Should().Be(ApplicationStatus.Applied);
        history[0].UserId.Should().Be(userId);
    }

    [Fact]
    public async Task UpdateStatus_UpdatesStatusChangedAtAndAppendsHistory()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);

        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new PipelineDbContext(options, currentUserServiceMock.Object);
        var companyService = new CompanyService(dbContext, currentUserServiceMock.Object);
        var applicationService = new ApplicationService(dbContext, currentUserServiceMock.Object, companyService);

        var initial = await applicationService.CreateApplicationAsync(new CreateApplicationRequest(
            RoleTitle: "Staff Engineer",
            CompanyName: "Google",
            Status: ApplicationStatus.Applied));

        // Act
        var updated = await applicationService.UpdateStatusAsync(initial.Id, new UpdateStatusRequest(
            Status: ApplicationStatus.Interview,
            Note: "Passed recruiter screen"));

        // Assert
        updated.Status.Should().Be(ApplicationStatus.Interview);

        var history = await dbContext.ApplicationStatusHistories
            .Where(h => h.ApplicationId == initial.Id)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

        history.Should().HaveCount(2);
        history[1].FromStatus.Should().Be(ApplicationStatus.Applied);
        history[1].ToStatus.Should().Be(ApplicationStatus.Interview);
        history[1].Note.Should().Be("Passed recruiter screen");
    }

    [Fact]
    public async Task DuplicateApplication_CreatesWishlistCopy()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);

        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new PipelineDbContext(options, currentUserServiceMock.Object);
        var companyService = new CompanyService(dbContext, currentUserServiceMock.Object);
        var applicationService = new ApplicationService(dbContext, currentUserServiceMock.Object, companyService);

        var original = await applicationService.CreateApplicationAsync(new CreateApplicationRequest(
            RoleTitle: "Principal Engineer",
            CompanyName: "Stripe",
            Status: ApplicationStatus.Offer,
            JobDescription: "Lead backend infrastructure"));

        // Act
        var duplicate = await applicationService.DuplicateApplicationAsync(original.Id);

        // Assert
        duplicate.Id.Should().NotBe(original.Id);
        duplicate.RoleTitle.Should().Be("[Copy] Principal Engineer");
        duplicate.Status.Should().Be(ApplicationStatus.Wishlist);
        duplicate.CompanyName.Should().Be("Stripe");
        duplicate.JobDescription.Should().Be("Lead backend infrastructure");
    }

    [Fact]
    public async Task CreateApplication_WithFutureAppliedAt_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);

        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new PipelineDbContext(options, currentUserServiceMock.Object);
        var companyService = new CompanyService(dbContext, currentUserServiceMock.Object);
        var applicationService = new ApplicationService(dbContext, currentUserServiceMock.Object, companyService);

        var futureDate = DateTime.UtcNow.AddDays(7);
        var request = new CreateApplicationRequest(
            RoleTitle: "Lead Engineer",
            CompanyName: "FutureTech",
            Status: ApplicationStatus.Applied,
            AppliedAt: futureDate);

        // Act & Assert
        var act = async () => await applicationService.CreateApplicationAsync(request);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Application date cannot be in the future*");
    }

    [Fact]
    public async Task CreateApplication_WithHistoricalPastAppliedAt_SucceedsAndPersistsDate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);

        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new PipelineDbContext(options, currentUserServiceMock.Object);
        var companyService = new CompanyService(dbContext, currentUserServiceMock.Object);
        var applicationService = new ApplicationService(dbContext, currentUserServiceMock.Object, companyService);

        var pastDate = DateTime.UtcNow.AddMonths(-3);
        var request = new CreateApplicationRequest(
            RoleTitle: "Staff Engineer",
            CompanyName: "PastCorp",
            Status: ApplicationStatus.Applied,
            AppliedAt: pastDate);

        // Act
        var created = await applicationService.CreateApplicationAsync(request);

        // Assert
        created.Should().NotBeNull();
        created.AppliedAt.Should().BeCloseTo(pastDate, TimeSpan.FromSeconds(1));
    }
}
