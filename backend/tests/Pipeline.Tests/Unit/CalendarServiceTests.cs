using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Calendar.DTOs;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Pipeline.Infrastructure.Services;
using Xunit;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Tests.Unit;

public class CalendarServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly PipelineDbContext _dbContext;

    public CalendarServiceTests()
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
    public async Task GetEventsAsync_AggregatesInterviewsTasksDeadlinesAndFollowUps()
    {
        var service = new CalendarService(_dbContext, _currentUserServiceMock.Object);

        var company = new Company { Id = Guid.NewGuid(), Name = "Stripe" };
        _dbContext.Companies.Add(company);

        var now = DateTime.UtcNow;

        // 1. Interview
        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            CompanyId = company.Id,
            Company = company,
            RoleTitle = "Senior Infrastructure Engineer",
            Status = ApplicationStatus.Interview,
            OfferDeadline = now.AddDays(7)
        };
        _dbContext.Applications.Add(app);

        _dbContext.Interviews.Add(new Interview
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            ApplicationId = app.Id,
            Application = app,
            Type = InterviewType.Technical,
            ScheduledAt = now.AddDays(2),
            DurationMinutes = 60,
            Location = "https://zoom.us/j/12345",
            Status = InterviewStatus.Scheduled
        });

        // 2. Task
        _dbContext.Tasks.Add(new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            ApplicationId = app.Id,
            Title = "Submit take-home architecture exercise",
            DueAt = now.AddDays(4)
        });

        // 3. Contact Follow-up
        _dbContext.Contacts.Add(new Contact
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            CompanyId = company.Id,
            Company = company,
            FullName = "John Doe",
            Email = "john@stripe.com",
            NextFollowUpAt = now.AddDays(1)
        });

        await _dbContext.SaveChangesAsync();

        var events = await service.GetEventsAsync(now.AddDays(-1), now.AddDays(10));

        events.Should().HaveCount(4);
        events.Should().Contain(e => e.Type == CalendarEventType.Interview && e.Title.Contains("Technical"));
        events.Should().Contain(e => e.Type == CalendarEventType.Task && e.Title.Contains("Submit take-home"));
        events.Should().Contain(e => e.Type == CalendarEventType.OfferDeadline && e.Title.Contains("Offer Deadline"));
        events.Should().Contain(e => e.Type == CalendarEventType.FollowUp && e.Title.Contains("Follow up with John Doe"));
    }

    [Fact]
    public async Task GenerateIcsFeedAsync_ProducesValidRfc5545Calendar()
    {
        var service = new CalendarService(_dbContext, _currentUserServiceMock.Object);

        var user = new User
        {
            Id = _userId,
            UserName = "cal.test@pipeline.local",
            Email = "cal.test@pipeline.local",
            CalendarFeedToken = "secret-token-xyz"
        };
        _dbContext.Users.Add(user);

        var company = new Company { Id = Guid.NewGuid(), Name = "Figma" };
        _dbContext.Companies.Add(company);

        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            CompanyId = company.Id,
            Company = company,
            RoleTitle = "Lead Designer",
            Status = ApplicationStatus.Interview
        };
        _dbContext.Applications.Add(app);

        _dbContext.Interviews.Add(new Interview
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            ApplicationId = app.Id,
            Application = app,
            Type = InterviewType.Final,
            ScheduledAt = DateTime.UtcNow.AddDays(3),
            DurationMinutes = 45,
            Location = "Google Meet",
            Status = InterviewStatus.Scheduled
        });

        await _dbContext.SaveChangesAsync();

        var ics = await service.GenerateIcsFeedAsync("secret-token-xyz");

        ics.Should().NotBeNull();
        ics!.Should().Contain("BEGIN:VCALENDAR");
        ics.Should().Contain("VERSION:2.0");
        ics.Should().Contain("PRODID:-//Pipeline//Job Search Command Center//EN");
        ics.Should().Contain("BEGIN:VEVENT");
        ics.Should().Contain("SUMMARY:");
        ics.Should().Contain("Lead Designer @ Figma");
        ics.Should().Contain("END:VEVENT");
        ics.Should().Contain("END:VCALENDAR");
    }

    [Fact]
    public async Task GenerateIcsFeedAsync_ReturnsNullWhenTokenIsInvalid()
    {
        var service = new CalendarService(_dbContext, _currentUserServiceMock.Object);
        var ics = await service.GenerateIcsFeedAsync("non-existent-token");
        ics.Should().BeNull();
    }
}
