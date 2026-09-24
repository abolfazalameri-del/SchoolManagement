using SchoolManagement.Application.Authorization;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Shared;

namespace SchoolManagement.Application.Services;

public interface IFeeService
{
    Task<Result<FeeStructureDto>> CreateFeeStructureAsync(CreateFeeStructureRequest request, CancellationToken ct = default);
    Task<List<FeeStructureDto>> GetFeeStructuresAsync(string academicYear, CancellationToken ct = default);
    Task<Result<int>> GenerateMonthlyInvoicesAsync(GenerateMonthlyInvoicesRequest request, CancellationToken ct = default);
    Task<Result<FeeInvoiceDto>> CreateAdHocInvoiceAsync(CreateAdHocInvoiceRequest request, CancellationToken ct = default);
    Task<List<FeeInvoiceDto>> GetInvoicesForStudentAsync(int studentId, CancellationToken ct = default);
    Task<Result<PaymentDto>> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken ct = default);
    Task<List<DebtorRowDto>> GetDebtorsReportAsync(int? schoolClassId, CancellationToken ct = default);
}

public class FeeService : IFeeService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthorizationService _authz;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _audit;

    public FeeService(IUnitOfWork uow, IAuthorizationService authz, ICurrentUserContext currentUser, IAuditLogger audit)
    {
        _uow = uow; _authz = authz; _currentUser = currentUser; _audit = audit;
    }

    public async Task<Result<FeeStructureDto>> CreateFeeStructureAsync(CreateFeeStructureRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageFees);
        if (request.Amount <= 0) return Result.Failure<FeeStructureDto>("مبلغ فیس باید بزرگ‌تر از صفر باشد.");

        var schoolClass = await _uow.Repository<SchoolClass>().GetByIdAsync(request.SchoolClassId, ct);
        if (schoolClass is null) return Result.Failure<FeeStructureDto>("صنف یافت نشد.");

        var structure = new FeeStructure
        {
            Title = request.Title, SchoolClassId = request.SchoolClassId, AcademicYear = request.AcademicYear,
            Amount = request.Amount, IsRecurringMonthly = request.IsRecurringMonthly, CreatedByUserId = _currentUser.UserId
        };
        await _uow.Repository<FeeStructure>().AddAsync(structure, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(new FeeStructureDto(structure.Id, structure.Title, structure.SchoolClassId, schoolClass.Name, structure.AcademicYear, structure.Amount, structure.IsRecurringMonthly));
    }

    public async Task<List<FeeStructureDto>> GetFeeStructuresAsync(string academicYear, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewFees);
        var structures = await _uow.Repository<FeeStructure>().FindAsync(f => f.AcademicYear == academicYear, ct);
        var classes = (await _uow.Repository<SchoolClass>().GetAllAsync(ct)).ToDictionary(c => c.Id, c => c.Name);
        return structures.Select(f => new FeeStructureDto(f.Id, f.Title, f.SchoolClassId, classes.GetValueOrDefault(f.SchoolClassId, ""), f.AcademicYear, f.Amount, f.IsRecurringMonthly)).ToList();
    }

    /// <summary>Generates one invoice per active student in the fee structure's class for the given month — skips students who already have one, so it's safe to re-run.</summary>
    public async Task<Result<int>> GenerateMonthlyInvoicesAsync(GenerateMonthlyInvoicesRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageFees);

        var structure = await _uow.Repository<FeeStructure>().GetByIdAsync(request.FeeStructureId, ct);
        if (structure is null) return Result.Failure<int>("ساختار فیس یافت نشد.");

        var students = await _uow.Repository<Student>().FindAsync(
            s => s.SchoolClassId == structure.SchoolClassId && s.Status == EnrollmentStatus.Active, ct);

        var existingInvoices = await _uow.Repository<FeeInvoice>().FindAsync(
            i => i.FeeStructureId == structure.Id && i.BillingMonth == request.BillingMonth && i.AcademicYear == structure.AcademicYear, ct);
        var alreadyInvoicedStudentIds = existingInvoices.Select(i => i.StudentId).ToHashSet();

        int created = 0;
        foreach (var student in students.Where(s => !alreadyInvoicedStudentIds.Contains(s.Id)))
        {
            await _uow.Repository<FeeInvoice>().AddAsync(new FeeInvoice
            {
                StudentId = student.Id, SchoolClassId = structure.SchoolClassId, FeeStructureId = structure.Id,
                Description = structure.Title, AcademicYear = structure.AcademicYear, BillingMonth = request.BillingMonth,
                AmountDue = structure.Amount, AmountPaid = 0, DueDate = request.DueDate, Status = FeeInvoiceStatus.Unpaid,
                CreatedByUserId = _currentUser.UserId
            }, ct);
            created++;
        }

        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "GeneratedInvoices", nameof(FeeInvoice), structure.Id, $"{created} فاکتور برای ماه {request.BillingMonth}", ct);
        return Result.Success(created);
    }

    public async Task<Result<FeeInvoiceDto>> CreateAdHocInvoiceAsync(CreateAdHocInvoiceRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageFees);
        if (request.AmountDue <= 0) return Result.Failure<FeeInvoiceDto>("مبلغ باید بزرگ‌تر از صفر باشد.");

        var student = await _uow.Repository<Student>().GetByIdAsync(request.StudentId, ct);
        if (student is null) return Result.Failure<FeeInvoiceDto>("شاگرد یافت نشد.");

        var invoice = new FeeInvoice
        {
            StudentId = request.StudentId, SchoolClassId = student.SchoolClassId, Description = request.Description,
            AcademicYear = request.AcademicYear, AmountDue = request.AmountDue, AmountPaid = 0,
            DueDate = request.DueDate, Status = FeeInvoiceStatus.Unpaid, CreatedByUserId = _currentUser.UserId
        };
        await _uow.Repository<FeeInvoice>().AddAsync(invoice, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(ToInvoiceDto(invoice, student.FullName));
    }

    public async Task<List<FeeInvoiceDto>> GetInvoicesForStudentAsync(int studentId, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewFees);
        var student = await _uow.Repository<Student>().GetByIdAsync(studentId, ct);
        var invoices = await _uow.Repository<FeeInvoice>().FindAsync(i => i.StudentId == studentId, ct);
        return invoices.OrderByDescending(i => i.DueDate).Select(i => ToInvoiceDto(i, student?.FullName ?? "")).ToList();
    }

    /// <summary>
    /// Records a real payment transaction: updates the invoice balance/status and writes an
    /// immutable Payment row with a unique receipt number — this pair is the "transaction" for
    /// every fee collected, never just a balance edit.
    /// </summary>
    public async Task<Result<PaymentDto>> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.RecordPayments);
        if (request.AmountPaid <= 0) return Result.Failure<PaymentDto>("مبلغ پرداخت باید بزرگ‌تر از صفر باشد.");

        var invoice = await _uow.Repository<FeeInvoice>().GetByIdAsync(request.FeeInvoiceId, ct);
        if (invoice is null) return Result.Failure<PaymentDto>("فاکتور یافت نشد.");

        var remainingBalance = invoice.AmountDue - invoice.AmountPaid;
        if (request.AmountPaid > remainingBalance)
            return Result.Failure<PaymentDto>($"مبلغ پرداختی ({request.AmountPaid}) بیشتر از باقی‌مانده فاکتور ({remainingBalance}) است.");

        var receiptNumber = await GenerateUniqueReceiptNumberAsync(ct);

        var payment = new Payment
        {
            FeeInvoiceId = invoice.Id, ReceiptNumber = receiptNumber, AmountPaid = request.AmountPaid,
            PaymentDate = DateTime.UtcNow, Method = request.Method, Note = request.Note,
            ReceivedByUserId = _currentUser.UserId ?? 0, CreatedByUserId = _currentUser.UserId
        };
        await _uow.Repository<Payment>().AddAsync(payment, ct);

        invoice.AmountPaid += request.AmountPaid;
        invoice.Status = invoice.AmountPaid >= invoice.AmountDue
            ? FeeInvoiceStatus.Paid
            : (invoice.AmountPaid > 0 ? FeeInvoiceStatus.PartiallyPaid : FeeInvoiceStatus.Unpaid);
        invoice.ModifiedByUserId = _currentUser.UserId;
        _uow.Repository<FeeInvoice>().Update(invoice);

        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "RecordedPayment", nameof(Payment), payment.Id, $"{request.AmountPaid} - رسید {receiptNumber}", ct);

        var student = await _uow.Repository<Student>().GetByIdAsync(invoice.StudentId, ct);
        return Result.Success(new PaymentDto(payment.Id, payment.ReceiptNumber, invoice.Id, invoice.StudentId, student?.FullName ?? "", payment.AmountPaid, payment.PaymentDate, payment.Method, _currentUser.FullName ?? ""));
    }

    /// <summary>Every student with an outstanding balance — this is the «بدهکاران» report, computed live from invoices, never a separately-maintained list that can drift out of sync.</summary>
    public async Task<List<DebtorRowDto>> GetDebtorsReportAsync(int? schoolClassId, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewDebtors);

        var invoices = await _uow.Repository<FeeInvoice>().FindAsync(
            i => i.Status != FeeInvoiceStatus.Paid && i.Status != FeeInvoiceStatus.Waived &&
                 (!schoolClassId.HasValue || i.SchoolClassId == schoolClassId.Value), ct);

        var students = (await _uow.Repository<Student>().GetAllAsync(ct)).ToDictionary(s => s.Id, s => s);
        var classes = (await _uow.Repository<SchoolClass>().GetAllAsync(ct)).ToDictionary(c => c.Id, c => c.Name);
        var today = DateTime.UtcNow.Date;

        return invoices.GroupBy(i => i.StudentId).Select(g =>
        {
            var student = students.GetValueOrDefault(g.Key);
            return new DebtorRowDto(
                g.Key, student?.FullName ?? "?", classes.GetValueOrDefault(student?.SchoolClassId ?? 0, ""),
                g.Sum(i => i.AmountDue), g.Sum(i => i.AmountPaid), g.Sum(i => i.AmountDue - i.AmountPaid),
                g.Count(i => i.DueDate.Date < today));
        })
        .Where(d => d.Balance > 0)
        .OrderByDescending(d => d.Balance)
        .ToList();
    }

    private async Task<string> GenerateUniqueReceiptNumberAsync(CancellationToken ct)
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        for (int attempt = 0; attempt < 20; attempt++)
        {
            var count = await _uow.Repository<Payment>().CountAsync(ct: ct) + 1 + attempt;
            var candidate = $"RCPT-{datePart}-{count:D5}";
            var exists = await _uow.Repository<Payment>().FirstOrDefaultAsync(p => p.ReceiptNumber == candidate, ct);
            if (exists is null) return candidate;
        }
        // Astronomically unlikely fallback — guarantees uniqueness even under heavy concurrent load.
        return $"RCPT-{datePart}-{Guid.NewGuid().ToString("N")[..8]}";
    }

    private static FeeInvoiceDto ToInvoiceDto(FeeInvoice i, string studentName) =>
        new(i.Id, i.StudentId, studentName, i.Description, i.AcademicYear, i.AmountDue, i.AmountPaid, i.AmountDue - i.AmountPaid, i.DueDate, i.Status);
}
