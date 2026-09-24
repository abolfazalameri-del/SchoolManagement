using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SchoolManagement.Infrastructure.Persistence;

/// <summary>
/// Used only by the `dotnet ef migrations add` / `dotnet ef database update` CLI commands at
/// design time. The real app builds its own DbContextOptions via DI in the WPF host (phase 4).
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        DbPathProvider.EnsureFoldersExist();
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite(DbPathProvider.ConnectionString);
        return new AppDbContext(optionsBuilder.Options);
    }
}
