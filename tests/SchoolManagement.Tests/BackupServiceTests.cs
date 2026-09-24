using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Infrastructure.Backup;
using SchoolManagement.Infrastructure.Persistence;
using Xunit;

namespace SchoolManagement.Tests;

/// <summary>
/// KNOWN LIMITATION (documented honestly rather than hidden): BackupService reads its paths from
/// the static DbPathProvider, which is not injectable. This test redirects DbPathProvider to a
/// temp folder via its existing SetNetworkPathOverride mechanism (built for pointing a real
/// workstation at a shared network folder) rather than a dedicated test seam. That means this
/// test mutates process-wide static state — it is NOT safely parallelizable with any other test
/// that also touches DbPathProvider, and xUnit runs test classes in parallel by default. A future
/// phase should extract an IDbPathProvider interface so this can be tested cleanly.
/// </summary>
public class BackupServiceTests : IDisposable
{
    private readonly string _tempRoot;

    public BackupServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"school-mgmt-backup-test-{Guid.NewGuid():N}");
        DbPathProvider.SetNetworkPathOverride(_tempRoot);
        DbPathProvider.EnsureFoldersExist();
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        DbPathProvider.SetNetworkPathOverride(null); // reset so it doesn't leak into other tests/real usage
        try { Directory.Delete(_tempRoot, recursive: true); } catch { /* best effort */ }
    }

    private AppDbContext CreateLiveContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(DbPathProvider.ConnectionString).Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task CreateBackup_ProducesNonEmptyValidFile_AndAuditsIt()
    {
        using var context = CreateLiveContext();
        context.Subjects.Add(new Subject { Name = "علوم", Code = "SCI" });
        await context.SaveChangesAsync();

        var audit = new AuditLogger(context);
        var backupService = new BackupService(context, audit);

        var backupPath = await backupService.CreateBackupAsync(isAutomatic: false, triggeredByUserId: 1);

        Assert.True(File.Exists(backupPath));
        Assert.True(new FileInfo(backupPath).Length > 0);

        var records = await context.BackupRecords.ToListAsync();
        Assert.Single(records);
        Assert.True(records[0].Succeeded);

        var auditEntries = await context.AuditLogEntries.Where(e => e.Action == "BackupCreated").ToListAsync();
        Assert.Single(auditEntries);
    }

    [Fact]
    public async Task RestoreBackup_BringsBackOlderData()
    {
        using var context = CreateLiveContext();
        var audit = new AuditLogger(context);
        var backupService = new BackupService(context, audit);

        context.Subjects.Add(new Subject { Name = "قبل از بکاپ", Code = "BEFORE" });
        await context.SaveChangesAsync();
        var backupPath = await backupService.CreateBackupAsync(isAutomatic: false, triggeredByUserId: 1);

        context.Subjects.Add(new Subject { Name = "بعد از بکاپ", Code = "AFTER" });
        await context.SaveChangesAsync();

        // Restore needs the live context's connection released first — same requirement WPF must
        // follow in real use (see BackupService's own ClearAllPools call inside RestoreBackupAsync).
        context.Dispose();

        await backupService.RestoreBackupAsync(backupPath);

        using var afterRestoreContext = CreateLiveContext();
        var subjects = await afterRestoreContext.Subjects.Select(s => s.Code).ToListAsync();
        Assert.Contains("BEFORE", subjects);
        Assert.DoesNotContain("AFTER", subjects); // proves the restore actually rolled back, not just copied on top
    }

    [Fact]
    public async Task RestoreBackup_WithInvalidFile_ThrowsAndLeavesLiveDbIntact()
    {
        using var context = CreateLiveContext();
        var audit = new AuditLogger(context);
        var backupService = new BackupService(context, audit);

        context.Subjects.Add(new Subject { Name = "داده اصلی", Code = "ORIGINAL" });
        await context.SaveChangesAsync();

        var fakeBackupPath = Path.Combine(_tempRoot, "not-a-real-backup.db");
        await File.WriteAllTextAsync(fakeBackupPath, "این یک فایل SQLite واقعی نیست");

        await Assert.ThrowsAsync<InvalidDataException>(() => backupService.RestoreBackupAsync(fakeBackupPath));

        context.Dispose();
        using var verifyContext = CreateLiveContext();
        var subjects = await verifyContext.Subjects.Select(s => s.Code).ToListAsync();
        Assert.Contains("ORIGINAL", subjects); // untouched — invalid restore must never damage the live database
    }
}
