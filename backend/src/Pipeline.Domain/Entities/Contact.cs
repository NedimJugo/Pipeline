using System;
using System.Collections.Generic;
using Pipeline.Domain.Common;
using Pipeline.Domain.Enums;

namespace Pipeline.Domain.Entities;

public class Contact : BaseEntity, IUserOwnedEntity, ISoftDeletable
{
    public Guid UserId { get; set; }
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LinkedInUrl { get; set; }
    public ContactType Type { get; set; } = ContactType.Recruiter;
    public string? Notes { get; set; }
    public DateTime? LastContactedAt { get; set; }
    public DateTime? NextFollowUpAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<ApplicationContact> ApplicationContacts { get; set; } = new List<ApplicationContact>();
    public ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();
    public ICollection<InterviewContact> InterviewContacts { get; set; } = new List<InterviewContact>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
