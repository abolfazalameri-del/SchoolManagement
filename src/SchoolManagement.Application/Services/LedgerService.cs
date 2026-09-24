using SchoolManagement.Application.Authorization;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Shared;

namespace SchoolManagement.Application.Services;

public interface ILedgerService
{
    Task<Result<ExpenseDto>> RecordExpenseAsync(CreateExpenseRequest request, CancellationToken ct = default);
    Task<List<ExpenseDto>> GetExpensesAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<Result<IncomeDto>> RecordIncomeAsync(CreateIncomeRequest request, CancellationToken ct = default);
    Task<List<IncomeDto>> GetIncomesAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<FinancialSummaryDto> GetFinancialSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

public class LedgerService : ILedgerService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthorizationService _authz;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _audit;

    public LedgerService(IUnitOfWork uow, IAuthorizationService authz, ICurrentUserContext currentUser, IAuditLogger audit)
    {
        _uow = uow; _authz = authz; _currentUser = currentUser; _audit = audit;
    }

    public async Task<Result<ExpenseDto>> RecordExpenseAsync(CreateExpenseRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageExpenses);
        if (request.Amount <= 0) return Result.Failure<ExpenseDto>("مبلغ مصرف باید بزرگ‌تر از صفر باشد.");

        var expense = new Expense
        {
            Category = request.Category, Description = request.Description, Amount = request.Amount,
            Date = request.Date, ReceiptReference = request.ReceiptReference,
            RecordedByUserId = _currentUser.UserId ?? 0, CreatedByUserId = _currentUser.UserId
        };
        await _uow.Repository<Expense>().AddAsync(expense, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "Created", nameof(Expense), expense.Id, $"{expense.Category} - {expense.Amount}", ct);
        return Result.Success(new ExpenseDto(expense.Id, expense.Category, expense.Description, expense.Amount, expense.Date));
    }

    public async Task<List<ExpenseDto>> GetExpensesAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewExpenses);
        var expenses = await _uow.Repository<Expense>().FindAsync(e => e.Date >= from.Date && e.Date <= to.Date, ct);
        return expenses.OrderByDescending(e => e.Date).Select(e => new ExpenseDto(e.Id, e.Category, e.Description, e.Amount, e.Date)).ToList();
    }

    public async Task<Result<IncomeDto>> RecordIncomeAsync(CreateIncomeRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageIncomes);
        if (request.Amount <= 0) return Result.Failure<IncomeDto>("مبلغ عاید باید بزرگ‌تر از صفر باشد.");

        var income = new Income
        {
            Source = request.Source, Description = request.Description, Amount = request.Amount, Date = request.Date,
            RecordedByUserId = _currentUser.UserId ?? 0, CreatedByUserId = _currentUser.UserId
        };
        await _uow.Repository<Income>().AddAsync(income, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success(new IncomeDto(income.Id, income.Source, income.Description, income.Amount, income.Date));
    }

    public async Task<List<IncomeDto>> GetIncomesAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewIncomes);
        var incomes = await _uow.Repository<Income>().FindAsync(i => i.Date >= from.Date && i.Date <= to.Date, ct);
        return incomes.OrderByDescending(i => i.Date).Select(i => new IncomeDto(i.Id, i.Source, i.Description, i.Amount, i.Date)).ToList();
    }

    /// <summary>
    /// One consolidated number set for the period: what came in (fees + other income), what went
    /// out (expenses + paid payroll), and what is still owed by students — the single screen a
    /// school owner actually wants to see.
    /// </summary>
    public async Task<FinancialSummaryDto> GetFinancialSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewReports);

        var payments = await _uow.Repository<Payment>().FindAsync(p => p.PaymentDate >= from.Date && p.PaymentDate <= to.Date, ct);
        var incomes = await _uow.Repository<Income>().FindAsync(i => i.Date >= from.Date && i.Date <= to.Date, ct);
        var expenses = await _uow.Repository<Expense>().FindAsync(e => e.Date >= from.Date && e.Date <= to.Date, ct);
        var payroll = await _uow.Repository<PayrollRecord>().FindAsync(
            p => p.Status == PayrollStatus.Paid && p.PaidAtUtc.HasValue && p.PaidAtUtc.Value >= from.Date && p.PaidAtUtc.Value <= to.Date, ct);
        var outstandingInvoices = await _uow.Repository<FeeInvoice>().FindAsync(
            i => i.Status != FeeInvoiceStatus.Paid && i.Status != FeeInvoiceStatus.Waived, ct);

        var feesCollected = payments.Sum(p => p.AmountPaid);
        var otherIncome = incomes.Sum(i => i.Amount);
        var totalExpenses = expenses.Sum(e => e.Amount);
        var totalPayroll = payroll.Sum(p => p.NetAmount);
        var outstandingDebt = outstandingInvoices.Sum(i => i.AmountDue - i.AmountPaid);

        return new FinancialSummaryDto(
            feesCollected, otherIncome, totalExpenses, totalPayroll,
            NetBalance: feesCollected + otherIncome - totalExpenses - totalPayroll,
            TotalOutstandingDebt: outstandingDebt);
    }
}
