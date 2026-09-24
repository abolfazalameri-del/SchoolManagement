using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence.Configurations;

public class FeeStructureConfiguration : IEntityTypeConfiguration<FeeStructure>
{
    public void Configure(EntityTypeBuilder<FeeStructure> b)
    {
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.HasOne(x => x.SchoolClass).WithMany().HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FeeInvoiceConfiguration : IEntityTypeConfiguration<FeeInvoice>
{
    public void Configure(EntityTypeBuilder<FeeInvoice> b)
    {
        b.Property(x => x.AmountDue).HasPrecision(18, 2);
        b.Property(x => x.AmountPaid).HasPrecision(18, 2);
        b.HasOne(x => x.Student).WithMany(s => s.FeeInvoices).HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SchoolClass).WithMany(c => c.FeeInvoices).HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.FeeStructure).WithMany().HasForeignKey(x => x.FeeStructureId).OnDelete(DeleteBehavior.SetNull);
        // Fast lookup for the "بدهکاران" (debtors) report: unpaid/partial invoices by class.
        b.HasIndex(x => new { x.Status, x.SchoolClassId });
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.Property(x => x.AmountPaid).HasPrecision(18, 2);
        b.Property(x => x.ReceiptNumber).IsRequired().HasMaxLength(30);
        b.HasIndex(x => x.ReceiptNumber).IsUnique();
        b.HasOne(x => x.FeeInvoice).WithMany(i => i.Payments).HasForeignKey(x => x.FeeInvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayrollRecordConfiguration : IEntityTypeConfiguration<PayrollRecord>
{
    public void Configure(EntityTypeBuilder<PayrollRecord> b)
    {
        b.Property(x => x.BaseSalary).HasPrecision(18, 2);
        b.Property(x => x.Deductions).HasPrecision(18, 2);
        b.Property(x => x.Bonuses).HasPrecision(18, 2);
        b.Property(x => x.NetAmount).HasPrecision(18, 2);
        b.HasOne(x => x.Teacher).WithMany(t => t.PayrollRecords).HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TeacherId, x.Year, x.Month }).IsUnique();
    }
}

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> b)
    {
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Category).IsRequired().HasMaxLength(80);
    }
}

public class IncomeConfiguration : IEntityTypeConfiguration<Income>
{
    public void Configure(EntityTypeBuilder<Income> b)
    {
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Source).IsRequired().HasMaxLength(80);
    }
}
