using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Xunit;

namespace Pipeline.Tests.Unit;

public class NedimDataSeederTests
{
    private readonly PipelineDbContext _dbContext;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Guid _testUserId = Guid.NewGuid();

    public NedimDataSeederTests()
    {
        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(databaseName: $"NedimDataSeederTests_{Guid.NewGuid()}")
            .Options;

        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_testUserId);

        _dbContext = new PipelineDbContext(options, _currentUserServiceMock.Object);

        var userStoreMock = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((string email) => _dbContext.Users.FirstOrDefault(u => u.Email == email));

        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync((User user, string pass) =>
            {
                _dbContext.Users.Add(user);
                _dbContext.SaveChanges();
                return IdentityResult.Success;
            });
    }

    [Fact]
    public async Task SeedAsync_PopulatesAll24Applications_WithExactStageBreakdown()
    {
        // Act
        var user = await NedimDataSeeder.SeedAsync(_dbContext, _userManagerMock.Object, targetUserId: null);

        // Assert User
        Assert.NotNull(user);
        Assert.Equal("Nedim Jugo", user.DisplayName);
        Assert.Equal(NedimDataSeeder.NedimEmail, user.Email);
        Assert.Equal(1750, user.SalaryExpectationMin);
        Assert.Equal("BAM", user.Currency);
        Assert.True(user.OnboardingCompleted);

        // Assert 24 Applications
        var apps = await _dbContext.Applications.IgnoreQueryFilters().Where(a => a.UserId == user.Id).ToListAsync();
        Assert.Equal(24, apps.Count);

        // Assert exact Stage Breakdown
        var ghostedCount = apps.Count(a => a.Status == ApplicationStatus.Ghosted);
        var rejectedCount = apps.Count(a => a.Status == ApplicationStatus.Rejected);
        var appliedCount = apps.Count(a => a.Status == ApplicationStatus.Applied);
        var withdrawnCount = apps.Count(a => a.Status == ApplicationStatus.Withdrawn);

        Assert.Equal(9, ghostedCount);
        Assert.Equal(11, rejectedCount);
        Assert.Equal(3, appliedCount);
        Assert.Equal(1, withdrawnCount);

        // Assert UniCredit offer
        var unicreditApp = apps.First(a => a.RoleTitle.Contains("IT sektor"));
        Assert.Equal(1450, unicreditApp.OfferSalary);
        Assert.NotNull(unicreditApp.OfferNegotiationNotes);

        // Assert Contacts >= 27
        var contacts = await _dbContext.Contacts.IgnoreQueryFilters().Where(c => c.UserId == user.Id).ToListAsync();
        Assert.True(contacts.Count >= 27, $"Expected at least 27 contacts, got {contacts.Count}");

        // Assert Interactions == 62
        var interactions = await _dbContext.Interactions.IgnoreQueryFilters().Where(i => i.UserId == user.Id).ToListAsync();
        Assert.Equal(62, interactions.Count);

        // Assert MoP Screening Interview
        var interview = await _dbContext.Interviews.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.UserId == user.Id);
        Assert.NotNull(interview);
        Assert.Equal(InterviewStatus.Completed, interview.Status);

        // Assert Tasks
        var tasks = await _dbContext.Tasks.IgnoreQueryFilters().Where(t => t.UserId == user.Id).ToListAsync();
        Assert.True(tasks.Count >= 4);

        // Assert Idempotency: Running seeder again does not duplicate applications or interactions
        await NedimDataSeeder.SeedAsync(_dbContext, _userManagerMock.Object, targetUserId: user.Id);
        var appsAfter = await _dbContext.Applications.IgnoreQueryFilters().Where(a => a.UserId == user.Id).ToListAsync();
        var interactionsAfter = await _dbContext.Interactions.IgnoreQueryFilters().Where(i => i.UserId == user.Id).ToListAsync();
        Assert.Equal(24, appsAfter.Count);
        Assert.Equal(62, interactionsAfter.Count);
    }
}
