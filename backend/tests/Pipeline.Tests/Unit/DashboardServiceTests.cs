using System;
using System.Collections.Generic;
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

public class DashboardServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly PipelineDbContext _dbContext;

    public DashboardServiceTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(s => s.IsAuthenticated).Returns(true);

        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PipelineDbContext(options, _currentUserServiceMock.Object);

        // Add user
        _dbContext.Users.Add(new User
        {
            Id = _userId,
            UserName = "dashboard@pipeline.local",
            Email = "dashboard@pipeline.local",
            DisplayName = "Alex Mercer",
            TargetRole = "Senior Full-Stack Engineer",
            SearchStatus = SearchStatus.Active,
            StaleAfterDays = 14
        });
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetDashboard_AggregatesDoTodayAndInterviews()
    {
        var company = new Company { Id = Guid.NewGuid(), Name = "FinTech Corp" };
        _dbContext.Companies.Add(company);

        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            CompanyId = company.Id,
            Company = company,
            RoleTitle = "Tech Lead",
            Status = ApplicationStatus.Interview
        };
        _dbContext.Applications.Add(app);

        // Add task due today
        _dbContext.Tasks.Add(new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            ApplicationId = app.Id,
            Title = "Follow up with recruiter",
            DueAt = DateTime.UtcNow,
            Source = TaskSource.Manual
        });

        // Add upcoming interview in 2 days
        _dbContext.Interviews.Add(new Interview
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            ApplicationId = app.Id,
            Type = InterviewType.Technical,
            Format = InterviewFormat.Video,
            Status = InterviewStatus.Scheduled,
            ScheduledAt = DateTime.UtcNow.AddDays(2),
            PrepChecklist = System.Text.Json.JsonSerializer.Serialize(new[]
            {
                new { text = "Read System Design doc", done = true },
                new { text = "Practice algorithm problem", done = false }
            })
        });

        await _dbContext.SaveChangesAsync();

        var automationEngine = new AutomationRuleEngine(_dbContext);
        var dashboardService = new DashboardService(_dbContext, _currentUserServiceMock.Object, automationEngine);

        var result = await dashboardService.GetDashboardAsync();

        result.Should().NotBeNull();
        result.Greeting.Should().Contain("Alex Mercer");
        result.TargetRole.Should().Be("Senior Full-Stack Engineer");
        result.SearchStatus.Should().Be(SearchStatus.Active);
        result.DoToday.Should().HaveCount(1);
        result.DoToday[0].Title.Should().Be("Follow up with recruiter");
        result.UpcomingInterviews.Should().HaveCount(1);
        result.UpcomingInterviews[0].CompanyName.Should().Be("FinTech Corp");
        result.UpcomingInterviews[0].PrepChecklistTotal.Should().Be(2);
        result.UpcomingInterviews[0].PrepChecklistCompleted.Should().Be(1);
        result.UpcomingInterviews[0].PrepProgressPercent.Should().Be(50);
    }
}
