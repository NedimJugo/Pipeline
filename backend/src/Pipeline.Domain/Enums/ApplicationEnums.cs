namespace Pipeline.Domain.Enums;

public enum ApplicationStatus
{
    Wishlist,
    Applied,
    Screening,
    Interview,
    Assignment,
    Offer,
    Accepted,
    Rejected,
    Withdrawn,
    Ghosted,
    Declined
}

public enum WorkMode
{
    Onsite,
    Hybrid,
    Remote
}

public enum EmploymentType
{
    FullTime,
    PartTime,
    Contract,
    Internship,
    Freelance
}

public enum ApplicationSource
{
    LinkedIn,
    CompanyWebsite,
    Referral,
    Recruiter,
    JobBoard,
    Event,
    Other
}

public enum InterviewType
{
    HR,
    Technical,
    Culture,
    Manager,
    Final,
    Assignment,
    Other
}

public enum InterviewFormat
{
    Phone,
    Video,
    Onsite
}

public enum InterviewStatus
{
    Scheduled,
    Completed,
    Cancelled,
    NoShow
}

public enum InterviewQuestionCategory
{
    Behavioral,
    Technical,
    Situational,
    Salary,
    Other
}

public enum InteractionChannel
{
    Email,
    LinkedIn,
    Phone,
    Video,
    InPerson,
    Message,
    Other
}

public enum InteractionDirection
{
    Inbound,
    Outbound
}

public enum DocumentType
{
    CV,
    CoverLetter,
    Portfolio,
    Certificate,
    Other
}

public enum ReferenceConsent
{
    NotAsked,
    Asked,
    Agreed,
    Declined
}

public enum ContactType
{
    Recruiter,
    HiringManager,
    Interviewer,
    Referrer,
    Peer,
    Other
}

public enum ContactWarmth
{
    Hot,
    Warm,
    Cooling,
    Cold
}

public enum TaskSource
{
    Manual,
    Auto
}

public enum ReminderChannel
{
    Push,
    Email
}

public enum EmailTemplateCategory
{
    FollowUp,
    ThankYou,
    Negotiation,
    Withdraw,
    Other
}

public enum SearchStatus
{
    Active,
    Passive,
    Paused
}
