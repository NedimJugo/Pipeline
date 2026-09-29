using System;
using System.Collections.Generic;
using Pipeline.Domain.Common;
using Pipeline.Domain.Enums;

namespace Pipeline.Domain.Entities;

public class JobReference : BaseEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public ReferenceConsent Consent { get; set; } = ReferenceConsent.NotAsked;
    public string? Notes { get; set; }
    public DateTime? LastNotifiedAt { get; set; }

    public ICollection<ApplicationReference> ApplicationReferences { get; set; } = new List<ApplicationReference>();
}

public class ApplicationReference : BaseEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public Guid ApplicationId { get; set; }
    public Application? Application { get; set; }

    public Guid ReferenceId { get; set; }
    public JobReference? Reference { get; set; }

    public DateTime SharedAt { get; set; } = DateTime.UtcNow;
    public string? Outcome { get; set; }
}
