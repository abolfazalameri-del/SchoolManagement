using Microsoft.EntityFrameworkCore;
using SchoolManagement.Domain.Common;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // People
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<StudentParent> StudentParents => Set<StudentParent>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<User> Users => Set<User>();

    // Academics
    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ClassSubjectTeacher> ClassSubjectTeachers => Set<ClassSubjectTeacher>();
    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();

    // Attendance
    public DbSet<StudentAttendanceRecord> StudentAttendanceRecords => Set<StudentAttendanceRecord>();
    public DbSet<StaffAttendanceRecord> StaffAttendanceRecords => Set<StaffAttendanceRecord>();

    // Exams
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamResult> ExamResults => Set<ExamResult>();
    public DbSet<ReportCard> ReportCards => Set<ReportCard>();
    public DbSet<ReportCardSubjectLine> ReportCardSubjectLines => Set<ReportCardSubjectLine>();

    // Finance
    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
    public DbSet<FeeInvoice> FeeInvoices => Set<FeeInvoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Income> Incomes => Set<Income>();

    // Facilities
    public DbSet<Notice> Notices => Set<Notice>();
    public DbSet<DocumentFile> DocumentFiles => Set<DocumentFile>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<LibraryLoan> LibraryLoans => Set<LibraryLoan>();
    public DbSet<Asset> Assets => Set<Asset>();

    // System
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<BackupRecord> BackupRecords => Set<BackupRecord>();
    public DbSet<Setting> Settings => Set<Setting>();

    // Academic year management
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<StudentEnrollmentHistory> StudentEnrollmentHistories => Set<StudentEnrollmentHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global soft-delete filter + optimistic-concurrency token: every entity deriving from
        // BaseEntity gets both, via the same reflection pass, so no individual configuration class
        // has to remember to opt in.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                InvokeGeneric(nameof(SetSoftDeleteFilter), entityType.ClrType, modelBuilder);
                InvokeGeneric(nameof(SetConcurrencyToken), entityType.ClrType, modelBuilder);
            }
        }
    }

    private static void InvokeGeneric(string methodName, Type entityClrType, ModelBuilder modelBuilder)
    {
        var method = typeof(AppDbContext)
            .GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(entityClrType);
        method.Invoke(null, new object[] { modelBuilder });
    }

    private static void SetConcurrencyToken<T>(ModelBuilder modelBuilder) where T : BaseEntity
    {
        // SQLite has no server-generated rowversion type (unlike SQL Server's ROWVERSION), so
        // RowVersion is a plain BLOB that THIS application must regenerate on every write — see
        // the SaveChangesAsync override below. IsConcurrencyToken() is what makes EF Core include
        // the old value in the UPDATE's WHERE clause and throw DbUpdateConcurrencyException when
        // zero rows match (i.e. someone else changed the row since it was read).
        modelBuilder.Entity<T>().Property(e => e.RowVersion).IsConcurrencyToken();
    }

    private static void SetSoftDeleteFilter<T>(ModelBuilder modelBuilder) where T : BaseEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    /// <summary>
    /// Stamps CreatedAtUtc/ModifiedAtUtc automatically so every use case doesn't have to remember to.
    /// User-id stamping (CreatedByUserId/ModifiedByUserId) is still set explicitly by the use case,
    /// since the DbContext itself does not know who the current user is.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAtUtc = now;
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
