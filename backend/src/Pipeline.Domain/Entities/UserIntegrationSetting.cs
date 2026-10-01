using System;
using Pipeline.Domain.Common;

namespace Pipeline.Domain.Entities;

public class UserIntegrationSetting : BaseEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    // SMTP Settings
    public bool UseCustomSmtp { get; set; } = false;
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public string? SmtpFrom { get; set; }

    // Google Settings
    public bool UseCustomGoogle { get; set; } = false;
    public string? GoogleClientId { get; set; }
    public string? GoogleClientSecret { get; set; }

    // Storage Provider Preference
    public string? StorageProvider { get; set; } // "Local" or "S3"
}
