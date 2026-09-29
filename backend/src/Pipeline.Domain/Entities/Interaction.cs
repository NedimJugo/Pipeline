using System;
using Pipeline.Domain.Common;
using Pipeline.Domain.Enums;

namespace Pipeline.Domain.Entities;

public class Interaction : BaseEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public Guid? ContactId { get; set; }
    public Contact? Contact { get; set; }

    public Guid? ApplicationId { get; set; }
    public Application? Application { get; set; }

    public InteractionChannel Channel { get; set; } = InteractionChannel.Email;
    public InteractionDirection Direction { get; set; } = InteractionDirection.Outbound;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string Summary { get; set; } = string.Empty;
    public string? SentContent { get; set; }
    public Guid? AttachmentDocumentId { get; set; }

    public bool FollowUpRequired { get; set; } = false;
    public DateTime? FollowUpDueAt { get; set; }
}
