using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

/// <summary>
/// The managed registry of academic years (e.g. "1405", "1406"). Its Name is the exact string
/// every other module already stores in its own "AcademicYear" field (SchoolClass, Exam,
/// FeeStructure, PayrollRecord, ReportCard, ClassSubjectTeacher) — this entity does not replace
/// those fields, it gives the school a real place to create/close years and mark which one is
/// current, while everything else keeps working exactly as already built.
/// </summary>
public class AcademicYear : BaseEntity
{
    public string Name { get; set; } = string.Empty; // e.g. "1405"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; }
    /// <summary>A closed year can no longer be set current or have new records freely added against it — it is history.</summary>
    public bool IsClosed { get; set; }
}

/// <summary>
/// One row per student per academic year: which class they were in and how that year ended.
/// This is the real, permanent "سوابق تحصیلی" (academic history) — Student.SchoolClassId only
/// ever holds the CURRENT placement, this table is what still answers "where was this student
/// in 1403?" ten years later.
/// </summary>
public class StudentEnrollmentHistory : BaseEntity
{
    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public int AcademicYearId { get; set; }
    public AcademicYear? AcademicYear { get; set; }

    public int SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }

    public DateTime EnrolledDate { get; set; } = DateTime.UtcNow;

    /// <summary>Null while the year is still in progress; set when the student's outcome for this year is recorded (promotion time).</summary>
    public PromotionOutcome? Outcome { get; set; }
    public DateTime? OutcomeRecordedAtUtc { get; set; }
}
