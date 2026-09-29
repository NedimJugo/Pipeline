using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Interviews.DTOs;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Pipeline.Infrastructure.Services;
using Xunit;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Tests.Unit;

public class InterviewServiceTests
{
    private (PipelineDbContext db, Mock<ICurrentUserService> userMock, InterviewService interviewService) CreateContext(Guid userId)
    {
        var userMock = new Mock<ICurrentUserService>();
        userMock.Setup(u => u.UserId).Returns(userId);
        userMock.Setup(u => u.IsAuthenticated).Returns(true);

        var options = new DbContextOptionsBuilder<PipelineDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new PipelineDbContext(options, userMock.Object);
        var interviewService = new InterviewService(db, userMock.Object);

        return (db, userMock, interviewService);
    }

    private async Task<(Company company, JobApplication app)> SeedAppAsync(PipelineDbContext db, Guid userId)
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Stripe",
            Website = "https://stripe.com"
        };
        db.Companies.Add(company);

        var app = new JobApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyId = company.Id,
            Company = company,
            RoleTitle = "Staff Infrastructure Engineer",
            Status = ApplicationStatus.Interview,
            StatusChangedAt = DateTime.UtcNow
        };
        db.Applications.Add(app);
        await db.SaveChangesAsync();

        return (company, app);
    }

    [Fact]
    public async Task CreateInterview_SeedsDefaultChecklist_AccordingToType()
    {
        var userId = Guid.NewGuid();
        var (db, _, interviewService) = CreateContext(userId);
        var (_, app) = await SeedAppAsync(db, userId);

        var interview = await interviewService.CreateInterviewAsync(new CreateInterviewRequest(
            ApplicationId: app.Id,
            Type: InterviewType.Technical,
            Format: InterviewFormat.Video,
            ScheduledAt: DateTime.UtcNow.AddDays(2),
            DurationMinutes: 60,
            MeetingLink: "https://zoom.us/j/123456789"));

        interview.Should().NotBeNull();
        interview.Type.Should().Be(InterviewType.Technical);
        interview.RoleTitle.Should().Be("Staff Infrastructure Engineer");
        interview.CompanyName.Should().Be("Stripe");
        interview.PrepChecklist.Should().NotBeEmpty();
        interview.PrepChecklist.Should().Contain(i => i.Text.Contains("algorithms") || i.Text.Contains("architecture"));
        interview.PrepChecklist.All(i => !i.Done).Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePrepChecklist_PersistsCheckboxStates()
    {
        var userId = Guid.NewGuid();
        var (db, _, interviewService) = CreateContext(userId);
        var (_, app) = await SeedAppAsync(db, userId);

        var interview = await interviewService.CreateInterviewAsync(new CreateInterviewRequest(
            ApplicationId: app.Id,
            Type: InterviewType.HR,
            ScheduledAt: DateTime.UtcNow.AddDays(1)));

        var updatedChecklist = interview.PrepChecklist
            .Select((item, idx) => idx == 0 ? item with { Done = true } : item)
            .ToList();

        var result = await interviewService.UpdatePrepChecklistAsync(interview.Id, new UpdatePrepChecklistRequest(updatedChecklist));

        result.PrepChecklist[0].Done.Should().BeTrue();
        result.PrepChecklist.Skip(1).All(i => !i.Done).Should().BeTrue();
    }

    [Fact]
    public async Task AddQuestion_And_DeleteQuestion_WorksCorrectly()
    {
        var userId = Guid.NewGuid();
        var (db, _, interviewService) = CreateContext(userId);
        var (_, app) = await SeedAppAsync(db, userId);

        var interview = await interviewService.CreateInterviewAsync(new CreateInterviewRequest(
            ApplicationId: app.Id,
            Type: InterviewType.Culture,
            ScheduledAt: DateTime.UtcNow.AddDays(3)));

        var question = await interviewService.AddQuestionAsync(interview.Id, new AddQuestionRequest(
            Question: "Tell me about a time you resolved a major production incident under high pressure.",
            MyAnswer: "Used blameless post-mortem framework and established clear communications channel.",
            Category: InterviewQuestionCategory.Situational,
            Difficulty: 4,
            WasPrepared: true));

        question.Should().NotBeNull();
        question.Question.Should().Contain("production incident");
        question.Difficulty.Should().Be(4);

        var detailed = await interviewService.GetByIdAsync(interview.Id);
        detailed.Questions.Should().HaveCount(1);

        await interviewService.DeleteQuestionAsync(interview.Id, question.Id);

        var afterDelete = await interviewService.GetByIdAsync(interview.Id);
        afterDelete.Questions.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateDebrief_UpdatesRatingAndMarksCompleted()
    {
        var userId = Guid.NewGuid();
        var (db, _, interviewService) = CreateContext(userId);
        var (_, app) = await SeedAppAsync(db, userId);

        var interview = await interviewService.CreateInterviewAsync(new CreateInterviewRequest(
            ApplicationId: app.Id,
            Type: InterviewType.Technical,
            ScheduledAt: DateTime.UtcNow.AddHours(2)));

        var debrief = await interviewService.UpdateDebriefAsync(interview.Id, new UpdateDebriefRequest(
            SelfRating: 5,
            WentWell: "Solid explanations on distributed caching and concurrency.",
            ToImprove: "Could have asked more questions regarding team's on-call rotation.",
            ThankYouSent: true,
            OutcomeNotes: "Positive initial verbal feedback from interviewer."));

        debrief.SelfRating.Should().Be(5);
        debrief.WentWell.Should().Contain("distributed caching");
        debrief.ThankYouSent.Should().BeTrue();
        debrief.Status.Should().Be(InterviewStatus.Completed);
    }

    [Fact]
    public async Task GenerateIcs_CreatesValidRFC5545Calendar()
    {
        var userId = Guid.NewGuid();
        var (db, _, interviewService) = CreateContext(userId);
        var (_, app) = await SeedAppAsync(db, userId);

        var interview = await interviewService.CreateInterviewAsync(new CreateInterviewRequest(
            ApplicationId: app.Id,
            Type: InterviewType.Final,
            Format: InterviewFormat.Video,
            ScheduledAt: new DateTime(2026, 10, 15, 14, 0, 0, DateTimeKind.Utc),
            DurationMinutes: 45,
            MeetingLink: "https://meet.google.com/abc-defg-hij"));

        var (fileName, content) = await interviewService.GenerateIcsAsync(interview.Id);

        fileName.Should().Be($"interview-{interview.Id}.ics");
        content.Should().Contain("BEGIN:VCALENDAR");
        content.Should().Contain("VERSION:2.0");
        content.Should().Contain("BEGIN:VEVENT");
        content.Should().Contain("DTSTART:20261015T140000Z");
        content.Should().Contain("DTEND:20261015T144500Z");
        content.Should().Contain("SUMMARY:Final Interview: Staff Infrastructure Engineer at Stripe");
        content.Should().Contain("LOCATION:https://meet.google.com/abc-defg-hij");
        content.Should().Contain("BEGIN:VALARM");
        content.Should().Contain("END:VCALENDAR");
    }
}
