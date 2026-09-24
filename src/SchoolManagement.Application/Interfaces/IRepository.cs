using System.Linq.Expressions;
using SchoolManagement.Domain.Common;

namespace SchoolManagement.Application.Interfaces;

/// <summary>
/// Generic data-access contract every use case depends on instead of talking to EF Core directly.
/// Keeps the Application layer free of any reference to Entity Framework or SQLite.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<T>> GetAllAsync(CancellationToken ct = default);
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    /// <summary>Soft-deletes: sets IsDeleted/DeletedAtUtc. Nothing in this system is ever hard-deleted from the UI.</summary>
    void SoftDelete(T entity, int deletedByUserId);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
}

/// <summary>Wraps one atomic save. Every use case: load via repositories, mutate, call SaveChangesAsync once.</summary>
public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : BaseEntity;
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Runs several dependent steps — each ending in its own SaveChangesAsync call — inside one
    /// real database transaction, committing only if every step completes and rolling back
    /// entirely otherwise. Use this whenever a change must be all-or-nothing across more than one
    /// SaveChangesAsync call (e.g. unset-old-then-set-new-current-year, or a multi-part promotion).
    /// A single SaveChangesAsync call is already atomic on its own and does not need this.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);
}

public interface IPasswordHasher
{
    (string hash, string salt) Hash(string plainTextPassword);
    bool Verify(string plainTextPassword, string hash, string salt);
}

public interface IBackupService
{
    /// <summary>Copies the live SQLite file to a timestamped backup path and records a BackupRecord.</summary>
    Task<string> CreateBackupAsync(bool isAutomatic, int? triggeredByUserId, CancellationToken ct = default);
    Task RestoreBackupAsync(string backupFilePath, CancellationToken ct = default);
    IReadOnlyList<string> ListAvailableBackups();
}
