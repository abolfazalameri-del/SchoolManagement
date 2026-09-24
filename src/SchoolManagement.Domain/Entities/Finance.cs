using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Domain.Entities;

/// <summary>A defined fee amount for a class/year (e.g. monthly tuition for Class 6, 2026). Invoices are generated from this.</summary>
public class FeeStructure : BaseEntity
{
    public string Title { get; set; } = string.Empty; // فیس ماهانه، فیس داخله، ...
    public int SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsRecurringMonthly { get; set; }
}

/// <summary>A specific amount owed by one student for one period. This is the source of truth for "who owes what" (بدهکاران).</summary>
public class FeeInvoice : BaseEntity
{
    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public int SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }

    public int? FeeStructureId { get; set; }
    public FeeStructure? FeeStructure { get; set; }

    public string Description { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public int? BillingMonth { get; set; } // 1-12 for monthly tuition invoices
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime DueDate { get; set; }
    public FeeInvoiceStatus Status { get; set; } = FeeInvoiceStatus.Unpaid;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class Payment : BaseEntity
{
    public int FeeInvoiceId { get; set; }
    public FeeInvoice? FeeInvoice { get; set; }

    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public PaymentMethod Method { get; set; }
    public string? Note { get; set; }

    public int ReceivedByUserId { get; set; }
}

public class PayrollRecord : BaseEntity
{
    public int TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public string AcademicYear { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }

    public decimal BaseSalary { get; set; }
    public decimal Deductions { get; set; }
    public decimal Bonuses { get; set; }
    public decimal NetAmount { get; set; }

    public PayrollStatus Status { get; set; } = PayrollStatus.Pending;
    public DateTime? PaidAtUtc { get; set; }
    public int? PaidByUserId { get; set; }
    public string? Note { get; set; }
}

public class Expense : BaseEntity
{
    public string Category { get; set; } = string.Empty; // برق، آب، اجاره، تعمیرات، ...
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? ReceiptReference { get; set; }
    public int RecordedByUserId { get; set; }
}

public class Income : BaseEntity
{
    public string Source { get; set; } = string.Empty; // کمک، فروش، سایر عواید غیر از فیس
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public int RecordedByUserId { get; set; }
}
