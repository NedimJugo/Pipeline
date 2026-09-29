using System;
using Pipeline.Domain.Common;

namespace Pipeline.Domain.Entities;

public class ApplicationContact : BaseEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public Guid ApplicationId { get; set; }
    public Application? Application { get; set; }

    public Guid ContactId { get; set; }
    public Contact? Contact { get; set; }

    public string? RoleInProcess { get; set; }
}
