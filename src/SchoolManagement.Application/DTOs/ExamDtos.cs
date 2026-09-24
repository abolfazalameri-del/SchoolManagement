using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs;

public record ExamDto(int Id, string Title, ExamType Type, string AcademicYear, int Term, DateTime ExamDate, int SubjectId, string SubjectName, int SchoolClassId, decimal MaxScore, decimal PassingScore);
public record CreateExamRequest(string Title, ExamType Type, string AcademicYear, int Term, DateTime ExamDate, int SubjectId, int SchoolClassId, decimal MaxScore, decimal PassingScore);

public record ExamResultEntry(int StudentId, decimal ScoreObtained, bool IsAbsent, string? Remarks);
public record RecordExamResultsRequest(int ExamId, List<ExamResultEntry> Entries);
public record ExamResultRowDto(int StudentId, string StudentName, decimal ScoreObtained, bool IsAbsent, bool IsPassing);

public record GenerateReportCardRequest(int StudentId, string AcademicYear, int Term);
public record ReportCardSubjectLineDto(string SubjectName, decimal Score);
public record ReportCardDto(
    int Id, int StudentId, string StudentName, string AcademicYear, int Term,
    decimal OverallAverage, int RankInClass, int TotalStudentsInClass,
    int DaysPresent, int DaysAbsent, bool IsFinalized,
    List<ReportCardSubjectLineDto> SubjectLines);
