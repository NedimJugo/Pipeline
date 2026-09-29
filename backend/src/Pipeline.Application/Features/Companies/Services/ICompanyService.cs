using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Features.Companies.DTOs;

namespace Pipeline.Application.Features.Companies.Services;

public interface ICompanyService
{
    Task<IReadOnlyList<CompanyDto>> SearchCompaniesAsync(string? query, CancellationToken ct = default);
    Task<CompanyDto> GetOrCreateCompanyAsync(string name, string? website = null, CancellationToken ct = default);
    Task<CompanyDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CompanyDto> CreateAsync(CreateCompanyRequest request, CancellationToken ct = default);
    Task<CompanyDto> UpdateAsync(Guid id, UpdateCompanyRequest request, CancellationToken ct = default);
}
