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

public class AutomationRuleEngineTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly PipelineDbContext _dbContext;

    public AutomationRuleEngineTests()
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
            UserName = "test@pipeline.local",
            Email = "test@pipeline.local",
            DisplayName = "Test User",
            StaleAfterDays = 14
        });
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task EvaluateRules_FollowUpAfterApply_GeneratesTask()
    {
        var company = new Company { Id = Guid.NewGuid(), Name = "CloudNine" };
        _dbContext.Companies.Add(company);

        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            CompanyId = company.Id,
            Company = company,
            RoleTitle = "DevOps Engineer",
            Status = ApplicationStatus.Applied,
            AppliedAt = DateTime.UtcNow.AddDays(-8),
            StatusChangedAt = DateTime.UtcNow.AddDays(-8)
        };
        _dbContext.Applications.Add(app);
        await _dbContext.SaveChangesAsync();

        var engine = new AutomationRuleEngine(_dbContext);
        var createdCount = await engine.EvaluateRulesForUserAsync(_userId);

        createdCount.Should().BeGreaterOrEqualTo(1);

        var tasks = await _dbContext.Tasks.Where(t => t.UserId == _userId).ToListAsync();
        tasks.Should().Contain(t => t.AutoRuleKey == $"follow_up_after_apply:{app.Id}");
        var task = tasks.First(t => t.AutoRuleKey == $"follow_up_after_apply:{app.Id}");
        task.Title.Should().Contain("DevOps Engineer").And.Contain("CloudNine");
        task.Source.Should().Be(TaskSource.Auto);
    }

    [Fact]
    public async Task EvaluateRules_Idempotency_DoesNotDuplicateTasks()
    {
        var company = new Company { Id = Guid.NewGuid(), Name = "CloudNine" };
        _dbContext.Companies.Add(company);

        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            CompanyId = company.Id,
            Company = company,
            RoleTitle = "DevOps Engineer",
            Status = ApplicationStatus.Applied,
            AppliedAt = DateTime.UtcNow.AddDays(-8),
            StatusChangedAt = DateTime.UtcNow.AddDays(-8)
        };
        _dbContext.Applications.Add(app);
        await _dbContext.SaveChangesAsync();

        var engine = new AutomationRuleEngine(_dbContext);

        // Run once
        var firstRun = await engine.EvaluateRulesForUserAsync(_userId);
        firstRun.Should().BeGreaterOrEqualTo(1);

        // Run second time
        var secondRun = await engine.EvaluateRulesForUserAsync(_userId);
        secondRun.Should().Be(0);

        var tasksCount = await _dbContext.Tasks.CountAsync(t => t.UserId == _userId);
        tasksCount.Should().Be(firstRun);
    }

    [Fact]
    public async Task EvaluateRules_ThankYouAfterCompletedInterview_GeneratesTask()
    {
        var company = new Company { Id = Guid.NewGuid(), Name = "Starlight" };
        _dbContext.Companies.Add(company);

        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            CompanyId = company.Id,
            Company = company,
            RoleTitle = "Principal Engineer",
            Status = ApplicationStatus.Interview
        };
        _dbContext.Applications.Add(app);

        var interview = new Interview
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            ApplicationId = app.Id,
            Type = InterviewType.Technical,
            Format = InterviewFormat.Video,
            Status = InterviewStatus.Completed,
            ScheduledAt = DateTime.UtcNow.AddHours(-2),
            ThankYouSent = false
        };
        _dbContext.Interviews.Add(interview);
        await _dbContext.SaveChangesAsync();

        var engine = new AutomationRuleEngine(_dbContext);
        await engine.EvaluateRulesForUserAsync(_userId);

        var tasks = await _dbContext.Tasks.Where(t => t.UserId == _userId).ToListAsync();
        tasks.Should().Contain(t => t.AutoRuleKey == $"thank_you:{interview.Id}");
        var task = tasks.First(t => t.AutoRuleKey == $"thank_you:{interview.Id}");
        task.Title.Should().Be("Send thank-you to interviewers");
        task.InterviewId.Should().Be(interview.Id);
    }

    [Fact]
    public async Task EvaluateRules_PrepReminder_GeneratesTaskWhenInterviewNearAndIncomplete()
    {
        var company = new Company { Id = Guid.NewGuid(), Name = "Nexis" };
        _dbContext.Companies.Add(company);

        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            CompanyId = company.Id,
            Company = company,
            RoleTitle = "Architect",
            Status = ApplicationStatus.Interview
        };
        _dbContext.Applications.Add(app);

        var interview = new Interview
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            ApplicationId = app.Id,
            Type = InterviewType.Culture,
            Format = InterviewFormat.Video,
            Status = InterviewStatus.Scheduled,
            ScheduledAt = DateTime.UtcNow.AddHours(24),
            PrepChecklist = System.Text.Json.JsonSerializer.Serialize(new[]
            {
                new { text = "Review company values", done = false }
            })
        };
        _dbContext.Interviews.Add(interview);
        await _dbContext.SaveChangesAsync();

        var engine = new AutomationRuleEngine(_dbContext);
        await engine.EvaluateRulesForUserAsync(_userId);

        var tasks = await _dbContext.Tasks.Where(t => t.UserId == _userId).ToListAsync();
        tasks.Should().Contain(t => t.AutoRuleKey == $"prep_reminder:{interview.Id}");
    }
}
