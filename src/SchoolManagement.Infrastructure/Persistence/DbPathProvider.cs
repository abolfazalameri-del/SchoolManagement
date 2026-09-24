namespace SchoolManagement.Infrastructure.Persistence;

/// <summary>
/// Single source of truth for on-disk paths.
///
/// IMPORTANT — read before assuming this gives you real multi-computer concurrency:
/// By default the database lives in this machine's LocalApplicationData, i.e. ONE computer only.
/// To let several office computers (manager, accountant, registrar) share the same data, an
/// administrator can point DatabaseFilePath at a shared Windows folder (a UNC path like
/// \\SERVER\SchoolData\school.db) via a small config file — see LoadNetworkPathOverride().
/// SQLite in WAL mode over a stable LAN share works fine for a small office (a handful of staff
/// computers, occasional simultaneous writes — attendance, one payment at a time). It is NOT a
/// substitute for a real database server under heavy concurrent write load, and it is NOT safe
/// over an unreliable network path (Wi-Fi drops, cloud-synced folders like OneDrive/Dropbox can
/// corrupt the file). If the school later needs many staff writing at the exact same moment,
/// the Repository/UnitOfWork pattern in this layer is what makes swapping to SQL Server or
/// PostgreSQL later a contained change — nothing above this layer needs to know.
/// </summary>
public static class DbPathProvider
{
    private static readonly string LocalBaseFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SchoolManagement");

    private static readonly string ConfigFilePath = Path.Combine(LocalBaseFolder, "db-location.txt");

    /// <summary>
    /// The effective base folder: a shared network path if configured (db-location.txt contains a
    /// folder path, e.g. \\SERVER\SchoolData), otherwise this machine's own LocalApplicationData.
    /// </summary>
    public static string DatabaseFolder
    {
        get
        {
            var overridePath = LoadNetworkPathOverride();
            return string.IsNullOrWhiteSpace(overridePath) ? LocalBaseFolder : overridePath;
        }
    }

    public static string DatabaseFilePath => Path.Combine(DatabaseFolder, "school.db");
    public static string BackupFolder => Path.Combine(DatabaseFolder, "Backups");
    public static string DocumentsFolder => Path.Combine(DatabaseFolder, "Documents");

    // WAL journal mode + a busy timeout make concurrent light access from a few workstations
    // survive brief lock contention instead of throwing immediately.
    public static string ConnectionString => $"Data Source={DatabaseFilePath};Cache=Shared;Pooling=True";

    public static void EnsureFoldersExist()
    {
        Directory.CreateDirectory(LocalBaseFolder); // always needed for the config file itself
        Directory.CreateDirectory(DatabaseFolder);
        Directory.CreateDirectory(BackupFolder);
        Directory.CreateDirectory(DocumentsFolder);
    }

    /// <summary>
    /// Reads an administrator-set shared folder path from db-location.txt, if present.
    /// Kept as a plain text file (not a DB setting) on purpose: it must be readable before the
    /// database connection even exists, and it is set once per workstation during installation.
    /// </summary>
    private static string? LoadNetworkPathOverride()
    {
        try
        {
            if (!File.Exists(ConfigFilePath)) return null;
            var content = File.ReadAllText(ConfigFilePath).Trim();
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
        catch
        {
            return null; // fall back silently to local mode rather than crash the app on startup
        }
    }

    /// <summary>Called from a Settings screen so an administrator can point this workstation at a shared folder.</summary>
    public static void SetNetworkPathOverride(string? uncOrLocalFolderPath)
    {
        Directory.CreateDirectory(LocalBaseFolder);
        File.WriteAllText(ConfigFilePath, uncOrLocalFolderPath ?? string.Empty);
    }
}
