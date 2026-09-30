using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Pipeline.Infrastructure.Services;
using Xunit;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Tests.Unit;

public class AnalyticsServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly PipelineDbContext _dbContext;

    public AnalyticsServiceTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);

        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PipelineDbContext(options, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task GetFunnelAsync_ComputesCorrectConversionCountsAndPercentages()
    {
        var service = new AnalyticsService(_dbContext, _currentUserServiceMock.Object);

        var company = new Company { Id = Guid.NewGuid(), Name = "Tech Corp" };
        _dbContext.Companies.Add(company);

        // 1 Wishlist, 2 Applied, 2 Screening, 2 Interview, 1 Offer, 1 Accepted
        var statuses = new[]
        {
            ApplicationStatus.Wishlist,
            ApplicationStatus.Applied,
            ApplicationStatus.Applied,
            ApplicationStatus.Screening,
            ApplicationStatus.Screening,
            ApplicationStatus.Interview,
            ApplicationStatus.Interview,
            ApplicationStatus.Offer,
            ApplicationStatus.Accepted
        };

        foreach (var status in statuses)
        {
            _dbContext.Applications.Add(new JobApplication
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                CompanyId = company.Id,
                RoleTitle = "Software Engineer",
                Status = status,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            });
        }
        await _dbContext.SaveChangesAsync();

        var funnel = await service.GetFunnelAsync();

        funnel.Should().HaveCount(5);
        var appliedStage = funnel.First(f => f.Stage == "Applied");
        var screeningStage = funnel.First(f => f.Stage == "Screening");
        var interviewStage = funnel.First(f => f.Stage == "Interview");
        var offerStage = funnel.First(f => f.Stage == "Offer");
        var acceptedStage = funnel.First(f => f.Stage == "Accepted");

        // Applied count = all from Applied to Accepted (8 total)
        appliedStage.Count.Should().Be(8);
        appliedStage.ConversionFromApplied.Should().Be(100.0);

        // Screening = 2 Screening + 2 Interview + 1 Offer + 1 Accepted = 6
        screeningStage.Count.Should().Be(6);
        screeningStage.ConversionFromApplied.Should().Be(75.0);

        // Interview = 2 Interview + 1 Offer + 1 Accepted = 4
        interviewStage.Count.Should().Be(4);
        interviewStage.ConversionFromApplied.Should().Be(50.0);

        // Offer = 1 Offer + 1 Accepted = 2
        offerStage.Count.Should().Be(2);

        // Accepted = 1
        acceptedStage.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetBySourceAsync_CalculatesResponseAndInterviewRates()
    {
        var service = new AnalyticsService(_dbContext, _currentUserServiceMock.Object);
        var company = new Company { Id = Guid.NewGuid(), Name = "Meta" };
        _dbContext.Companies.Add(company);

        // 3 LinkedIn: 1 Applied, 1 Screening, 1 Interview
        // 2 Referral: 1 Interview, 1 Offer
        _dbContext.Applications.AddRange(
            new JobApplication { Id = Guid.NewGuid(), UserId = _userId, CompanyId = company.Id, RoleTitle = "Eng 1", Status = ApplicationStatus.Applied, Source = ApplicationSource.LinkedIn, CreatedAt = DateTime.UtcNow },
            new JobApplication { Id = Guid.NewGuid(), UserId = _userId, CompanyId = company.Id, RoleTitle = "Eng 2", Status = ApplicationStatus.Screening, Source = ApplicationSource.LinkedIn, CreatedAt = DateTime.UtcNow },
            new JobApplication { Id = Guid.NewGuid(), UserId = _userId, CompanyId = company.Id, RoleTitle = "Eng 3", Status = ApplicationStatus.Interview, Source = ApplicationSource.LinkedIn, CreatedAt = DateTime.UtcNow },
            new JobApplication { Id = Guid.NewGuid(), UserId = _userId, CompanyId = company.Id, RoleTitle = "Eng 4", Status = ApplicationStatus.Interview, Source = ApplicationSource.Referral, CreatedAt = DateTime.UtcNow },
            new JobApplication { Id = Guid.NewGuid(), UserId = _userId, CompanyId = company.Id, RoleTitle = "Eng 5", Status = ApplicationStatus.Offer, Source = ApplicationSource.Referral, CreatedAt = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var breakdown = await service.GetBySourceAsync();

        var linkedin = breakdown.FirstOrDefault(b => b.Label == "LinkedIn");
        linkedin.Should().NotBeNull();
        linkedin!.TotalApplications.Should().Be(3);
        // Responses for LinkedIn = Screening + Interview = 2 out of 3 = 66.7%
        linkedin.Responses.Should().Be(2);
        linkedin.ResponseRate.Should().BeApproximately(66.67, 0.1);
        linkedin.Interviews.Should().Be(1);
        linkedin.InterviewRate.Should().BeApproximately(33.33, 0.1);

        var referral = breakdown.FirstOrDefault(b => b.Label == "Referral");
        referral.Should().NotBeNull();
        referral!.TotalApplications.Should().Be(2);
        referral.Responses.Should().Be(2);
        referral.ResponseRate.Should().Be(100.0);
    }

    [Fact]
    public async Task GetStageDurationsAsync_CalculatesAverageAndMedianDays()
    {
        var service = new AnalyticsService(_dbContext, _currentUserServiceMock.Object);
        var company = new Company { Id = Guid.NewGuid(), Name = "Netflix" };
        _dbContext.Companies.Add(company);

        var app1 = new JobApplication { Id = Guid.NewGuid(), UserId = _userId, CompanyId = company.Id, RoleTitle = "Eng", Status = ApplicationStatus.Interview, CreatedAt = DateTime.UtcNow.AddDays(-20) };
        _dbContext.Applications.Add(app1);

        // App 1 stayed in Applied for 4 days (CreatedAt -20 to -16)
        _dbContext.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            ApplicationId = app1.Id,
            Application = app1,
            FromStatus = ApplicationStatus.Applied,
            ToStatus = ApplicationStatus.Screening,
            ChangedAt = DateTime.UtcNow.AddDays(-16)
        });

        // App 2 stayed in Applied for 10 days (CreatedAt -15 to -5)
        var app2 = new JobApplication { Id = Guid.NewGuid(), UserId = _userId, CompanyId = company.Id, RoleTitle = "Eng", Status = ApplicationStatus.Screening, CreatedAt = DateTime.UtcNow.AddDays(-15) };
        _dbContext.Applications.Add(app2);
        _dbContext.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            ApplicationId = app2.Id,
            Application = app2,
            FromStatus = ApplicationStatus.Applied,
            ToStatus = ApplicationStatus.Screening,
            ChangedAt = DateTime.UtcNow.AddDays(-5)
        });

        await _dbContext.SaveChangesAsync();

        var stageDurations = await service.GetStageDurationsAsync();
        var appliedDuration = stageDurations.FirstOrDefault(d => d.Stage == "Applied");

        appliedDuration.Should().NotBeNull();
        appliedDuration!.SampleCount.Should().Be(2);
        appliedDuration.AverageDays.Should().BeApproximately(7.0, 0.2); // (4 + 10) / 2
        appliedDuration.MedianDays.Should().BeApproximately(7.0, 0.2);
    }

    [Fact]
    public async Task GetInsightsAsync_RespectsMinimumSampleSizeThreshold()
    {
        var service = new AnalyticsService(_dbContext, _currentUserServiceMock.Object);
        var company = new Company { Id = Guid.NewGuid(), Name = "Apple" };
        _dbContext.Companies.Add(company);

        // Only 3 applications: below N >= 5 minimum sample size
        _dbContext.Applications.AddRange(
            new JobApplication { Id = Guid.NewGuid(), UserId = _userId, CompanyId = company.Id, RoleTitle = "Eng 1", Status = ApplicationStatus.Applied, Source = ApplicationSource.LinkedIn, CreatedAt = DateTime.UtcNow },
            new JobApplication { Id = Guid.NewGuid(), UserId = _userId, CompanyId = company.Id, RoleTitle = "Eng 2", Status = ApplicationStatus.Applied, Source = ApplicationSource.Referral, CreatedAt = DateTime.UtcNow },
            new JobApplication { Id = Guid.NewGuid(), UserId = _userId, CompanyId = company.Id, RoleTitle = "Eng 3", Status = ApplicationStatus.Offer, Source = ApplicationSource.Referral, CreatedAt = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var insights = await service.GetInsightsAsync();

        // When sample size < 5, sample-size-dependent insights must indicate threshold not met
        var sampleSizeInsight = insights.FirstOrDefault(i => i.Id == "min_sample_size");
        sampleSizeInsight.Should().NotBeNull();
        sampleSizeInsight!.MinSampleSizeMet.Should().BeFalse();
    }
}
