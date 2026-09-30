using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Common.Interfaces;
using Pipeline.Application.Features.Automation.Services;

namespace Pipeline.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AutomationController : ControllerBase
{
    private readonly IAutomationRuleEngine _automationEngine;
    private readonly ICurrentUserService _currentUserService;

    public AutomationController(
        IAutomationRuleEngine automationEngine,
        ICurrentUserService currentUserService)
    {
        _automationEngine = automationEngine;
        _currentUserService = currentUserService;
    }

    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate(CancellationToken ct)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;
        var created = await _automationEngine.EvaluateRulesForUserAsync(userId, ct);
        return Ok(new { evaluated = true, tasksCreated = created });
    }
}
