using SchoolManagement.Application.Authorization;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Shared;

namespace SchoolManagement.Application.Services;

public interface ISettingsService
{
    Task<string?> GetValueAsync(string key, CancellationToken ct = default);
    Task<List<SettingDto>> GetAllAsync(CancellationToken ct = default);
    Task<Result> SetValueAsync(UpdateSettingRequest request, CancellationToken ct = default);
}

public class SettingsService : ISettingsService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthorizationService _authz;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _audit;

    public SettingsService(IUnitOfWork uow, IAuthorizationService authz, ICurrentUserContext currentUser, IAuditLogger audit)
    {
        _uow = uow; _authz = authz; _currentUser = currentUser; _audit = audit;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken ct = default)
    {
        var setting = await _uow.Repository<Setting>().FirstOrDefaultAsync(s => s.Key == key, ct);
        return setting?.Value;
    }

    public async Task<List<SettingDto>> GetAllAsync(CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageSettings);
        var settings = await _uow.Repository<Setting>().GetAllAsync(ct);
        return settings.Select(s => new SettingDto(s.Key, s.Value, s.Description)).ToList();
    }

    public async Task<Result> SetValueAsync(UpdateSettingRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageSettings);
        var setting = await _uow.Repository<Setting>().FirstOrDefaultAsync(s => s.Key == request.Key, ct);
        if (setting is null)
        {
            await _uow.Repository<Setting>().AddAsync(new Setting { Key = request.Key, Value = request.Value }, ct);
        }
        else
        {
            var oldValue = setting.Value;
            setting.Value = request.Value;
            _uow.Repository<Setting>().Update(setting);
            await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "ChangedSetting", nameof(Setting), setting.Id, $"{request.Key}: {oldValue} -> {request.Value}", ct);
        }
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public interface IAuditLogQueryService
{
    Task<List<AuditLogRowDto>> GetRecentAsync(int count, CancellationToken ct = default);
    Task<List<AuditLogRowDto>> GetForEntityAsync(string entityName, int entityId, CancellationToken ct = default);
}

public class AuditLogQueryService : IAuditLogQueryService
{
    private readonly IAuditLogReader _reader;
    private readonly IAuthorizationService _authz;

    public AuditLogQueryService(IAuditLogReader reader, IAuthorizationService authz)
    {
        _reader = reader; _authz = authz;
    }

    public async Task<List<AuditLogRowDto>> GetRecentAsync(int count, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewAuditLog);
        var entries = await _reader.GetRecentAsync(count, ct);
        return entries.Select(ToDto).ToList();
    }

    public async Task<List<AuditLogRowDto>> GetForEntityAsync(string entityName, int entityId, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewAuditLog);
        var entries = await _reader.GetForEntityAsync(entityName, entityId, ct);
        return entries.Select(ToDto).ToList();
    }

    private static AuditLogRowDto ToDto(AuditLogEntry e) => new(e.TimestampUtc, e.UserFullName, e.Action, e.EntityName, e.EntityId, e.Details);
}

/// <summary>Read-side port for the audit log, implemented in Infrastructure where the DbContext actually lives.</summary>
public interface IAuditLogReader
{
    Task<List<AuditLogEntry>> GetRecentAsync(int count, CancellationToken ct = default);
    Task<List<AuditLogEntry>> GetForEntityAsync(string entityName, int entityId, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);
}

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthorizationService _authz;

    public DashboardService(IUnitOfWork uow, IAuthorizationService authz)
    {
        _uow = uow; _authz = authz;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewReports);

        var activeStudents = await _uow.Repository<Student>().CountAsync(s => s.Status == EnrollmentStatus.Active, ct);
        var teachers = await _uow.Repository<Teacher>().CountAsync(t => t.Status == StaffEmploymentStatus.Active, ct);
        var classes = await _uow.Repository<SchoolClass>().CountAsync(ct: ct);

        var today = DateTime.UtcNow.Date;
        var todayAttendance = await _uow.Repository<StudentAttendanceRecord>().FindAsync(a => a.Date == today, ct);
        double attendanceRate = todayAttendance.Count == 0 ? 0 :
            Math.Round(todayAttendance.Count(a => a.Status is AttendanceStatus.Present or AttendanceStatus.Late) * 100.0 / todayAttendance.Count, 1);

        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthPayments = await _uow.Repository<Payment>().FindAsync(p => p.PaymentDate >= monthStart, ct);
        var feesThisMonth = monthPayments.Sum(p => p.AmountPaid);

        var unpaidInvoices = await _uow.Repository<FeeInvoice>().FindAsync(
            i => i.Status != FeeInvoiceStatus.Paid && i.Status != FeeInvoiceStatus.Waived, ct);
        var outstandingDebt = unpaidInvoices.Sum(i => i.AmountDue - i.AmountPaid);

        return new DashboardSummaryDto(activeStudents, teachers, classes, attendanceRate, feesThisMonth, outstandingDebt);
    }
}
