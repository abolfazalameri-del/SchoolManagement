using SchoolManagement.Application.Authorization;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Shared;

namespace SchoolManagement.Application.Services;

public interface IAcademicsService
{
    Task<Result<SchoolClassDto>> CreateClassAsync(CreateSchoolClassRequest request, CancellationToken ct = default);
    Task<List<SchoolClassDto>> GetAllClassesAsync(CancellationToken ct = default);
    Task<Result<SubjectDto>> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken ct = default);
    Task<List<SubjectDto>> GetAllSubjectsAsync(CancellationToken ct = default);
    Task<Result> AssignTeacherAsync(AssignTeacherRequest request, CancellationToken ct = default);
    Task<Result<TimetableEntryDto>> AddTimetableEntryAsync(CreateTimetableEntryRequest request, CancellationToken ct = default);
    Task<List<TimetableEntryDto>> GetTimetableForClassAsync(int schoolClassId, CancellationToken ct = default);
}

public class AcademicsService : IAcademicsService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthorizationService _authz;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _audit;

    public AcademicsService(IUnitOfWork uow, IAuthorizationService authz, ICurrentUserContext currentUser, IAuditLogger audit)
    {
        _uow = uow; _authz = authz; _currentUser = currentUser; _audit = audit;
    }

    public async Task<Result<SchoolClassDto>> CreateClassAsync(CreateSchoolClassRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageClasses);
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure<SchoolClassDto>("نام صنف الزامی است.");

        var schoolClass = new SchoolClass
        {
            Name = request.Name, GradeLevel = request.GradeLevel, Section = request.Section,
            AcademicYear = request.AcademicYear, Capacity = request.Capacity,
            HomeroomTeacherId = request.HomeroomTeacherId, CreatedByUserId = _currentUser.UserId
        };
        await _uow.Repository<SchoolClass>().AddAsync(schoolClass, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "Created", nameof(SchoolClass), schoolClass.Id, schoolClass.Name, ct);

        string? teacherName = null;
        if (request.HomeroomTeacherId.HasValue)
        {
            var t = await _uow.Repository<Teacher>().GetByIdAsync(request.HomeroomTeacherId.Value, ct);
            teacherName = t?.FullName;
        }
        return Result.Success(ToDto(schoolClass, 0, teacherName));
    }

    public async Task<List<SchoolClassDto>> GetAllClassesAsync(CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewClasses);
        var classes = await _uow.Repository<SchoolClass>().GetAllAsync(ct);
        var students = await _uow.Repository<Student>().FindAsync(s => s.Status == EnrollmentStatus.Active, ct);
        var teachers = await _uow.Repository<Teacher>().GetAllAsync(ct);
        var teacherNames = teachers.ToDictionary(t => t.Id, t => t.FullName);

        return classes.Select(c => ToDto(
            c,
            students.Count(s => s.SchoolClassId == c.Id),
            c.HomeroomTeacherId.HasValue ? teacherNames.GetValueOrDefault(c.HomeroomTeacherId.Value) : null))
            .OrderBy(c => c.GradeLevel).ThenBy(c => c.Section)
            .ToList();
    }

    public async Task<Result<SubjectDto>> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageSubjects);
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure<SubjectDto>("نام مضمون الزامی است.");

        var subject = new Subject { Name = request.Name, Code = request.Code, IsActive = true, CreatedByUserId = _currentUser.UserId };
        await _uow.Repository<Subject>().AddAsync(subject, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(new SubjectDto(subject.Id, subject.Name, subject.Code, subject.IsActive));
    }

    public async Task<List<SubjectDto>> GetAllSubjectsAsync(CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewSubjects);
        var subjects = await _uow.Repository<Subject>().GetAllAsync(ct);
        return subjects.OrderBy(s => s.Name).Select(s => new SubjectDto(s.Id, s.Name, s.Code, s.IsActive)).ToList();
    }

    public async Task<Result> AssignTeacherAsync(AssignTeacherRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageClasses);

        var duplicate = await _uow.Repository<ClassSubjectTeacher>().FirstOrDefaultAsync(
            a => a.SchoolClassId == request.SchoolClassId && a.SubjectId == request.SubjectId && a.AcademicYear == request.AcademicYear, ct);
        if (duplicate is not null)
            return Result.Failure("این مضمون قبلاً برای این صنف در این سال تحصیلی تخصیص داده شده است.");

        await _uow.Repository<ClassSubjectTeacher>().AddAsync(new ClassSubjectTeacher
        {
            SchoolClassId = request.SchoolClassId, SubjectId = request.SubjectId,
            TeacherId = request.TeacherId, AcademicYear = request.AcademicYear,
            CreatedByUserId = _currentUser.UserId
        }, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<TimetableEntryDto>> AddTimetableEntryAsync(CreateTimetableEntryRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageTimetable);

        if (request.EndTime <= request.StartTime)
            return Result.Failure<TimetableEntryDto>("زمان پایان باید بعد از زمان شروع باشد.");

        // Clash checks: same class already has a period at this time, OR the teacher is already
        // teaching somewhere else at this time — both are real scheduling conflicts.
        var classEntries = await _uow.Repository<TimetableEntry>().FindAsync(
            e => e.SchoolClassId == request.SchoolClassId && e.DayOfWeek == request.DayOfWeek, ct);
        if (classEntries.Any(e => request.StartTime < e.EndTime && request.EndTime > e.StartTime))
            return Result.Failure<TimetableEntryDto>("این صنف در این بازه زمانی، درس دیگری دارد.");

        var teacherEntries = await _uow.Repository<TimetableEntry>().FindAsync(
            e => e.TeacherId == request.TeacherId && e.DayOfWeek == request.DayOfWeek, ct);
        if (teacherEntries.Any(e => request.StartTime < e.EndTime && request.EndTime > e.StartTime))
            return Result.Failure<TimetableEntryDto>("این معلم در این بازه زمانی، در صنف دیگری تدریس دارد.");

        var entry = new TimetableEntry
        {
            SchoolClassId = request.SchoolClassId, SubjectId = request.SubjectId, TeacherId = request.TeacherId,
            DayOfWeek = request.DayOfWeek, StartTime = request.StartTime, EndTime = request.EndTime,
            RoomName = request.RoomName, CreatedByUserId = _currentUser.UserId
        };
        await _uow.Repository<TimetableEntry>().AddAsync(entry, ct);
        await _uow.SaveChangesAsync(ct);

        var subject = await _uow.Repository<Subject>().GetByIdAsync(request.SubjectId, ct);
        var teacher = await _uow.Repository<Teacher>().GetByIdAsync(request.TeacherId, ct);
        return Result.Success(new TimetableEntryDto(entry.Id, entry.SchoolClassId, subject?.Name ?? "", teacher?.FullName ?? "", entry.DayOfWeek, entry.StartTime, entry.EndTime, entry.RoomName));
    }

    public async Task<List<TimetableEntryDto>> GetTimetableForClassAsync(int schoolClassId, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewTimetable);
        var entries = await _uow.Repository<TimetableEntry>().FindAsync(e => e.SchoolClassId == schoolClassId, ct);
        var subjects = (await _uow.Repository<Subject>().GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.Name);
        var teachers = (await _uow.Repository<Teacher>().GetAllAsync(ct)).ToDictionary(t => t.Id, t => t.FullName);

        return entries
            .OrderBy(e => e.DayOfWeek).ThenBy(e => e.StartTime)
            .Select(e => new TimetableEntryDto(e.Id, e.SchoolClassId, subjects.GetValueOrDefault(e.SubjectId, ""), teachers.GetValueOrDefault(e.TeacherId, ""), e.DayOfWeek, e.StartTime, e.EndTime, e.RoomName))
            .ToList();
    }

    private static SchoolClassDto ToDto(SchoolClass c, int enrolledCount, string? teacherName) =>
        new(c.Id, c.Name, c.GradeLevel, c.Section, c.AcademicYear, c.Capacity, enrolledCount, teacherName);
}
