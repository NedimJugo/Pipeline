using System;
using Pipeline.Domain.Common;
using Pipeline.Domain.Enums;

namespace Pipeline.Domain.Entities;

public class ApplicationStatusHistory : BaseEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public Guid ApplicationId { get; set; }
    public Application? Application { get; set; }

    public ApplicationStatus FromStatus { get; set; }
    public ApplicationStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}
