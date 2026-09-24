using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs;

public record CreateAcademicYearRequest(string Name, DateTime StartDate, DateTime EndDate);
public record AcademicYearDto(int Id, string Name, DateTime StartDate, DateTime EndDate, bool IsCurrent, bool IsClosed);

public record PromoteStudentEntry(int StudentId, PromotionOutcome Outcome, int? NewSchoolClassId);
public record PromoteStudentsRequest(string FromAcademicYearName, string ToAcademicYearName, List<PromoteStudentEntry> Entries);

public record StudentHistoryRowDto(string AcademicYearName, string SchoolClassName, PromotionOutcome? Outcome);
