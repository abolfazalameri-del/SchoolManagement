using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs;

public record CreateFeeStructureRequest(string Title, int SchoolClassId, string AcademicYear, decimal Amount, bool IsRecurringMonthly);
public record FeeStructureDto(int Id, string Title, int SchoolClassId, string SchoolClassName, string AcademicYear, decimal Amount, bool IsRecurringMonthly);

public record GenerateMonthlyInvoicesRequest(int FeeStructureId, int BillingMonth, DateTime DueDate);
public record CreateAdHocInvoiceRequest(int StudentId, string Description, decimal AmountDue, DateTime DueDate, string AcademicYear);

public record FeeInvoiceDto(
    int Id, int StudentId, string StudentName, string Description, string AcademicYear,
    decimal AmountDue, decimal AmountPaid, decimal Balance, DateTime DueDate, FeeInvoiceStatus Status);

public record RecordPaymentRequest(int FeeInvoiceId, decimal AmountPaid, PaymentMethod Method, string? Note);
public record PaymentDto(int Id, string ReceiptNumber, int FeeInvoiceId, int StudentId, string StudentName, decimal AmountPaid, DateTime PaymentDate, PaymentMethod Method, string ReceivedByUserFullName);

public record DebtorRowDto(int StudentId, string StudentName, string SchoolClassName, decimal TotalDue, decimal TotalPaid, decimal Balance, int OverdueInvoiceCount);
