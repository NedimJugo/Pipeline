using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Tasks.DTOs;
using Pipeline.Application.Features.Tasks.Services;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using Pipeline.Infrastructure.Persistence;

namespace Pipeline.Infrastructure.Services;

public class TaskService : ITaskService
{
    private readonly PipelineDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public TaskService(PipelineDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<PagedTasksResult> GetTasksAsync(TaskFilterParams filter, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var query = _dbContext.Tasks
            .Include(t => t.Application)
                .ThenInclude(a => a != null ? a.Company : null)
            .Include(t => t.Contact)
            .Include(t => t.Interview)
            .Where(t => t.UserId == userId)
            .AsNoTracking();

        var today = DateTime.UtcNow.Date;

        if (!string.IsNullOrWhiteSpace(filter.View))
        {
            var view = filter.View.Trim().ToLowerInvariant();
            switch (view)
            {
                case "today":
                    query = query.Where(t => t.CompletedAt == null && t.DueAt != null && t.DueAt.Value.Date <= today);
                    break;
                case "upcoming":
                    query = query.Where(t => t.CompletedAt == null && t.DueAt != null && t.DueAt.Value.Date > today);
                    break;
                case "overdue":
                    query = query.Where(t => t.CompletedAt == null && t.DueAt != null && t.DueAt.Value.Date < today);
                    break;
                case "done":
                    query = query.Where(t => t.CompletedAt != null);
                    break;
            }
        }

        if (filter.Source.HasValue)
        {
            query = query.Where(t => t.Source == filter.Source.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(t => t.Title.ToLower().Contains(search) || (t.Notes != null && t.Notes.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(ct);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 50 : filter.PageSize;

        var items = await query
            .OrderBy(t => t.CompletedAt != null)
            .ThenBy(t => t.DueAt.HasValue ? 0 : 1)
            .ThenBy(t => t.DueAt)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TaskItemDto(
                t.Id,
                t.ApplicationId,
                t.Application != null ? t.Application.RoleTitle : null,
                t.Application != null && t.Application.Company != null ? t.Application.Company.Name : null,
                t.ContactId,
                t.Contact != null ? t.Contact.FullName : null,
                t.InterviewId,
                t.Interview != null ? $"{t.Interview.Type} Interview" : null,
                t.Title,
                t.Notes,
                t.DueAt,
                t.CompletedAt,
                t.Source,
                t.AutoRuleKey,
                t.CreatedAt
            ))
            .ToListAsync(ct);

        return new PagedTasksResult(items, totalCount, page, pageSize);
    }

    public async Task<TaskItemDto?> GetTaskByIdAsync(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var task = await _dbContext.Tasks
            .Include(t => t.Application)
                .ThenInclude(a => a != null ? a.Company : null)
            .Include(t => t.Contact)
            .Include(t => t.Interview)
            .Where(t => t.UserId == userId && t.Id == id)
            .FirstOrDefaultAsync(ct);

        if (task == null) return null;

        return MapToDto(task);
    }

    public async Task<TaskItemDto> CreateTaskAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title,
            Notes = request.Notes,
            DueAt = request.DueAt,
            ApplicationId = request.ApplicationId,
            ContactId = request.ContactId,
            InterviewId = request.InterviewId,
            Source = request.Source,
            AutoRuleKey = request.AutoRuleKey,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync(ct);

        return await GetTaskByIdAsync(task.Id, ct) ?? MapToDto(task);
    }

    public async Task<TaskItemDto> UpdateTaskAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
        if (task == null)
        {
            throw new KeyNotFoundException($"Task with ID {id} not found.");
        }

        task.Title = request.Title;
        task.Notes = request.Notes;
        task.DueAt = request.DueAt;
        task.ApplicationId = request.ApplicationId;
        task.ContactId = request.ContactId;
        task.InterviewId = request.InterviewId;
        task.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
        return await GetTaskByIdAsync(task.Id, ct) ?? MapToDto(task);
    }

    public async Task DeleteTaskAsync(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
        if (task != null)
        {
            _dbContext.Tasks.Remove(task);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task<TaskItemDto> CompleteTaskAsync(Guid id, bool isCompleted, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
        if (task == null)
        {
            throw new KeyNotFoundException($"Task with ID {id} not found.");
        }

        task.CompletedAt = isCompleted ? DateTime.UtcNow : null;
        task.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
        return await GetTaskByIdAsync(task.Id, ct) ?? MapToDto(task);
    }

    public async Task<TaskItemDto> SnoozeTaskAsync(Guid id, int days, CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);
        if (task == null)
        {
            throw new KeyNotFoundException($"Task with ID {id} not found.");
        }

        var baseDate = DateTime.UtcNow.Date;
        task.DueAt = baseDate.AddDays(days > 0 ? days : 1).AddHours(12);
        task.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
        return await GetTaskByIdAsync(task.Id, ct) ?? MapToDto(task);
    }

    private static TaskItemDto MapToDto(TaskItem task)
    {
        return new TaskItemDto(
            task.Id,
            task.ApplicationId,
            task.Application?.RoleTitle,
            task.Application?.Company?.Name,
            task.ContactId,
            task.Contact?.FullName,
            task.InterviewId,
            task.Interview != null ? $"{task.Interview.Type} Interview" : null,
            task.Title,
            task.Notes,
            task.DueAt,
            task.CompletedAt,
            task.Source,
            task.AutoRuleKey,
            task.CreatedAt
        );
    }
}
