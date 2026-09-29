using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Xunit;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Tests.Unit;

public class UserIsolationTests
{
    [Fact]
    public async Task QueryingApplications_ReturnsOnlyCurrentUserRecords()
    {
        // Arrange
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.UserId).Returns(userAId);
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);

        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var seedContext = new PipelineDbContext(options, new Mock<ICurrentUserService>().Object))
        {
            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "Tech Corp",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            seedContext.Companies.Add(company);

            seedContext.Applications.AddRange(
                new JobApplication
                {
                    Id = Guid.NewGuid(),
                    UserId = userAId,
                    CompanyId = company.Id,
                    RoleTitle = "Senior .NET Engineer",
                    Status = ApplicationStatus.Applied,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new JobApplication
                {
                    Id = Guid.NewGuid(),
                    UserId = userBId,
                    CompanyId = company.Id,
                    RoleTitle = "Secret Role For User B",
                    Status = ApplicationStatus.Interview,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            await seedContext.SaveChangesAsync();
        }

        // Act - query with User A's context
        using (var context = new PipelineDbContext(options, currentUserServiceMock.Object))
        {
            var apps = await context.Applications.ToListAsync();

            // Assert
            apps.Should().HaveCount(1);
            apps[0].RoleTitle.Should().Be("Senior .NET Engineer");
            apps[0].UserId.Should().Be(userAId);
        }
    }

    [Fact]
    public async Task QueryingApplications_ExcludesSoftDeletedRecords()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);

        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var seedContext = new PipelineDbContext(options, new Mock<ICurrentUserService>().Object))
        {
            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "Acme Corp",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            seedContext.Companies.Add(company);

            seedContext.Applications.AddRange(
                new JobApplication
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CompanyId = company.Id,
                    RoleTitle = "Active Role",
                    Status = ApplicationStatus.Applied,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    DeletedAt = null
                },
                new JobApplication
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CompanyId = company.Id,
                    RoleTitle = "Deleted Role",
                    Status = ApplicationStatus.Applied,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    DeletedAt = DateTime.UtcNow
                }
            );

            await seedContext.SaveChangesAsync();
        }

        // Act
        using (var context = new PipelineDbContext(options, currentUserServiceMock.Object))
        {
            var apps = await context.Applications.ToListAsync();

            // Assert
            apps.Should().HaveCount(1);
            apps[0].RoleTitle.Should().Be("Active Role");
        }
    }
}
