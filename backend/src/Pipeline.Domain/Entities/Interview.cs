using System;
using System.Collections.Generic;
using Pipeline.Domain.Common;
using Pipeline.Domain.Enums;

namespace Pipeline.Domain.Entities;

public class Interview : BaseEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public Guid ApplicationId { get; set; }
    public Application? Application { get; set; }

    public InterviewType Type { get; set; } = InterviewType.Technical;
    public InterviewFormat Format { get; set; } = InterviewFormat.Video;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 45;
    public string? Location { get; set; }
    public string? MeetingLink { get; set; }
    public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;

    public string? PrepNotes { get; set; }
    public string PrepChecklist { get; set; } = "[]"; // json list of { text, done }

    public int? SelfRating { get; set; } // 1-5
    public string? WentWell { get; set; }
    public string? ToImprove { get; set; }
    public bool ThankYouSent { get; set; } = false;
    public string? OutcomeNotes { get; set; }

    public ICollection<InterviewContact> InterviewContacts { get; set; } = new List<InterviewContact>();
    public ICollection<InterviewQuestion> Questions { get; set; } = new List<InterviewQuestion>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}

public class InterviewContact : BaseEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public Guid InterviewId { get; set; }
    public Interview? Interview { get; set; }

    public Guid ContactId { get; set; }
    public Contact? Contact { get; set; }
}

public class InterviewQuestion : BaseEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public Guid InterviewId { get; set; }
    public Interview? Interview { get; set; }

    public string Question { get; set; } = string.Empty;
    public string? MyAnswer { get; set; }
    public InterviewQuestionCategory Category { get; set; } = InterviewQuestionCategory.Behavioral;
    public int Difficulty { get; set; } = 3; // 1-5
    public bool WasPrepared { get; set; } = true;
}
