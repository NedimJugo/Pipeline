using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Tasks.DTOs;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;
using Pipeline.Infrastructure.Services;
using Xunit;

namespace Pipeline.Tests.Unit;

public class TaskServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly PipelineDbContext _dbContext;

    public TaskServiceTests()
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
    public async Task CreateTask_PersistsAndReturnsTask()
    {
        var service = new TaskService(_dbContext, _currentUserServiceMock.Object);
        var request = new CreateTaskRequest(
            Title: "Follow up with recruiter",
            Notes: "Check email sent on Monday",
            DueAt: DateTime.UtcNow.AddDays(1));

        var result = await service.CreateTaskAsync(request);

        result.Should().NotBeNull();
        result.Title.Should().Be("Follow up with recruiter");
        result.Notes.Should().Be("Check email sent on Monday");
        result.Source.Should().Be(TaskSource.Manual);
        result.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task CompleteTask_TogglesCompletionTimestamp()
    {
        var service = new TaskService(_dbContext, _currentUserServiceMock.Object);
        var created = await service.CreateTaskAsync(new CreateTaskRequest("Test task"));

        // Complete it
        var completed = await service.CompleteTaskAsync(created.Id, true);
        completed.CompletedAt.Should().NotBeNull();

        // Un-complete it
        var uncompleted = await service.CompleteTaskAsync(created.Id, false);
        uncompleted.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task SnoozeTask_PostponesDueDate()
    {
        var service = new TaskService(_dbContext, _currentUserServiceMock.Object);
        var originalDue = DateTime.UtcNow;
        var created = await service.CreateTaskAsync(new CreateTaskRequest("Snooze task", DueAt: originalDue));

        var snoozed = await service.SnoozeTaskAsync(created.Id, 3);
        snoozed.DueAt.Should().NotBeNull();
        snoozed.DueAt!.Value.Date.Should().Be(DateTime.UtcNow.AddDays(3).Date);
    }

    [Fact]
    public async Task FilterTasks_ByView_ReturnsCorrectItems()
    {
        var service = new TaskService(_dbContext, _currentUserServiceMock.Object);

        // Due today
        await service.CreateTaskAsync(new CreateTaskRequest("Task today", DueAt: DateTime.UtcNow));
        // Overdue
        await service.CreateTaskAsync(new CreateTaskRequest("Task overdue", DueAt: DateTime.UtcNow.AddDays(-2)));
        // Upcoming
        await service.CreateTaskAsync(new CreateTaskRequest("Task upcoming", DueAt: DateTime.UtcNow.AddDays(4)));

        var todayResult = await service.GetTasksAsync(new TaskFilterParams(View: "today"));
        todayResult.Items.Should().Contain(t => t.Title == "Task today");

        var overdueResult = await service.GetTasksAsync(new TaskFilterParams(View: "overdue"));
        overdueResult.Items.Should().Contain(t => t.Title == "Task overdue");

        var upcomingResult = await service.GetTasksAsync(new TaskFilterParams(View: "upcoming"));
        upcomingResult.Items.Should().Contain(t => t.Title == "Task upcoming");
    }
}
