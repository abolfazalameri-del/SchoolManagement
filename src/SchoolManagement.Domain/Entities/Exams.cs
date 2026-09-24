using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

public class Exam : BaseEntity
{
    public string Title { get; set; } = string.Empty; // e.g. امتحان چهارربع اول
    public ExamType Type { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public int Term { get; set; } // 1..4 (چهارربع) or configurable
    public DateTime ExamDate { get; set; }

    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public int SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }

    public decimal MaxScore { get; set; } = 100;
    public decimal PassingScore { get; set; } = 40;

    public ICollection<ExamResult> Results { get; set; } = new List<ExamResult>();
}

public class ExamResult : BaseEntity
{
    public int ExamId { get; set; }
    public Exam? Exam { get; set; }

    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public decimal ScoreObtained { get; set; }
    public bool IsAbsent { get; set; }
    public string? Remarks { get; set; }

    public int RecordedByUserId { get; set; }
}

/// <summary>
/// A generated, snapshot document for one student for one term/year — built from ExamResults + attendance,
/// so a report card remains stable even if underlying exam records are later edited.
/// </summary>
public class ReportCard : BaseEntity
{
    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public string AcademicYear { get; set; } = string.Empty;
    public int Term { get; set; }

    public decimal OverallAverage { get; set; }
    public int RankInClass { get; set; }
    public int TotalStudentsInClass { get; set; }
    public int DaysPresent { get; set; }
    public int DaysAbsent { get; set; }
    public string? TeacherRemarks { get; set; }
    public bool IsFinalized { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
    public int? IssuedByUserId { get; set; }

    public ICollection<ReportCardSubjectLine> SubjectLines { get; set; } = new List<ReportCardSubjectLine>();
}

public class ReportCardSubjectLine : BaseEntity
{
    public int ReportCardId { get; set; }
    public ReportCard? ReportCard { get; set; }

    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public decimal Score { get; set; }
    public string? Grade { get; set; } // letter/rank label if used
}
