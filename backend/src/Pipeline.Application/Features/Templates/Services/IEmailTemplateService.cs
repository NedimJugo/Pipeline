using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pipeline.Application.Features.Templates.DTOs;
using Pipeline.Domain.Enums;

namespace Pipeline.Application.Features.Templates.Services;

public interface IEmailTemplateService
{
    Task<IReadOnlyList<EmailTemplateDto>> GetTemplatesAsync(EmailTemplateCategory? category = null, CancellationToken ct = default);
    Task<EmailTemplateDto?> GetTemplateByIdAsync(Guid id, CancellationToken ct = default);
    Task<EmailTemplateDto> CreateTemplateAsync(CreateEmailTemplateRequest request, CancellationToken ct = default);
    Task<EmailTemplateDto> UpdateTemplateAsync(Guid id, UpdateEmailTemplateRequest request, CancellationToken ct = default);
    Task DeleteTemplateAsync(Guid id, CancellationToken ct = default);
    Task<RenderedEmailTemplateDto> RenderTemplateAsync(Guid id, RenderEmailTemplateRequest request, CancellationToken ct = default);
    Task EnsureDefaultTemplatesSeededAsync(Guid userId, CancellationToken ct = default);
}
