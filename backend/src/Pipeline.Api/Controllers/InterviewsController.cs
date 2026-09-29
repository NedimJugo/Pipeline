using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Features.Interviews.DTOs;
using Pipeline.Application.Features.Interviews.Services;

namespace Pipeline.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InterviewsController : ControllerBase
{
    private readonly IInterviewService _interviewService;

    public InterviewsController(IInterviewService interviewService)
    {
        _interviewService = interviewService;
    }

    [HttpGet]
    public async Task<IActionResult> GetInterviews([FromQuery] InterviewFilterDto filter, CancellationToken ct)
    {
        var interviews = await _interviewService.GetInterviewsAsync(filter, ct);
        return Ok(interviews);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var interview = await _interviewService.GetByIdAsync(id, ct);
        return Ok(interview);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInterviewRequest request, CancellationToken ct)
    {
        var created = await _interviewService.CreateInterviewAsync(request, ct);
        return Ok(created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInterviewRequest request, CancellationToken ct)
    {
        var updated = await _interviewService.UpdateInterviewAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpPatch("{id:guid}/debrief")]
    public async Task<IActionResult> UpdateDebrief(Guid id, [FromBody] UpdateDebriefRequest request, CancellationToken ct)
    {
        var updated = await _interviewService.UpdateDebriefAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpPut("{id:guid}/checklist")]
    public async Task<IActionResult> UpdateChecklist(Guid id, [FromBody] UpdatePrepChecklistRequest request, CancellationToken ct)
    {
        var updated = await _interviewService.UpdatePrepChecklistAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/questions")]
    public async Task<IActionResult> AddQuestion(Guid id, [FromBody] AddQuestionRequest request, CancellationToken ct)
    {
        var created = await _interviewService.AddQuestionAsync(id, request, ct);
        return Ok(created);
    }

    [HttpDelete("{id:guid}/questions/{questionId:guid}")]
    public async Task<IActionResult> DeleteQuestion(Guid id, Guid questionId, CancellationToken ct)
    {
        await _interviewService.DeleteQuestionAsync(id, questionId, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _interviewService.DeleteInterviewAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/calendar.ics")]
    public async Task<IActionResult> DownloadIcs(Guid id, CancellationToken ct)
    {
        var (fileName, content) = await _interviewService.GenerateIcsAsync(id, ct);
        var bytes = Encoding.UTF8.GetBytes(content);
        return File(bytes, "text/calendar", fileName);
    }
}
