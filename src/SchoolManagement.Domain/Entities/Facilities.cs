using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

public class Notice : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NoticeAudience Audience { get; set; }
    public int? TargetSchoolClassId { get; set; }
    public SchoolClass? TargetSchoolClass { get; set; }
    public DateTime PublishDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
}

/// <summary>A stored file (scan of a document, certificate, ID copy) attached to a student, staff member, or the school generally.</summary>
public class DocumentFile : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public DocumentOwnerType OwnerType { get; set; }
    public int? StudentId { get; set; }
    public Student? Student { get; set; }
    public int? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public string FilePath { get; set; } = string.Empty; // relative path under the app's data/documents folder
    public string? Category { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public int UploadedByUserId { get; set; }
}

public class Book : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Isbn { get; set; }
    public string? Category { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }

    public ICollection<LibraryLoan> Loans { get; set; } = new List<LibraryLoan>();
}

public class LibraryLoan : BaseEntity
{
    public int BookId { get; set; }
    public Book? Book { get; set; }

    public int? StudentId { get; set; }
    public Student? Student { get; set; }
    public int? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public DateTime BorrowedDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public DateTime? ReturnedDate { get; set; }
    public LibraryLoanStatus Status { get; set; } = LibraryLoanStatus.Borrowed;
}

public class Asset : BaseEntity
{
    public string Name { get; set; } = string.Empty; // میز، کمپیوتر، پرده و ...
    public string? Category { get; set; }
    public string? SerialNumber { get; set; }
    public string? Location { get; set; } // کدام صنف/دفتر
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseValue { get; set; }
    public AssetCondition Condition { get; set; } = AssetCondition.Good;
    public string? Note { get; set; }
}
