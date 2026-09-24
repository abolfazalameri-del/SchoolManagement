using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

public class SchoolClass : BaseEntity
{
    public string Name { get; set; } = string.Empty; // e.g. صنف ششم - الف
    public int GradeLevel { get; set; }              // 1..12
    public string Section { get; set; } = string.Empty; // الف، ب، ج
    public string AcademicYear { get; set; } = string.Empty;
    public int Capacity { get; set; }

    public int? HomeroomTeacherId { get; set; }
    public Teacher? HomeroomTeacher { get; set; }

    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<ClassSubjectTeacher> Assignments { get; set; } = new List<ClassSubjectTeacher>();
    public ICollection<TimetableEntry> TimetableEntries { get; set; } = new List<TimetableEntry>();
    public ICollection<FeeInvoice> FeeInvoices { get; set; } = new List<FeeInvoice>();
}

public class Subject : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<ClassSubjectTeacher> Assignments { get; set; } = new List<ClassSubjectTeacher>();
}

/// <summary>Which teacher teaches which subject to which class — the backbone of timetable, attendance and grading scoping.</summary>
public class ClassSubjectTeacher : BaseEntity
{
    public int SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }

    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public string AcademicYear { get; set; } = string.Empty;
}

public class TimetableEntry : BaseEntity
{
    public int SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }

    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public Weekday DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? RoomName { get; set; }
}
