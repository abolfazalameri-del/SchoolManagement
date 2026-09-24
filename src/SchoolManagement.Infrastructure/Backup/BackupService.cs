using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Infrastructure.Persistence;

namespace SchoolManagement.Infrastructure.Backup;

/// <summary>
/// Backs up by copying the SQLite file itself — simplest and most reliable approach for SQLite,
/// and trivially restorable by any non-technical admin (it's just a file).
/// </summary>
public class BackupService : IBackupService
{
    private readonly AppDbContext _context;
    private readonly IAuditLogger _audit;

    public BackupService(AppDbContext context, IAuditLogger audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<string> CreateBackupAsync(bool isAutomatic, int? triggeredByUserId, CancellationToken ct = default)
    {
        DbPathProvider.EnsureFoldersExist();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var backupFileName = $"school-backup-{timestamp}.db";
        var backupFilePath = Path.Combine(DbPathProvider.BackupFolder, backupFileName);

        var record = new BackupRecord
        {
            FilePath = backupFilePath,
            WasAutomatic = isAutomatic,
            CreatedByUserId = triggeredByUserId
        };

        try
        {
            // Checkpoint WAL into the main file first so the copy is complete and consistent.
            await _context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL);", ct);
            File.Copy(DbPathProvider.DatabaseFilePath, backupFilePath, overwrite: false);

            // Phase 3.6 hardening: an incomplete or corrupt copy must never be recorded as a
            // successful backup — verify size and the SQLite file header before trusting it.
            if (!IsPlausibleSqliteFile(backupFilePath))
            {
                TryDelete(backupFilePath);
                throw new IOException("فایل پشتیبان ایجاد‌شده معتبر یا کامل نیست (احتمالاً فضای دیسک کافی نبوده).");
            }

            record.Succeeded = true;
            record.FileSizeBytes = new FileInfo(backupFilePath).Length;
        }
        catch (Exception ex)
        {
            record.Succeeded = false;
            record.ErrorMessage = ex.Message;
        }

        _context.BackupRecords.Add(record);
        await _context.SaveChangesAsync(ct);

        await _audit.LogAsync(
            triggeredByUserId ?? 0, isAutomatic ? "سیستم (خودکار)" : "کاربر",
            record.Succeeded ? "BackupCreated" : "BackupFailed",
            nameof(BackupRecord), record.Id,
            record.Succeeded ? backupFileName : record.ErrorMessage, ct);

        if (!record.Succeeded)
            throw new IOException($"پشتیبان‌گیری ناموفق بود: {record.ErrorMessage}");

        return backupFilePath;
    }

    public async Task RestoreBackupAsync(string backupFilePath, CancellationToken ct = default)
    {
        if (!File.Exists(backupFilePath))
            throw new FileNotFoundException("فایل بکاپ یافت نشد.", backupFilePath);

        // Validate BEFORE touching the live database — restoring a corrupt file must never be
        // allowed to destroy a working one.
        if (!IsPlausibleSqliteFile(backupFilePath))
            throw new InvalidDataException("فایل انتخاب‌شده یک فایل پشتیبان معتبر SQLite نیست.");

        var liveDbPath = DbPathProvider.DatabaseFilePath;
        var safetyCopyPath = liveDbPath + $".before-restore-{DateTime.Now:yyyyMMdd-HHmmss}.bak";

        int? currentUserId = null;
        try
        {
            // Release every pooled connection this process holds to the live database file —
            // SQLite cannot have its file replaced on Windows while a connection has it open,
            // and simply telling the caller "close your DbContext first" (Phase 3.5's approach)
            // was not actually enforced anywhere.
            SqliteConnection.ClearAllPools();

            File.Copy(liveDbPath, safetyCopyPath, overwrite: true);
            File.Copy(backupFilePath, liveDbPath, overwrite: true);

            // Verify the file now in place is actually openable, not just byte-identical to a
            // valid header — a truncated copy could still pass the header check.
            if (!IsPlausibleSqliteFile(liveDbPath) || !CanOpenAndQuery(liveDbPath))
            {
                // Restore failed to leave a working database — recover the original immediately
                // rather than leaving the school with a broken file and only a report of failure.
                File.Copy(safetyCopyPath, liveDbPath, overwrite: true);
                throw new IOException("بازیابی ناموفق بود؛ پایگاه داده اصلی بازگردانده شد و دست‌نخورده باقی ماند.");
            }

            await _audit.LogAsync(currentUserId ?? 0, "کاربر", "RestoreSucceeded", nameof(BackupRecord), null, Path.GetFileName(backupFilePath), ct);
        }
        catch (Exception ex)
        {
            await _audit.LogAsync(currentUserId ?? 0, "کاربر", "RestoreFailed", nameof(BackupRecord), null, ex.Message, ct);
            throw;
        }
    }

    public IReadOnlyList<string> ListAvailableBackups()
    {
        DbPathProvider.EnsureFoldersExist();
        return Directory.GetFiles(DbPathProvider.BackupFolder, "school-backup-*.db")
            .OrderByDescending(f => f)
            .ToList();
    }

    /// <summary>Cheap, fast check: non-trivial size and the literal SQLite file header ("SQLite format 3\0"). Catches truncated/empty copies from a full disk without needing to open the file.</summary>
    private static bool IsPlausibleSqliteFile(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length < 512) return false;

            using var stream = File.OpenRead(path);
            var header = new byte[16];
            var read = stream.Read(header, 0, 16);
            if (read < 16) return false;

            var expected = "SQLite format 3\0"u8.ToArray();
            return header.SequenceEqual(expected);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>More expensive check used only after Restore: actually open the file and run a trivial query, since a file can pass the header check and still be internally corrupt.</summary>
    private static bool CanOpenAndQuery(string path)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={path}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT count(*) FROM sqlite_master;";
            command.ExecuteScalar();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* best-effort cleanup only */ }
    }
}
