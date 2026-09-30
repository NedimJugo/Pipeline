using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Features.Tasks.DTOs;
using Pipeline.Application.Features.Tasks.Services;

namespace Pipeline.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks([FromQuery] TaskFilterParams filter, CancellationToken ct)
    {
        var result = await _taskService.GetTasksAsync(filter, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var task = await _taskService.GetTaskByIdAsync(id, ct);
        if (task == null) return NotFound();
        return Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var created = await _taskService.CreateTaskAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request, CancellationToken ct)
    {
        var updated = await _taskService.UpdateTaskAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _taskService.DeleteTaskAsync(id, ct);
        return NoContent();
    }

    public record CompleteTaskBody(bool IsCompleted = true);

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteTaskBody? body, CancellationToken ct)
    {
        var isCompleted = body?.IsCompleted ?? true;
        var updated = await _taskService.CompleteTaskAsync(id, isCompleted, ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/snooze")]
    public async Task<IActionResult> Snooze(Guid id, [FromBody] SnoozeTaskRequest request, CancellationToken ct)
    {
        var updated = await _taskService.SnoozeTaskAsync(id, request.Days, ct);
        return Ok(updated);
    }
}
