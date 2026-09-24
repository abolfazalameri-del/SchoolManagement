using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.Authorization;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Application.Services;
using SchoolManagement.Infrastructure.Backup;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Repositories;
using SchoolManagement.Infrastructure.Security;

namespace SchoolManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        DbPathProvider.EnsureFoldersExist();

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(DbPathProvider.ConnectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IBackupService, BackupService>();

        // Security/session — one CurrentUserContext per running app instance (Singleton),
        // everything that reads "who is logged in" shares this same object.
        services.AddSingleton<CurrentUserContext>();
        services.AddSingleton<ICurrentUserContext>(sp => sp.GetRequiredService<CurrentUserContext>());
        services.AddScoped<IAuthorizationService, AuthorizationService>();

        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<IAuditLogReader, AuditLogReader>();

        return services;
    }
}
