using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs;

public record StudentAttendanceEntry(int StudentId, AttendanceStatus Status, string? Note);
public record RecordClassAttendanceRequest(int SchoolClassId, DateTime Date, List<StudentAttendanceEntry> Entries);
public record StudentAttendanceRowDto(int StudentId, string StudentName, AttendanceStatus Status, string? Note);

public record StaffAttendanceEntry(int TeacherId, AttendanceStatus Status, TimeSpan? CheckInTime, TimeSpan? CheckOutTime, string? Note);
public record RecordStaffAttendanceRequest(DateTime Date, List<StaffAttendanceEntry> Entries);

public record StudentAttendanceSummaryDto(int StudentId, int DaysPresent, int DaysAbsent, int DaysLate, int DaysExcused, double AttendanceRatePercent);
