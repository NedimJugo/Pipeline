using System;
using System.Collections.Generic;
using Pipeline.Domain.Common;
using Pipeline.Domain.Enums;

namespace Pipeline.Domain.Entities;

public class Application : BaseEntity, IUserOwnedEntity, ISoftDeletable
{
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string RoleTitle { get; set; } = string.Empty;
    public string? JobUrl { get; set; }
    public ApplicationSource Source { get; set; } = ApplicationSource.LinkedIn;
    public string? SourceDetail { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Wishlist;
    public DateTime StatusChangedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AppliedAt { get; set; }
    public WorkMode WorkMode { get; set; } = WorkMode.Remote;
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    public string? Location { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string Currency { get; set; } = "USD";
    public string? JobDescription { get; set; }
    public string? Notes { get; set; }
    public string? Pros { get; set; }
    public string? Cons { get; set; }
    public int Priority { get; set; } = 2; // 1 = Low, 2 = Medium, 3 = High
    public bool Favorite { get; set; } = false;
    public int ExcitementRating { get; set; } = 3; // 1-5

    // Document links
    public Guid? DocumentVersionCvId { get; set; }
    public DocumentVersion? DocumentVersionCv { get; set; }
    public Guid? DocumentVersionCoverId { get; set; }
    public DocumentVersion? DocumentVersionCover { get; set; }

    // Closing details
    public string? ClosedReason { get; set; }
    public string? RejectionStage { get; set; }
    public string? LessonsLearned { get; set; }

    // Offer details
    public decimal? OfferSalary { get; set; }
    public string? OfferBenefits { get; set; }
    public DateTime? OfferDeadline { get; set; }

    // Aggregator placeholder
    public Guid? DiscoveredJobId { get; set; }

    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = new List<ApplicationStatusHistory>();
    public ICollection<ApplicationContact> ApplicationContacts { get; set; } = new List<ApplicationContact>();
    public ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();
    public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
    public ICollection<ApplicationReference> ApplicationReferences { get; set; } = new List<ApplicationReference>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
