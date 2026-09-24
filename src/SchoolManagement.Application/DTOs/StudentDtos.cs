using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs;

public record StudentDto(
    int Id, string StudentNumber, string FullName, string? FatherName, Gender Gender,
    DateTime? DateOfBirth, string? Address, DateTime EnrollmentDate, EnrollmentStatus Status,
    int SchoolClassId, string SchoolClassName);

public record CreateStudentRequest(
    string StudentNumber, string FullName, string? FatherName, Gender Gender,
    DateTime? DateOfBirth, string? NationalId, string? Address, int SchoolClassId);

public record UpdateStudentRequest(
    int Id, string FullName, string? FatherName, Gender Gender,
    DateTime? DateOfBirth, string? Address, int SchoolClassId, EnrollmentStatus Status);

public record ParentDto(int Id, string FullName, string? Relationship, string? PhoneNumber, string? Address);

public record CreateParentRequest(string FullName, string? Relationship, string? PhoneNumber, string? SecondaryPhoneNumber, string? Address, string? Occupation);

public record LinkParentToStudentRequest(int StudentId, int ParentId, bool IsPrimaryContact);
