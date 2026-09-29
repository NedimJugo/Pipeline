using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pipeline.Application.Features.Applications.DTOs;
using Pipeline.Application.Features.Applications.Services;
using Pipeline.Application.Features.Companies.Services;

namespace Pipeline.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IApplicationService _applicationService;
    private readonly ICompanyService _companyService;

    public SearchController(IApplicationService applicationService, ICompanyService companyService)
    {
        _applicationService = applicationService;
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new
            {
                applications = System.Array.Empty<ApplicationListItemDto>(),
                companies = System.Array.Empty<object>()
            });
        }

        var apps = await _applicationService.GetApplicationsAsync(new ApplicationFilterDto(Search: q), ct);
        var companies = await _companyService.SearchCompaniesAsync(q, ct);

        return Ok(new
        {
            applications = apps,
            companies
        });
    }
}
