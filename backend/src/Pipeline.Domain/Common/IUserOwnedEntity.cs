using System;

namespace Pipeline.Domain.Common;

public interface IUserOwnedEntity
{
    Guid UserId { get; set; }
}

public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
}
