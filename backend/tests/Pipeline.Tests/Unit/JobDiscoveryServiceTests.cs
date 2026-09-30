using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Applications.DTOs;
using Pipeline.Application.Features.Applications.Services;
using Pipeline.Application.Features.Discovery.Services;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Pipeline.Infrastructure.Services;
using Xunit;

namespace Pipeline.Tests.Unit;

public class JobDiscoveryServiceTests
{
    private readonly PipelineDbContext _dbContext;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Mock<IApplicationService> _appServiceMock;
    private readonly Guid _testUserId = Guid.NewGuid();

    public JobDiscoveryServiceTests()
    {
        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(databaseName: $"DiscoveryTests_{Guid.NewGuid()}")
            .Options;

        _currentUserMock = new Mock<ICurrentUserService>();
        _currentUserMock.Setup(u => u.UserId).Returns(_testUserId);

        _appServiceMock = new Mock<IApplicationService>();

        _dbContext = new PipelineDbContext(options, _currentUserMock.Object);
    }

    [Fact]
    public async Task IngestJobsAsync_ShouldDeduplicateAndComputeUniqueHashes()
    {
        // Arrange
        var connectorMock = new Mock<IJobSourceConnector>();
        connectorMock.Setup(c => c.SourceKey).Returns("test-connector");
        connectorMock.Setup(c => c.FetchAsync(It.IsAny<JobSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RawJob>
            {
                new("id-1", "Staff SRE", "Stripe", "Remote", "https://stripe.com/1", "Desc", DateTimeOffset.UtcNow, new[] { "Go" }),
                new("id-2", "Staff SRE", "Stripe", "Remote", "https://stripe.com/2", "Desc", DateTimeOffset.UtcNow, new[] { "Go" }), // duplicate hash
                new("id-3", "Backend Dev", "GitHub", "San Francisco", "https://github.com/3", "Desc", DateTimeOffset.UtcNow, new[] { "C#" })
            });

        var service = new JobDiscoveryService(
            _dbContext,
            _currentUserMock.Object,
            _appServiceMock.Object,
            new[] { connectorMock.Object });

        // Act
        var result = await service.IngestJobsAsync();

        // Assert
        Assert.Equal(3, result.TotalFetched);
        Assert.Equal(2, result.NewJobsStored);
        Assert.Equal(1, result.DuplicatesSkipped);
        Assert.Equal(2, await _dbContext.DiscoveredJobs.CountAsync());
    }

    [Fact]
    public async Task GetDiscoveredJobsAsync_FiltersByStatusAndReflectsUserState()
    {
        // Arrange
        var job1 = new DiscoveredJob
        {
            Id = Guid.NewGuid(),
            Title = "Senior Go Engineer",
            CompanyName = "HashiCorp",
            Location = "Remote",
            Url = "https://hashicorp.com/1",
            Tags = "[\"Go\", \"Consul\"]",
            Hash = "hash1",
            PostedAt = DateTimeOffset.UtcNow
        };
        var job2 = new DiscoveredJob
        {
            Id = Guid.NewGuid(),
            Title = "Lead Rust Architect",
            CompanyName = "Cloudflare",
            Location = "Austin, TX",
            Url = "https://cloudflare.com/2",
            Tags = "[\"Rust\", \"WASM\"]",
            Hash = "hash2",
            PostedAt = DateTimeOffset.UtcNow
        };
        _dbContext.DiscoveredJobs.AddRange(job1, job2);

        // Mark job1 as Saved for current user
        _dbContext.UserDiscoveredJobStates.Add(new UserDiscoveredJobState
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            DiscoveredJobId = job1.Id,
            State = "Saved"
        });
        await _dbContext.SaveChangesAsync();

        var service = new JobDiscoveryService(
            _dbContext,
            _currentUserMock.Object,
            _appServiceMock.Object,
            Enumerable.Empty<IJobSourceConnector>());

        // Act - Fetch All
        var all = await service.GetDiscoveredJobsAsync();
        Assert.Equal(2, all.TotalCount);

        var savedJob = all.Items.First(j => j.Id == job1.Id);
        var newJob = all.Items.First(j => j.Id == job2.Id);
        Assert.Equal("Saved", savedJob.Status);
        Assert.Equal("New", newJob.Status);

        // Act - Filter by Saved
        var savedOnly = await service.GetDiscoveredJobsAsync(status: "Saved");
        Assert.Single(savedOnly.Items);
        Assert.Equal(job1.Id, savedOnly.Items[0].Id);

        // Act - Filter by New
        var newOnly = await service.GetDiscoveredJobsAsync(status: "New");
        Assert.Single(newOnly.Items);
        Assert.Equal(job2.Id, newOnly.Items[0].Id);
    }

    [Fact]
    public async Task SaveJobToWishlistAsync_CreatesWishlistApplicationAndSetsSavedState()
    {
        // Arrange
        var job = new DiscoveredJob
        {
            Id = Guid.NewGuid(),
            Title = "Distributed Systems Engineer",
            CompanyName = "Datadog",
            Location = "Remote",
            Url = "https://datadog.com/jobs/1",
            Description = "Join the APM backend team.",
            Tags = "[\"Go\", \"Distributed Systems\"]",
            Hash = "hash-datadog",
            PostedAt = DateTimeOffset.UtcNow
        };
        _dbContext.DiscoveredJobs.Add(job);
        await _dbContext.SaveChangesAsync();

        var createdAppId = Guid.NewGuid();
        _appServiceMock.Setup(a => a.CreateApplicationAsync(It.IsAny<CreateApplicationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationDetailDto(
                createdAppId,
                Guid.NewGuid(),
                "Datadog",
                null,
                "Distributed Systems Engineer",
                "https://datadog.com/jobs/1",
                ApplicationSource.Other,
                "Job Discovery",
                ApplicationStatus.Wishlist,
                DateTime.UtcNow,
                null,
                WorkMode.Remote,
                EmploymentType.FullTime,
                "Remote",
                null,
                null,
                "USD",
                "Join the APM backend team.",
                null,
                null,
                null,
                2,
                false,
                3,
                0,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                DateTime.UtcNow,
                DateTime.UtcNow
            ));

        var service = new JobDiscoveryService(
            _dbContext,
            _currentUserMock.Object,
            _appServiceMock.Object,
            Enumerable.Empty<IJobSourceConnector>());

        // Act
        var result = await service.SaveJobToWishlistAsync(job.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdAppId, result.Id);
        Assert.Equal(ApplicationStatus.Wishlist, result.Status);

        var state = await _dbContext.UserDiscoveredJobStates.FirstOrDefaultAsync(s => s.DiscoveredJobId == job.Id);
        Assert.NotNull(state);
        Assert.Equal("Saved", state.State);
    }

    [Fact]
    public async Task DismissJobAsync_SetsStateToDismissed()
    {
        // Arrange
        var job = new DiscoveredJob
        {
            Id = Guid.NewGuid(),
            Title = "DevOps Engineer",
            CompanyName = "AWS",
            Location = "Seattle, WA",
            Url = "https://aws.amazon.com/jobs/2",
            Tags = "[]",
            Hash = "hash-aws",
            PostedAt = DateTimeOffset.UtcNow
        };
        _dbContext.DiscoveredJobs.Add(job);
        await _dbContext.SaveChangesAsync();

        var service = new JobDiscoveryService(
            _dbContext,
            _currentUserMock.Object,
            _appServiceMock.Object,
            Enumerable.Empty<IJobSourceConnector>());

        // Act
        var dismissed = await service.DismissJobAsync(job.Id);

        // Assert
        Assert.True(dismissed);
        var state = await _dbContext.UserDiscoveredJobStates.FirstOrDefaultAsync(s => s.DiscoveredJobId == job.Id);
        Assert.NotNull(state);
        Assert.Equal("Dismissed", state.State);
    }
}
