using SchoolManagement.Application.Authorization;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Shared;

namespace SchoolManagement.Application.Services;

public interface IAttendanceService
{
    Task<Result> RecordClassAttendanceAsync(RecordClassAttendanceRequest request, CancellationToken ct = default);
    Task<List<StudentAttendanceRowDto>> GetClassAttendanceForDateAsync(int schoolClassId, DateTime date, CancellationToken ct = default);
    Task<Result> RecordStaffAttendanceAsync(RecordStaffAttendanceRequest request, CancellationToken ct = default);
    Task<StudentAttendanceSummaryDto> GetStudentSummaryAsync(int studentId, DateTime from, DateTime to, CancellationToken ct = default);
}

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthorizationService _authz;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _audit;

    public AttendanceService(IUnitOfWork uow, IAuthorizationService authz, ICurrentUserContext currentUser, IAuditLogger audit)
    {
        _uow = uow; _authz = authz; _currentUser = currentUser; _audit = audit;
    }

    public async Task<Result> RecordClassAttendanceAsync(RecordClassAttendanceRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.RecordStudentAttendance);
        var date = request.Date.Date;

        foreach (var entry in request.Entries)
        {
            var existing = await _uow.Repository<StudentAttendanceRecord>().FirstOrDefaultAsync(
                r => r.StudentId == entry.StudentId && r.Date == date, ct);

            if (existing is not null)
            {
                existing.Status = entry.Status;
                existing.Note = entry.Note;
                existing.ModifiedByUserId = _currentUser.UserId;
                _uow.Repository<StudentAttendanceRecord>().Update(existing);
            }
            else
            {
                await _uow.Repository<StudentAttendanceRecord>().AddAsync(new StudentAttendanceRecord
                {
                    StudentId = entry.StudentId,
                    SchoolClassId = request.SchoolClassId,
                    Date = date,
                    Status = entry.Status,
                    Note = entry.Note,
                    RecordedByUserId = _currentUser.UserId ?? 0,
                    CreatedByUserId = _currentUser.UserId
                }, ct);
            }
        }

        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "RecordedAttendance", nameof(StudentAttendanceRecord), request.SchoolClassId, $"{date:yyyy-MM-dd} - {request.Entries.Count} شاگرد", ct);
        return Result.Success();
    }

    public async Task<List<StudentAttendanceRowDto>> GetClassAttendanceForDateAsync(int schoolClassId, DateTime date, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewStudentAttendance);
        var d = date.Date;

        var students = await _uow.Repository<Student>().FindAsync(s => s.SchoolClassId == schoolClassId && s.Status == EnrollmentStatus.Active, ct);
        var records = await _uow.Repository<StudentAttendanceRecord>().FindAsync(r => r.SchoolClassId == schoolClassId && r.Date == d, ct);
        var recordsByStudent = records.ToDictionary(r => r.StudentId, r => r);

        return students.OrderBy(s => s.FullName).Select(s =>
        {
            recordsByStudent.TryGetValue(s.Id, out var record);
            return new StudentAttendanceRowDto(s.Id, s.FullName, record?.Status ?? AttendanceStatus.Present, record?.Note);
        }).ToList();
    }

    public async Task<Result> RecordStaffAttendanceAsync(RecordStaffAttendanceRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.RecordStaffAttendance);
        var date = request.Date.Date;

        foreach (var entry in request.Entries)
        {
            var existing = await _uow.Repository<StaffAttendanceRecord>().FirstOrDefaultAsync(
                r => r.TeacherId == entry.TeacherId && r.Date == date, ct);

            if (existing is not null)
            {
                existing.Status = entry.Status;
                existing.CheckInTime = entry.CheckInTime;
                existing.CheckOutTime = entry.CheckOutTime;
                existing.Note = entry.Note;
                existing.ModifiedByUserId = _currentUser.UserId;
                _uow.Repository<StaffAttendanceRecord>().Update(existing);
            }
            else
            {
                await _uow.Repository<StaffAttendanceRecord>().AddAsync(new StaffAttendanceRecord
                {
                    TeacherId = entry.TeacherId,
                    Date = date,
                    Status = entry.Status,
                    CheckInTime = entry.CheckInTime,
                    CheckOutTime = entry.CheckOutTime,
                    Note = entry.Note,
                    RecordedByUserId = _currentUser.UserId ?? 0,
                    CreatedByUserId = _currentUser.UserId
                }, ct);
            }
        }

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<StudentAttendanceSummaryDto> GetStudentSummaryAsync(int studentId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewStudentAttendance);
        var records = await _uow.Repository<StudentAttendanceRecord>().FindAsync(
            r => r.StudentId == studentId && r.Date >= from.Date && r.Date <= to.Date, ct);

        int present = records.Count(r => r.Status == AttendanceStatus.Present);
        int absent = records.Count(r => r.Status == AttendanceStatus.Absent);
        int late = records.Count(r => r.Status == AttendanceStatus.Late);
        int excused = records.Count(r => r.Status == AttendanceStatus.Excused);
        int total = records.Count;
        double rate = total == 0 ? 0 : Math.Round((present + late) * 100.0 / total, 1);

        return new StudentAttendanceSummaryDto(studentId, present, absent, late, excused, rate);
    }
}
