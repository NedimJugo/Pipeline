using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Features.Contacts.DTOs;
using Pipeline.Application.Features.Contacts.Services;
using Pipeline.Application.Features.Interactions.Services;

namespace Pipeline.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ContactsController : ControllerBase
{
    private readonly IContactService _contactService;
    private readonly IInteractionService _interactionService;

    public ContactsController(IContactService contactService, IInteractionService interactionService)
    {
        _contactService = contactService;
        _interactionService = interactionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetContacts([FromQuery] ContactFilterDto filter, CancellationToken ct)
    {
        var contacts = await _contactService.GetContactsAsync(filter, ct);
        return Ok(contacts);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var contact = await _contactService.GetByIdAsync(id, ct);
        return Ok(contact);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContactRequest request, CancellationToken ct)
    {
        var created = await _contactService.CreateContactAsync(request, ct);
        return Ok(created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContactRequest request, CancellationToken ct)
    {
        var updated = await _contactService.UpdateContactAsync(id, request, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _contactService.DeleteContactAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/interactions")]
    public async Task<IActionResult> GetInteractions(Guid id, CancellationToken ct)
    {
        var interactions = await _interactionService.GetInteractionsForContactAsync(id, ct);
        return Ok(interactions);
    }

    [HttpGet("{id:guid}/applications")]
    public async Task<IActionResult> GetApplications(Guid id, CancellationToken ct)
    {
        var applications = await _contactService.GetApplicationsForContactAsync(id, ct);
        return Ok(applications);
    }

    [HttpPost("{id:guid}/link-application")]
    public async Task<IActionResult> LinkApplication(Guid id, [FromBody] LinkApplicationContactRequest request, CancellationToken ct)
    {
        await _contactService.LinkApplicationContactAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/link-application/{applicationId:guid}")]
    public async Task<IActionResult> UnlinkApplication(Guid id, Guid applicationId, CancellationToken ct)
    {
        await _contactService.UnlinkApplicationContactAsync(id, applicationId, ct);
        return NoContent();
    }
}
