using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Users.DTOs;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Pipeline.Infrastructure.Services;
using Xunit;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Tests.Unit;

public class UserServiceTests
{
    private readonly PipelineDbContext _dbContext;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Guid _testUserId = Guid.NewGuid();

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(databaseName: $"UserServiceTests_{Guid.NewGuid()}")
            .Options;

        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_testUserId);

        _dbContext = new PipelineDbContext(options, _currentUserServiceMock.Object);

        var userStoreMock = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsUserProfileDto()
    {
        // Arrange
        var user = new User
        {
            Id = _testUserId,
            Email = "tester@example.com",
            DisplayName = "Test User",
            TargetRole = "Senior Engineer",
            Seniority = "Senior",
            SalaryExpectationMin = 150000,
            SalaryExpectationMax = 180000,
            Currency = "USD",
            SearchStatus = SearchStatus.Active,
            Timezone = "UTC"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var service = new UserService(_dbContext, _currentUserServiceMock.Object, _userManagerMock.Object);

        // Act
        var result = await service.GetProfileAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("tester@example.com", result.Email);
        Assert.Equal("Test User", result.DisplayName);
        Assert.Equal("Senior Engineer", result.TargetRole);
        Assert.Equal(150000, result.SalaryExpectationMin);
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesFieldsSuccessfully()
    {
        // Arrange
        var user = new User
        {
            Id = _testUserId,
            Email = "update_test@example.com",
            DisplayName = "Old Name"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var service = new UserService(_dbContext, _currentUserServiceMock.Object, _userManagerMock.Object);

        // Act
        var request = new UpdateProfileRequest(
            DisplayName: "New Name",
            TargetRole: "Staff Engineer",
            Seniority: "Staff",
            Location: "Remote",
            SalaryExpectationMin: 180000,
            SalaryExpectationMax: 210000,
            Currency: "USD",
            SearchStatus: SearchStatus.Passive,
            Timezone: "America/New_York"
        );
        var result = await service.UpdateProfileAsync(request);

        // Assert
        Assert.Equal("New Name", result.DisplayName);
        Assert.Equal("Staff Engineer", result.TargetRole);
        Assert.Equal(SearchStatus.Passive, result.SearchStatus);
        Assert.Equal("America/New_York", result.Timezone);
    }

    [Fact]
    public async Task ExportGdprDataAsync_ReturnsAllUserEntities()
    {
        // Arrange
        var user = new User
        {
            Id = _testUserId,
            Email = "gdpr@example.com",
            DisplayName = "GDPR Tester"
        };
        _dbContext.Users.Add(user);

        var company = new Company { Id = Guid.NewGuid(), Name = "Stripe" };
        _dbContext.Companies.Add(company);

        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            CompanyId = company.Id,
            RoleTitle = "Staff Engineer",
            Status = ApplicationStatus.Offer
        };
        _dbContext.Applications.Add(app);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Title = "Follow up"
        };
        _dbContext.Tasks.Add(task);

        await _dbContext.SaveChangesAsync();

        var service = new UserService(_dbContext, _currentUserServiceMock.Object, _userManagerMock.Object);

        // Act
        var result = await service.ExportGdprDataAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("gdpr@example.com", result.Profile.Email);
        Assert.NotNull(result.Applications);
        Assert.NotNull(result.Tasks);
    }

    [Fact]
    public async Task SeedDemoDataAsync_PopulatesRealisticJobSearchData()
    {
        // Arrange
        var user = new User
        {
            Id = _testUserId,
            Email = "demo@example.com"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var service = new UserService(_dbContext, _currentUserServiceMock.Object, _userManagerMock.Object);

        // Act
        await service.SeedDemoDataAsync();

        // Assert
        var appsCount = await _dbContext.Applications.CountAsync(a => a.UserId == _testUserId);
        var contactsCount = await _dbContext.Contacts.CountAsync(c => c.UserId == _testUserId);
        var interviewsCount = await _dbContext.Interviews.CountAsync(i => i.UserId == _testUserId);
        var tasksCount = await _dbContext.Tasks.CountAsync(t => t.UserId == _testUserId);
        var updatedUser = await _dbContext.Users.FindAsync(_testUserId);

        Assert.True(appsCount >= 8, $"Expected >= 8 applications, got {appsCount}");
        Assert.True(contactsCount >= 1, "Expected contacts to be seeded");
        Assert.True(interviewsCount >= 1, "Expected interviews to be seeded");
        Assert.True(tasksCount >= 1, "Expected tasks to be seeded");
        Assert.True(updatedUser?.OnboardingCompleted);
    }

    [Fact]
    public async Task DeleteAccountAsync_PurgesAllUserData()
    {
        // Arrange
        var user = new User
        {
            Id = _testUserId,
            Email = "delete_me@example.com"
        };
        _dbContext.Users.Add(user);

        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            RoleTitle = "Senior Dev",
            Status = ApplicationStatus.Applied
        };
        _dbContext.Applications.Add(app);

        await _dbContext.SaveChangesAsync();

        var service = new UserService(_dbContext, _currentUserServiceMock.Object, _userManagerMock.Object);

        // Act
        await service.DeleteAccountAsync();

        // Assert
        var userExists = await _dbContext.Users.AnyAsync(u => u.Id == _testUserId);
        var appExists = await _dbContext.Applications.AnyAsync(a => a.UserId == _testUserId);

        Assert.False(userExists);
        Assert.False(appExists);
    }
}
