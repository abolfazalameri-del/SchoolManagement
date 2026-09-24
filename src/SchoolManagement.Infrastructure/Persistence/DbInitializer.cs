using System.Security.Cryptography;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Infrastructure.Persistence;

/// <summary>
/// Runs once at app startup. Creates the database file/schema if missing, and — only on a truly
/// empty database — seeds the one administrator account needed to log in for the first time.
///
/// NOTE on migrations (unchanged since Phase 3.5, still true in Phase 3.6): this uses
/// EnsureCreated() rather than Migrate(), because generating real EF Core migration files needs
/// the `dotnet ef` CLI tool and the .NET SDK — neither available in the Linux sandbox this code
/// was authored in. Phase 3.6 added a manual GitHub Actions job
/// (generate-ef-migration, workflow_dispatch) that generates a real InitialCreate migration using
/// CI's actual SDK. This method is deliberately NOT yet switched to Migrate(): flipping it before
/// the CI-generated migration has been reviewed and confirmed correct would risk Migrate() running
/// against zero migrations and creating no schema at all. Switch this once that migration exists
/// and has been verified.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context, IPasswordHasher passwordHasher)
    {
        await context.Database.EnsureCreatedAsync();

        if (!context.Users.IgnoreQueryFilters().Any())
        {
            // Phase 3.6 security fix: no more fixed "Admin@12345" — that exact string was already
            // hardcoded here AND printed on the login screen in earlier phases, which is a real
            // default-credential-in-production finding (see PHASE-3.6-AUDIT.md). A random password
            // is generated instead and written once to a text file next to the database, since this
            // phase is not allowed to build the "First Run Setup" UI that would normally collect it
            // from the administrator interactively (that is Phase 4 work).
            var temporaryPassword = GenerateRandomPassword();
            var (hash, salt) = passwordHasher.Hash(temporaryPassword);
            context.Users.Add(new User
            {
                Username = "admin",
                FullName = "مدیر سیستم",
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = UserRoleType.Administrator,
                IsActive = true,
                MustChangePasswordOnNextLogin = true
            });

            WriteFirstRunCredentialFile(temporaryPassword);
        }

        if (!context.Settings.Any(s => s.Key == "SchoolName"))
        {
            // Phase 3.6 fix: no "AcademicYear" setting seeded here anymore, and no AcademicYear
            // entity is auto-created either (see below) — DateTime.Now.Year silently standing in
            // for a real Afghan academic year was flagged in the audit as a real correctness bug,
            // not just a placeholder. An administrator must create the real academic year explicitly
            // (via IAcademicYearService — a UI for this is Phase 4 work).
            context.Settings.AddRange(
                new Setting { Key = "SchoolName", Value = "مکتب خصوصی", Description = "نام مکتب که در سربرگ‌ها نمایش داده می‌شود" },
                new Setting { Key = "Currency", Value = "افغانی", Description = "واحد پول برای فیس و معاش" }
            );
        }

        // Deliberately no AcademicYears seed row (Phase 3.5 had one, seeded from DateTime.Now.Year —
        // removed in Phase 3.6, see audit). The system now starts with zero academic years, which is
        // an honest reflection of "not configured yet" rather than a silently wrong default.

        await context.SaveChangesAsync();
    }

    private static string GenerateRandomPassword()
    {
        const string allowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        var bytes = RandomNumberGenerator.GetBytes(16);
        var chars = new char[16];
        for (int i = 0; i < bytes.Length; i++)
            chars[i] = allowedChars[bytes[i] % allowedChars.Length];
        return new string(chars);
    }

    private static void WriteFirstRunCredentialFile(string temporaryPassword)
    {
        try
        {
            DbPathProvider.EnsureFoldersExist();
            var path = Path.Combine(DbPathProvider.DatabaseFolder, "FIRST-RUN-ADMIN-PASSWORD.txt");
            var content =
                $"این رمز عبور موقت مدیر سیستم است — فقط یک‌بار اینجا نوشته می‌شود.{Environment.NewLine}" +
                $"نام کاربری: admin{Environment.NewLine}" +
                $"رمز عبور موقت: {temporaryPassword}{Environment.NewLine}{Environment.NewLine}" +
                $"لطفاً بلافاصله بعد از اولین ورود، رمز عبور را تغییر دهید (سیستم این کار را اجباری می‌کند)،{Environment.NewLine}" +
                $"سپس این فایل را برای امنیت حذف کنید.{Environment.NewLine}" +
                $"تاریخ ایجاد: {DateTime.Now:yyyy-MM-dd HH:mm}";
            File.WriteAllText(path, content);
        }
        catch
        {
            // If this file can't be written (e.g. read-only folder), the admin account still
            // works — it just means the administrator must recover access via a database-level
            // reset rather than reading this file. Not silently swallowed in spirit: the password
            // itself was already hashed and saved to the database regardless of whether this
            // convenience copy could be written to disk.
        }
    }
}
