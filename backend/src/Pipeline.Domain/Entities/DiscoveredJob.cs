using System;
using Pipeline.Domain.Common;

namespace Pipeline.Domain.Entities;

public class JobSource : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Api"; // Api, Rss, Scraper, Manual
    public string? BaseUrl { get; set; }
    public string Config { get; set; } = "{}"; // jsonb
    public bool Enabled { get; set; } = false;
    public DateTime? LastRunAt { get; set; }
}

public class DiscoveredJob : BaseEntity
{
    public Guid? SourceId { get; set; }
    public JobSource? Source { get; set; }

    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset? PostedAt { get; set; }
    public string Tags { get; set; } = "[]"; // json string or text array
    public string Hash { get; set; } = string.Empty; // unique hash of company + title + location
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}

public class UserDiscoveredJobState : BaseEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public Guid DiscoveredJobId { get; set; }
    public DiscoveredJob? DiscoveredJob { get; set; }
    public string State { get; set; } = "New"; // New, Saved, Dismissed
}
