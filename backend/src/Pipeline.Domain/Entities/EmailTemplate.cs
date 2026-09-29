using System;
using Pipeline.Domain.Common;
using Pipeline.Domain.Enums;

namespace Pipeline.Domain.Entities;

public class EmailTemplate : BaseEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public EmailTemplateCategory Category { get; set; } = EmailTemplateCategory.FollowUp;
    public bool IsSystem { get; set; } = false;
}

public class PushSubscriptionEntity : BaseEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
}

public class CustomStage : BaseEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public int Order { get; set; }
    public string Label { get; set; } = string.Empty;
    public ApplicationStatus MapsToStatus { get; set; } = ApplicationStatus.Screening;
}
