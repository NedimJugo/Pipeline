using System;
using System.Collections.Generic;
using Pipeline.Domain.Common;
using Pipeline.Domain.Enums;

namespace Pipeline.Domain.Entities;

public class Document : BaseEntity, IUserOwnedEntity, ISoftDeletable
{
    public Guid UserId { get; set; }
    public DocumentType Type { get; set; } = DocumentType.CV;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
}

public class DocumentVersion : BaseEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }

    public string VersionLabel { get; set; } = "v1";
    public string FileKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long SizeBytes { get; set; }
    public string? Notes { get; set; }
    public bool IsDefault { get; set; } = false;
}
