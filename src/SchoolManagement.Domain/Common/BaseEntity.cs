namespace SchoolManagement.Domain.Common;

/// <summary>
/// Base class for all domain entities. Provides identity, audit trail and soft-delete support.
/// Every entity in the system inherits from this so history is never silently lost
/// (required for a school: attendance, grades and fee records must be auditable, never hard-deleted).
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }

    public DateTime? ModifiedAtUtc { get; set; }
    public int? ModifiedByUserId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public int? DeletedByUserId { get; set; }

    /// <summary>Optimistic concurrency token — prevents two users on the same LAN from silently overwriting each other's edits.</summary>
    public byte[]? RowVersion { get; set; }
}
