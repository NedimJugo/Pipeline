using System;
using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Features.Tasks.DTOs;

namespace Pipeline.Application.Features.Tasks.Services;

public interface ITaskService
{
    Task<PagedTasksResult> GetTasksAsync(TaskFilterParams filter, CancellationToken ct = default);
    Task<TaskItemDto?> GetTaskByIdAsync(Guid id, CancellationToken ct = default);
    Task<TaskItemDto> CreateTaskAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task<TaskItemDto> UpdateTaskAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default);
    Task DeleteTaskAsync(Guid id, CancellationToken ct = default);
    Task<TaskItemDto> CompleteTaskAsync(Guid id, bool isCompleted, CancellationToken ct = default);
    Task<TaskItemDto> SnoozeTaskAsync(Guid id, int days, CancellationToken ct = default);
}
