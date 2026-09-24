using SchoolManagement.Application.Authorization;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Shared;

namespace SchoolManagement.Application.Services;

public interface IPayrollService
{
    Task<Result<int>> GeneratePayrollAsync(GeneratePayrollRequest request, CancellationToken ct = default);
    Task<List<PayrollRowDto>> GetPayrollForMonthAsync(int month, int year, CancellationToken ct = default);
    Task<Result<PayrollRowDto>> AdjustAsync(AdjustPayrollRequest request, CancellationToken ct = default);
    Task<Result> MarkPaidAsync(MarkPayrollPaidRequest request, CancellationToken ct = default);
}

public class PayrollService : IPayrollService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthorizationService _authz;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _audit;

    public PayrollService(IUnitOfWork uow, IAuthorizationService authz, ICurrentUserContext currentUser, IAuditLogger audit)
    {
        _uow = uow; _authz = authz; _currentUser = currentUser; _audit = audit;
    }

    /// <summary>Creates one payroll record per active teacher for the month, seeded from their current MonthlySalary — safe to re-run, skips teachers who already have one.</summary>
    public async Task<Result<int>> GeneratePayrollAsync(GeneratePayrollRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManagePayroll);

        var teachers = await _uow.Repository<Teacher>().FindAsync(t => t.Status == StaffEmploymentStatus.Active, ct);
        var existing = await _uow.Repository<PayrollRecord>().FindAsync(p => p.Month == request.Month && p.Year == request.Year, ct);
        var alreadyHave = existing.Select(p => p.TeacherId).ToHashSet();

        int created = 0;
        foreach (var teacher in teachers.Where(t => !alreadyHave.Contains(t.Id)))
        {
            await _uow.Repository<PayrollRecord>().AddAsync(new PayrollRecord
            {
                TeacherId = teacher.Id, AcademicYear = request.AcademicYear, Month = request.Month, Year = request.Year,
                BaseSalary = teacher.MonthlySalary, Deductions = 0, Bonuses = 0, NetAmount = teacher.MonthlySalary,
                Status = PayrollStatus.Pending, CreatedByUserId = _currentUser.UserId
            }, ct);
            created++;
        }

        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "GeneratedPayroll", nameof(PayrollRecord), null, $"{request.Month}/{request.Year} - {created} معلم", ct);
        return Result.Success(created);
    }

    public async Task<List<PayrollRowDto>> GetPayrollForMonthAsync(int month, int year, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewPayroll);
        var records = await _uow.Repository<PayrollRecord>().FindAsync(p => p.Month == month && p.Year == year, ct);
        var teachers = (await _uow.Repository<Teacher>().GetAllAsync(ct)).ToDictionary(t => t.Id, t => t.FullName);
        return records.Select(p => ToDto(p, teachers.GetValueOrDefault(p.TeacherId, ""))).OrderBy(p => p.TeacherName).ToList();
    }

    public async Task<Result<PayrollRowDto>> AdjustAsync(AdjustPayrollRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManagePayroll);
        var record = await _uow.Repository<PayrollRecord>().GetByIdAsync(request.PayrollId, ct);
        if (record is null) return Result.Failure<PayrollRowDto>("رکورد معاش یافت نشد.");
        if (record.Status == PayrollStatus.Paid) return Result.Failure<PayrollRowDto>("معاش پرداخت‌شده قابل تغییر نیست.");

        record.Deductions = request.Deductions;
        record.Bonuses = request.Bonuses;
        record.NetAmount = record.BaseSalary - request.Deductions + request.Bonuses;
        record.Note = request.Note;
        record.ModifiedByUserId = _currentUser.UserId;
        _uow.Repository<PayrollRecord>().Update(record);
        await _uow.SaveChangesAsync(ct);

        var teacher = await _uow.Repository<Teacher>().GetByIdAsync(record.TeacherId, ct);
        return Result.Success(ToDto(record, teacher?.FullName ?? ""));
    }

    public async Task<Result> MarkPaidAsync(MarkPayrollPaidRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManagePayroll);
        var record = await _uow.Repository<PayrollRecord>().GetByIdAsync(request.PayrollId, ct);
        if (record is null) return Result.Failure("رکورد معاش یافت نشد.");
        if (record.Status == PayrollStatus.Paid) return Result.Failure("این معاش قبلاً پرداخت شده است.");

        record.Status = PayrollStatus.Paid;
        record.PaidAtUtc = DateTime.UtcNow;
        record.PaidByUserId = _currentUser.UserId;
        _uow.Repository<PayrollRecord>().Update(record);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "PaidPayroll", nameof(PayrollRecord), record.Id, record.NetAmount.ToString(), ct);
        return Result.Success();
    }

    private static PayrollRowDto ToDto(PayrollRecord p, string teacherName) =>
        new(p.Id, p.TeacherId, teacherName, p.Month, p.Year, p.BaseSalary, p.Deductions, p.Bonuses, p.NetAmount, p.Status);
}
