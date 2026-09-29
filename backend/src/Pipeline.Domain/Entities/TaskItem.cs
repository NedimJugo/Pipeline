using System;
using System.Collections.Generic;
using Pipeline.Domain.Common;
using Pipeline.Domain.Enums;

namespace Pipeline.Domain.Entities;

public class TaskItem : BaseEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public Guid? ApplicationId { get; set; }
    public Application? Application { get; set; }

    public Guid? ContactId { get; set; }
    public Contact? Contact { get; set; }

    public Guid? InterviewId { get; set; }
    public Interview? Interview { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TaskSource Source { get; set; } = TaskSource.Manual;
    public string? AutoRuleKey { get; set; }

    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}

public class Reminder : BaseEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public Guid TaskId { get; set; }
    public TaskItem? Task { get; set; }

    public DateTime RemindAt { get; set; }
    public ReminderChannel Channel { get; set; } = ReminderChannel.Push;
    public DateTime? SentAt { get; set; }
}
