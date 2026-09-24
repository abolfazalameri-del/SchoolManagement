using SchoolManagement.Application.Authorization;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Shared;

namespace SchoolManagement.Application.Services;

public interface IAcademicYearService
{
    Task<Result<AcademicYearDto>> CreateAsync(CreateAcademicYearRequest request, CancellationToken ct = default);
    Task<Result> SetCurrentAsync(int academicYearId, CancellationToken ct = default);
    Task<Result> CloseAsync(int academicYearId, CancellationToken ct = default);
    Task<List<AcademicYearDto>> GetAllAsync(CancellationToken ct = default);
    Task<AcademicYearDto?> GetCurrentAsync(CancellationToken ct = default);

    /// <summary>
    /// Records how each student's outgoing year ended, and — for anyone staying enrolled —
    /// moves them into their new class for the new year. Safe to re-run for a student: it
    /// updates their existing history row for the outgoing year instead of duplicating it.
    /// </summary>
    Task<Result<int>> PromoteStudentsAsync(PromoteStudentsRequest request, CancellationToken ct = default);
    Task<List<StudentHistoryRowDto>> GetHistoryForStudentAsync(int studentId, CancellationToken ct = default);
}

public class AcademicYearService : IAcademicYearService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthorizationService _authz;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _audit;

    public AcademicYearService(IUnitOfWork uow, IAuthorizationService authz, ICurrentUserContext currentUser, IAuditLogger audit)
    {
        _uow = uow; _authz = authz; _currentUser = currentUser; _audit = audit;
    }

    public async Task<Result<AcademicYearDto>> CreateAsync(CreateAcademicYearRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageAcademicYears);
        if (string.IsNullOrWhiteSpace(request.Name)) return Result.Failure<AcademicYearDto>("نام سال تعلیمی الزامی است.");
        if (request.EndDate <= request.StartDate) return Result.Failure<AcademicYearDto>("تاریخ پایان باید بعد از تاریخ شروع باشد.");

        var duplicate = await _uow.Repository<AcademicYear>().FirstOrDefaultAsync(y => y.Name == request.Name, ct);
        if (duplicate is not null) return Result.Failure<AcademicYearDto>("این سال تعلیمی قبلاً ثبت شده است.");

        var year = new AcademicYear
        {
            Name = request.Name, StartDate = request.StartDate, EndDate = request.EndDate,
            IsCurrent = false, IsClosed = false, CreatedByUserId = _currentUser.UserId
        };
        await _uow.Repository<AcademicYear>().AddAsync(year, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "Created", nameof(AcademicYear), year.Id, year.Name, ct);

        return Result.Success(ToDto(year));
    }

    public async Task<Result> SetCurrentAsync(int academicYearId, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageAcademicYears);
        var year = await _uow.Repository<AcademicYear>().GetByIdAsync(academicYearId, ct);
        if (year is null) return Result.Failure("سال تعلیمی یافت نشد.");
        if (year.IsClosed) return Result.Failure("سال بسته‌شده نمی‌تواند فعال شود.");
        if (year.IsCurrent) return Result.Success(); // already current — nothing to do, not an error

        // Two ordered saves in one real transaction: the old current year(s) MUST be unset and
        // committed before the new one is set, or the partial unique index on IsCurrent=1 would
        // reject the new row while the old one is still marked current.
        await _uow.ExecuteInTransactionAsync(async () =>
        {
            var allCurrent = await _uow.Repository<AcademicYear>().FindAsync(y => y.IsCurrent, ct);
            foreach (var y in allCurrent)
            {
                y.IsCurrent = false;
                _uow.Repository<AcademicYear>().Update(y);
            }
            await _uow.SaveChangesAsync(ct);

            year.IsCurrent = true;
            _uow.Repository<AcademicYear>().Update(year);
            await _uow.SaveChangesAsync(ct);
        }, ct);

        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "SetCurrent", nameof(AcademicYear), year.Id, year.Name, ct);
        return Result.Success();
    }

    public async Task<Result> CloseAsync(int academicYearId, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageAcademicYears);
        var year = await _uow.Repository<AcademicYear>().GetByIdAsync(academicYearId, ct);
        if (year is null) return Result.Failure("سال تعلیمی یافت نشد.");
        if (year.IsCurrent) return Result.Failure("سال فعال را نمی‌توان بست — ابتدا سال دیگری را فعال کنید.");

        year.IsClosed = true;
        _uow.Repository<AcademicYear>().Update(year);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<List<AcademicYearDto>> GetAllAsync(CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewAcademicYears);
        var years = await _uow.Repository<AcademicYear>().GetAllAsync(ct);
        return years.OrderByDescending(y => y.StartDate).Select(ToDto).ToList();
    }

    public async Task<AcademicYearDto?> GetCurrentAsync(CancellationToken ct = default)
    {
        var year = await _uow.Repository<AcademicYear>().FirstOrDefaultAsync(y => y.IsCurrent, ct);
        return year is null ? null : ToDto(year);
    }

    public async Task<Result<int>> PromoteStudentsAsync(PromoteStudentsRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.PromoteStudents);

        var fromYear = await _uow.Repository<AcademicYear>().FirstOrDefaultAsync(y => y.Name == request.FromAcademicYearName, ct);
        if (fromYear is null) return Result.Failure<int>("سال تعلیمی مبدأ یافت نشد.");
        var toYear = await _uow.Repository<AcademicYear>().FirstOrDefaultAsync(y => y.Name == request.ToAcademicYearName, ct);
        if (toYear is null) return Result.Failure<int>("سال تعلیمی مقصد یافت نشد.");

        // Validate everything before touching the database — a mid-transaction throw would leak a
        // raw exception past the Result pattern this whole app relies on, so every entry must be
        // checked upfront and the transaction only opened once all of them are known-valid.
        foreach (var entry in request.Entries)
        {
            var staying = entry.Outcome is PromotionOutcome.Promoted or PromotionOutcome.Repeated;
            if (staying && entry.NewSchoolClassId is null)
                return Result.Failure<int>($"برای شاگرد #{entry.StudentId} صنف سال جدید مشخص نشده است.");
        }

        int processed = 0;

        // Explicit transaction: everything below either all lands together or none of it does.
        // (The loop already only stages changes via Add/Update — nothing hits the database until
        // the SaveChangesAsync calls inside — so wrapping it here is what makes that guarantee
        // real and visible, rather than relying on it being an accidental side effect of only
        // calling SaveChanges once.)
        await _uow.ExecuteInTransactionAsync(async () =>
        {
            foreach (var entry in request.Entries)
            {
            var student = await _uow.Repository<Student>().GetByIdAsync(entry.StudentId, ct);
            if (student is null) continue;

            var staying = entry.Outcome is PromotionOutcome.Promoted or PromotionOutcome.Repeated;

            // Close out the outgoing year's history row (create it now if it never existed).
            var outgoingHistory = await _uow.Repository<StudentEnrollmentHistory>().FirstOrDefaultAsync(
                h => h.StudentId == entry.StudentId && h.AcademicYearId == fromYear.Id, ct);
            if (outgoingHistory is null)
            {
                await _uow.Repository<StudentEnrollmentHistory>().AddAsync(new StudentEnrollmentHistory
                {
                    StudentId = entry.StudentId, AcademicYearId = fromYear.Id, SchoolClassId = student.SchoolClassId,
                    EnrolledDate = fromYear.StartDate, Outcome = entry.Outcome, OutcomeRecordedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = _currentUser.UserId
                }, ct);
            }
            else
            {
                outgoingHistory.Outcome = entry.Outcome;
                outgoingHistory.OutcomeRecordedAtUtc = DateTime.UtcNow;
                outgoingHistory.ModifiedByUserId = _currentUser.UserId;
                _uow.Repository<StudentEnrollmentHistory>().Update(outgoingHistory);
            }

            if (staying)
            {
                student.SchoolClassId = entry.NewSchoolClassId!.Value;
                student.Status = EnrollmentStatus.Active;
                student.ModifiedByUserId = _currentUser.UserId;
                _uow.Repository<Student>().Update(student);

                // Idempotency: re-running the same promotion must update the destination row,
                // never insert a second one (StudentId+AcademicYearId is a unique constraint).
                var destinationHistory = await _uow.Repository<StudentEnrollmentHistory>().FirstOrDefaultAsync(
                    h => h.StudentId == entry.StudentId && h.AcademicYearId == toYear.Id, ct);
                if (destinationHistory is null)
                {
                    await _uow.Repository<StudentEnrollmentHistory>().AddAsync(new StudentEnrollmentHistory
                    {
                        StudentId = entry.StudentId, AcademicYearId = toYear.Id, SchoolClassId = entry.NewSchoolClassId.Value,
                        EnrolledDate = toYear.StartDate, Outcome = null, CreatedByUserId = _currentUser.UserId
                    }, ct);
                }
                else
                {
                    destinationHistory.SchoolClassId = entry.NewSchoolClassId.Value;
                    destinationHistory.ModifiedByUserId = _currentUser.UserId;
                    _uow.Repository<StudentEnrollmentHistory>().Update(destinationHistory);
                }
            }
            else
            {
                student.Status = entry.Outcome switch
                {
                    PromotionOutcome.Graduated => EnrollmentStatus.Graduated,
                    PromotionOutcome.Withdrawn => EnrollmentStatus.Withdrawn,
                    PromotionOutcome.Transferred => EnrollmentStatus.Withdrawn,
                    _ => student.Status
                };
                student.ModifiedByUserId = _currentUser.UserId;
                _uow.Repository<Student>().Update(student);
            }

            processed++;
            }

            await _uow.SaveChangesAsync(ct);
        }, ct);

        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "PromotedStudents", nameof(StudentEnrollmentHistory), null, $"{fromYear.Name} -> {toYear.Name}: {processed} شاگرد", ct);
        return Result.Success(processed);
    }

    public async Task<List<StudentHistoryRowDto>> GetHistoryForStudentAsync(int studentId, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewStudents);
        var history = await _uow.Repository<StudentEnrollmentHistory>().FindAsync(h => h.StudentId == studentId, ct);
        var years = (await _uow.Repository<AcademicYear>().GetAllAsync(ct)).ToDictionary(y => y.Id, y => y.Name);
        var classes = (await _uow.Repository<SchoolClass>().GetAllAsync(ct)).ToDictionary(c => c.Id, c => c.Name);

        return history
            .OrderByDescending(h => h.EnrolledDate)
            .Select(h => new StudentHistoryRowDto(years.GetValueOrDefault(h.AcademicYearId, ""), classes.GetValueOrDefault(h.SchoolClassId, ""), h.Outcome))
            .ToList();
    }

    private static AcademicYearDto ToDto(AcademicYear y) => new(y.Id, y.Name, y.StartDate, y.EndDate, y.IsCurrent, y.IsClosed);
}
