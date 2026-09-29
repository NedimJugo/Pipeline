using System.Collections.Generic;
using Pipeline.Domain.Common;

namespace Pipeline.Domain.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? Industry { get; set; }
    public string? Size { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public string? LinkedInUrl { get; set; }

    public ICollection<Application> Applications { get; set; } = new List<Application>();
    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
}
