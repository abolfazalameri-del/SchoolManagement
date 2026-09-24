using SchoolManagement.Application.Authorization;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Shared;

namespace SchoolManagement.Application.Services;

public interface IExamService
{
    Task<Result<ExamDto>> CreateExamAsync(CreateExamRequest request, CancellationToken ct = default);
    Task<List<ExamDto>> GetExamsForClassAsync(int schoolClassId, string academicYear, CancellationToken ct = default);
    Task<Result> RecordResultsAsync(RecordExamResultsRequest request, CancellationToken ct = default);
    Task<List<ExamResultRowDto>> GetResultsAsync(int examId, CancellationToken ct = default);
    Task<Result<ReportCardDto>> GenerateReportCardAsync(GenerateReportCardRequest request, CancellationToken ct = default);
    Task<Result> FinalizeReportCardAsync(int reportCardId, CancellationToken ct = default);
}

public class ExamService : IExamService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthorizationService _authz;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _audit;

    public ExamService(IUnitOfWork uow, IAuthorizationService authz, ICurrentUserContext currentUser, IAuditLogger audit)
    {
        _uow = uow; _authz = authz; _currentUser = currentUser; _audit = audit;
    }

    public async Task<Result<ExamDto>> CreateExamAsync(CreateExamRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageExams);
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<ExamDto>("عنوان امتحان الزامی است.");
        if (request.MaxScore <= 0)
            return Result.Failure<ExamDto>("نمره کامل باید بزرگ‌تر از صفر باشد.");

        var exam = new Exam
        {
            Title = request.Title, Type = request.Type, AcademicYear = request.AcademicYear, Term = request.Term,
            ExamDate = request.ExamDate, SubjectId = request.SubjectId, SchoolClassId = request.SchoolClassId,
            MaxScore = request.MaxScore, PassingScore = request.PassingScore, CreatedByUserId = _currentUser.UserId
        };
        await _uow.Repository<Exam>().AddAsync(exam, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "Created", nameof(Exam), exam.Id, exam.Title, ct);

        var subject = await _uow.Repository<Subject>().GetByIdAsync(request.SubjectId, ct);
        return Result.Success(ToDto(exam, subject?.Name ?? ""));
    }

    public async Task<List<ExamDto>> GetExamsForClassAsync(int schoolClassId, string academicYear, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewExams);
        var exams = await _uow.Repository<Exam>().FindAsync(e => e.SchoolClassId == schoolClassId && e.AcademicYear == academicYear, ct);
        var subjects = (await _uow.Repository<Subject>().GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.Name);
        return exams.OrderByDescending(e => e.ExamDate).Select(e => ToDto(e, subjects.GetValueOrDefault(e.SubjectId, ""))).ToList();
    }

    public async Task<Result> RecordResultsAsync(RecordExamResultsRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.RecordGrades);
        var exam = await _uow.Repository<Exam>().GetByIdAsync(request.ExamId, ct);
        if (exam is null) return Result.Failure("امتحان یافت نشد.");

        foreach (var entry in request.Entries)
        {
            if (!entry.IsAbsent && (entry.ScoreObtained < 0 || entry.ScoreObtained > exam.MaxScore))
                return Result.Failure($"نمره شاگرد باید بین ۰ و {exam.MaxScore} باشد.");

            var existing = await _uow.Repository<ExamResult>().FirstOrDefaultAsync(
                r => r.ExamId == request.ExamId && r.StudentId == entry.StudentId, ct);

            if (existing is not null)
            {
                existing.ScoreObtained = entry.IsAbsent ? 0 : entry.ScoreObtained;
                existing.IsAbsent = entry.IsAbsent;
                existing.Remarks = entry.Remarks;
                existing.ModifiedByUserId = _currentUser.UserId;
                _uow.Repository<ExamResult>().Update(existing);
            }
            else
            {
                await _uow.Repository<ExamResult>().AddAsync(new ExamResult
                {
                    ExamId = request.ExamId, StudentId = entry.StudentId,
                    ScoreObtained = entry.IsAbsent ? 0 : entry.ScoreObtained,
                    IsAbsent = entry.IsAbsent, Remarks = entry.Remarks,
                    RecordedByUserId = _currentUser.UserId ?? 0, CreatedByUserId = _currentUser.UserId
                }, ct);
            }
        }

        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "RecordedGrades", nameof(ExamResult), request.ExamId, $"{request.Entries.Count} شاگرد", ct);
        return Result.Success();
    }

    public async Task<List<ExamResultRowDto>> GetResultsAsync(int examId, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewGrades);
        var exam = await _uow.Repository<Exam>().GetByIdAsync(examId, ct);
        if (exam is null) return new List<ExamResultRowDto>();

        var results = await _uow.Repository<ExamResult>().FindAsync(r => r.ExamId == examId, ct);
        var students = (await _uow.Repository<Student>().GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.FullName);

        return results.Select(r => new ExamResultRowDto(
            r.StudentId, students.GetValueOrDefault(r.StudentId, ""), r.ScoreObtained, r.IsAbsent,
            !r.IsAbsent && r.ScoreObtained >= exam.PassingScore))
            .OrderBy(r => r.StudentName)
            .ToList();
    }

    /// <summary>
    /// Aggregates every exam a student took in a term/subject into one average, ranks the student
    /// against classmates, and pulls in the term's attendance — this is the one calculation that
    /// justifies the whole exams+attendance data model existing.
    /// </summary>
    public async Task<Result<ReportCardDto>> GenerateReportCardAsync(GenerateReportCardRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.IssueReportCards);

        var student = await _uow.Repository<Student>().GetByIdAsync(request.StudentId, ct);
        if (student is null) return Result.Failure<ReportCardDto>("شاگرد یافت نشد.");

        var classmates = await _uow.Repository<Student>().FindAsync(
            s => s.SchoolClassId == student.SchoolClassId && s.Status == EnrollmentStatus.Active, ct);

        var classExams = await _uow.Repository<Exam>().FindAsync(
            e => e.SchoolClassId == student.SchoolClassId && e.AcademicYear == request.AcademicYear && e.Term == request.Term, ct);
        var examIds = classExams.Select(e => e.Id).ToHashSet();
        var subjectIds = classExams.Select(e => e.SubjectId).Distinct().ToList();
        var subjects = (await _uow.Repository<Subject>().GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.Name);

        var allResults = await _uow.Repository<ExamResult>().FindAsync(r => examIds.Contains(r.ExamId), ct);

        // Compute each classmate's overall average (percentage of max score across all their exams this term).
        var classmateAverages = new Dictionary<int, decimal>();
        foreach (var classmate in classmates)
        {
            var theirResults = allResults.Where(r => r.StudentId == classmate.Id && !r.IsAbsent).ToList();
            if (theirResults.Count == 0) { classmateAverages[classmate.Id] = 0; continue; }

            decimal sumPercent = 0;
            foreach (var r in theirResults)
            {
                var exam = classExams.First(e => e.Id == r.ExamId);
                sumPercent += exam.MaxScore == 0 ? 0 : (r.ScoreObtained / exam.MaxScore) * 100m;
            }
            classmateAverages[classmate.Id] = Math.Round(sumPercent / theirResults.Count, 2);
        }

        var studentAverage = classmateAverages.GetValueOrDefault(request.StudentId, 0);
        var rank = classmateAverages.Values.Count(avg => avg > studentAverage) + 1;

        // Per-subject line: average percentage converted back to a 0..100 display score.
        var subjectLines = new List<ReportCardSubjectLineDto>();
        var subjectLineEntities = new List<ReportCardSubjectLine>();
        foreach (var subjectId in subjectIds)
        {
            var subjectResults = allResults.Where(r => r.StudentId == request.StudentId && !r.IsAbsent &&
                classExams.First(e => e.Id == r.ExamId).SubjectId == subjectId).ToList();
            if (subjectResults.Count == 0) continue;

            decimal sumPercent = 0;
            foreach (var r in subjectResults)
            {
                var exam = classExams.First(e => e.Id == r.ExamId);
                sumPercent += exam.MaxScore == 0 ? 0 : (r.ScoreObtained / exam.MaxScore) * 100m;
            }
            var subjectAverage = Math.Round(sumPercent / subjectResults.Count, 2);
            subjectLines.Add(new ReportCardSubjectLineDto(subjects.GetValueOrDefault(subjectId, ""), subjectAverage));
            subjectLineEntities.Add(new ReportCardSubjectLine { SubjectId = subjectId, Score = subjectAverage, CreatedByUserId = _currentUser.UserId });
        }

        // Attendance for the same academic year (term-level date filtering is left to the caller
        // via the school's own term calendar; here we count everything recorded this academic year).
        var attendance = await _uow.Repository<StudentAttendanceRecord>().FindAsync(
            r => r.StudentId == request.StudentId && r.Date.Year.ToString() == request.AcademicYear, ct);
        int daysPresent = attendance.Count(a => a.Status is AttendanceStatus.Present or AttendanceStatus.Late);
        int daysAbsent = attendance.Count(a => a.Status == AttendanceStatus.Absent);

        var existingCard = await _uow.Repository<ReportCard>().FirstOrDefaultAsync(
            rc => rc.StudentId == request.StudentId && rc.AcademicYear == request.AcademicYear && rc.Term == request.Term, ct);

        if (existingCard is not null && existingCard.IsFinalized)
            return Result.Failure<ReportCardDto>("این کارنامه قبلاً نهایی شده و قابل تغییر نیست.");

        ReportCard card;
        if (existingCard is not null)
        {
            card = existingCard;
            card.OverallAverage = studentAverage;
            card.RankInClass = rank;
            card.TotalStudentsInClass = classmates.Count;
            card.DaysPresent = daysPresent;
            card.DaysAbsent = daysAbsent;
            card.ModifiedByUserId = _currentUser.UserId;
            _uow.Repository<ReportCard>().Update(card);

            var oldLines = await _uow.Repository<ReportCardSubjectLine>().FindAsync(l => l.ReportCardId == card.Id, ct);
            foreach (var old in oldLines) _uow.Repository<ReportCardSubjectLine>().SoftDelete(old, _currentUser.UserId ?? 0);
        }
        else
        {
            card = new ReportCard
            {
                StudentId = request.StudentId, AcademicYear = request.AcademicYear, Term = request.Term,
                OverallAverage = studentAverage, RankInClass = rank, TotalStudentsInClass = classmates.Count,
                DaysPresent = daysPresent, DaysAbsent = daysAbsent, CreatedByUserId = _currentUser.UserId
            };
            await _uow.Repository<ReportCard>().AddAsync(card, ct);
        }
        await _uow.SaveChangesAsync(ct); // ensure card.Id is assigned before attaching lines

        foreach (var line in subjectLineEntities)
        {
            line.ReportCardId = card.Id;
            await _uow.Repository<ReportCardSubjectLine>().AddAsync(line, ct);
        }
        await _uow.SaveChangesAsync(ct);

        return Result.Success(new ReportCardDto(
            card.Id, student.Id, student.FullName, card.AcademicYear, card.Term,
            card.OverallAverage, card.RankInClass, card.TotalStudentsInClass,
            card.DaysPresent, card.DaysAbsent, card.IsFinalized, subjectLines));
    }

    public async Task<Result> FinalizeReportCardAsync(int reportCardId, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.IssueReportCards);
        var card = await _uow.Repository<ReportCard>().GetByIdAsync(reportCardId, ct);
        if (card is null) return Result.Failure("کارنامه یافت نشد.");

        card.IsFinalized = true;
        card.IssuedAtUtc = DateTime.UtcNow;
        card.IssuedByUserId = _currentUser.UserId;
        _uow.Repository<ReportCard>().Update(card);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "Finalized", nameof(ReportCard), card.Id, null, ct);
        return Result.Success();
    }

    private static ExamDto ToDto(Exam e, string subjectName) =>
        new(e.Id, e.Title, e.Type, e.AcademicYear, e.Term, e.ExamDate, e.SubjectId, subjectName, e.SchoolClassId, e.MaxScore, e.PassingScore);
}
