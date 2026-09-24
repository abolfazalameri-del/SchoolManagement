using SchoolManagement.Domain.Common;

namespace SchoolManagement.Domain.Entities;

/// <summary>
/// Immutable record of who did what and when. Never soft-deleted, never editable —
/// this is what makes the school's records trustworthy and forensically checkable.
/// </summary>
public class AuditLogEntry
{
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;      // e.g. "Created", "Updated", "Deleted", "LoggedIn"
    public string EntityName { get; set; } = string.Empty;  // e.g. "Student", "Payment"
    public int? EntityId { get; set; }
    public string? Details { get; set; }                    // JSON diff or human-readable summary
}

public class BackupRecord
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public bool WasAutomatic { get; set; }
    public int? CreatedByUserId { get; set; }
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>Single-row-per-key application settings (school name, academic year, currency, backup schedule, etc.).</summary>
public class Setting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
}
