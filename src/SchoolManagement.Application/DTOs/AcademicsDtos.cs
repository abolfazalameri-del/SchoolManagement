using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs;

public record TeacherDto(int Id, string FullName, Gender Gender, string? PhoneNumber, string? Qualification, decimal MonthlySalary, StaffEmploymentStatus Status);
public record CreateTeacherRequest(string FullName, Gender Gender, string? PhoneNumber, string? Address, string? Qualification, decimal MonthlySalary);
public record UpdateTeacherRequest(int Id, string FullName, string? PhoneNumber, string? Qualification, decimal MonthlySalary, StaffEmploymentStatus Status);

public record SchoolClassDto(int Id, string Name, int GradeLevel, string Section, string AcademicYear, int Capacity, int EnrolledCount, string? HomeroomTeacherName);
public record CreateSchoolClassRequest(string Name, int GradeLevel, string Section, string AcademicYear, int Capacity, int? HomeroomTeacherId);

public record SubjectDto(int Id, string Name, string Code, bool IsActive);
public record CreateSubjectRequest(string Name, string Code);

public record AssignTeacherRequest(int SchoolClassId, int SubjectId, int TeacherId, string AcademicYear);

public record TimetableEntryDto(int Id, int SchoolClassId, string SubjectName, string TeacherName, Weekday DayOfWeek, TimeSpan StartTime, TimeSpan EndTime, string? RoomName);
public record CreateTimetableEntryRequest(int SchoolClassId, int SubjectId, int TeacherId, Weekday DayOfWeek, TimeSpan StartTime, TimeSpan EndTime, string? RoomName);
