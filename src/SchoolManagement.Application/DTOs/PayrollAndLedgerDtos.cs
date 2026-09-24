using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs;

public record GeneratePayrollRequest(int Month, int Year, string AcademicYear);
public record PayrollRowDto(int Id, int TeacherId, string TeacherName, int Month, int Year, decimal BaseSalary, decimal Deductions, decimal Bonuses, decimal NetAmount, PayrollStatus Status);
public record AdjustPayrollRequest(int PayrollId, decimal Deductions, decimal Bonuses, string? Note);
public record MarkPayrollPaidRequest(int PayrollId);

public record CreateExpenseRequest(string Category, string Description, decimal Amount, DateTime Date, string? ReceiptReference);
public record ExpenseDto(int Id, string Category, string Description, decimal Amount, DateTime Date);

public record CreateIncomeRequest(string Source, string Description, decimal Amount, DateTime Date);
public record IncomeDto(int Id, string Source, string Description, decimal Amount, DateTime Date);

public record FinancialSummaryDto(
    decimal TotalFeesCollected, decimal TotalOtherIncome, decimal TotalExpenses, decimal TotalPayroll,
    decimal NetBalance, decimal TotalOutstandingDebt);
