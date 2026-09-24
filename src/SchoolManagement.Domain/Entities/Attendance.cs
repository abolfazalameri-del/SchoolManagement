using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

public class StudentAttendanceRecord : BaseEntity
{
    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public int SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }

    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Note { get; set; }

    public int RecordedByUserId { get; set; }
}

public class StaffAttendanceRecord : BaseEntity
{
    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public TimeSpan? CheckInTime { get; set; }
    public TimeSpan? CheckOutTime { get; set; }
    public string? Note { get; set; }

    public int RecordedByUserId { get; set; }
}
