using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Features.Applications.DTOs;
using Pipeline.Application.Features.Applications.Services;
using Pipeline.Application.Features.Contacts.DTOs;
using Pipeline.Application.Features.Contacts.Services;
using Pipeline.Application.Features.Interviews.DTOs;
using Pipeline.Application.Features.Interviews.Services;

namespace Pipeline.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;
    private readonly IContactService _contactService;
    private readonly IInterviewService _interviewService;

    public ApplicationsController(
        IApplicationService applicationService,
        IContactService contactService,
        IInterviewService interviewService)
    {
        _applicationService = applicationService;
        _contactService = contactService;
        _interviewService = interviewService;
    }

    [HttpGet]
    public async Task<IActionResult> GetApplications([FromQuery] ApplicationFilterDto filter, CancellationToken ct)
    {
        var applications = await _applicationService.GetApplicationsAsync(filter, ct);
        return Ok(applications);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var application = await _applicationService.GetByIdAsync(id, ct);
        return Ok(application);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApplicationRequest request, CancellationToken ct)
    {
        var created = await _applicationService.CreateApplicationAsync(request, ct);
        return Ok(created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateApplicationRequest request, CancellationToken ct)
    {
        var updated = await _applicationService.UpdateApplicationAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _applicationService.DeleteApplicationAsync(id, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var updated = await _applicationService.UpdateStatusAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpGet("{id:guid}/timeline")]
    public async Task<IActionResult> GetTimeline(Guid id, CancellationToken ct)
    {
        var timeline = await _applicationService.GetTimelineAsync(id, ct);
        return Ok(timeline);
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id, CancellationToken ct)
    {
        var duplicate = await _applicationService.DuplicateApplicationAsync(id, ct);
        return Ok(duplicate);
    }

    [HttpGet("{id:guid}/contacts")]
    public async Task<IActionResult> GetContacts(Guid id, CancellationToken ct)
    {
        var contacts = await _contactService.GetContactsForApplicationAsync(id, ct);
        return Ok(contacts);
    }

    [HttpPost("{id:guid}/contacts")]
    public async Task<IActionResult> LinkContact(Guid id, [FromBody] LinkContactToAppRequest request, CancellationToken ct)
    {
        await _contactService.LinkApplicationContactAsync(request.ContactId, new LinkApplicationContactRequest(id, request.RoleInProcess), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/contacts/{contactId:guid}")]
    public async Task<IActionResult> UnlinkContact(Guid id, Guid contactId, CancellationToken ct)
    {
        await _contactService.UnlinkApplicationContactAsync(contactId, id, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/interviews")]
    public async Task<IActionResult> GetInterviews(Guid id, CancellationToken ct)
    {
        var interviews = await _interviewService.GetInterviewsForApplicationAsync(id, ct);
        return Ok(interviews);
    }

    [HttpPost("{id:guid}/interviews")]
    public async Task<IActionResult> CreateInterview(Guid id, [FromBody] CreateInterviewRequest request, CancellationToken ct)
    {
        var created = await _interviewService.CreateInterviewAsync(request with { ApplicationId = id }, ct);
        return Ok(created);
    }
}
