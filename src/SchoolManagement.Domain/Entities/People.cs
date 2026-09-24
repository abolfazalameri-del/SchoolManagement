using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

public class Student : BaseEntity
{
    public string StudentNumber { get; set; } = string.Empty; // school-assigned admission number
    public string FullName { get; set; } = string.Empty;
    public string? FatherName { get; set; }
    public Gender Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? NationalId { get; set; }
    public string? Address { get; set; }
    public string? PhotoPath { get; set; }
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;

    public int SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }

    public ICollection<StudentParent> StudentParents { get; set; } = new List<StudentParent>();
    public ICollection<StudentAttendanceRecord> AttendanceRecords { get; set; } = new List<StudentAttendanceRecord>();
    public ICollection<ExamResult> ExamResults { get; set; } = new List<ExamResult>();
    public ICollection<FeeInvoice> FeeInvoices { get; set; } = new List<FeeInvoice>();
    public ICollection<DocumentFile> Documents { get; set; } = new List<DocumentFile>();
}

public class Parent : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? Relationship { get; set; } // پدر، مادر، سرپرست
    public string? PhoneNumber { get; set; }
    public string? SecondaryPhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Occupation { get; set; }

    public ICollection<StudentParent> StudentParents { get; set; } = new List<StudentParent>();
}

/// <summary>Many-to-many link: a student can have multiple guardians, a guardian can have multiple children enrolled.</summary>
public class StudentParent : BaseEntity
{
    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public int ParentId { get; set; }
    public Parent? Parent { get; set; }

    public bool IsPrimaryContact { get; set; }
}

public class Teacher : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Qualification { get; set; }
    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public StaffEmploymentStatus Status { get; set; } = StaffEmploymentStatus.Active;
    public decimal MonthlySalary { get; set; }

    public int? UserId { get; set; } // optional linked login account
    public User? User { get; set; }

    public ICollection<ClassSubjectTeacher> Assignments { get; set; } = new List<ClassSubjectTeacher>();
    public ICollection<StaffAttendanceRecord> AttendanceRecords { get; set; } = new List<StaffAttendanceRecord>();
    public ICollection<PayrollRecord> PayrollRecords { get; set; } = new List<PayrollRecord>();
}

/// <summary>A login account. Every account has exactly one role that determines its Permission set.</summary>
public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public UserRoleType Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAtUtc { get; set; }
    public bool MustChangePasswordOnNextLogin { get; set; }
}
