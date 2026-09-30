using System;
using Microsoft.AspNetCore.Identity;
using Pipeline.Domain.Enums;

namespace Pipeline.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }
    public string? TargetRole { get; set; }
    public string? Seniority { get; set; }
    public string? Location { get; set; }
    public decimal? SalaryExpectationMin { get; set; }
    public decimal? SalaryExpectationMax { get; set; }
    public string Currency { get; set; } = "USD";
    public SearchStatus SearchStatus { get; set; } = SearchStatus.Active;
    public string Timezone { get; set; } = "UTC";
    public string NotificationPrefs { get; set; } = "{}";
    public int StaleAfterDays { get; set; } = 14;
    public bool OnboardingCompleted { get; set; } = false;
    public string CalendarFeedToken { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
