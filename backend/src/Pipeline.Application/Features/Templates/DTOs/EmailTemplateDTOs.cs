using System;
using Pipeline.Domain.Enums;

namespace Pipeline.Application.Features.Templates.DTOs;

public record EmailTemplateDto(
    Guid Id,
    string Name,
    string Subject,
    string Body,
    EmailTemplateCategory Category,
    bool IsSystem,
    DateTime CreatedAt);

public record CreateEmailTemplateRequest(
    string Name,
    string Subject,
    string Body,
    EmailTemplateCategory Category);

public record UpdateEmailTemplateRequest(
    string Name,
    string Subject,
    string Body,
    EmailTemplateCategory Category);

public record RenderEmailTemplateRequest(
    Guid? ApplicationId = null,
    Guid? ContactId = null);

public record RenderedEmailTemplateDto(
    string Subject,
    string Body);
