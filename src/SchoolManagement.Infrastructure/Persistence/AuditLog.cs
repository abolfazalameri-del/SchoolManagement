using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.Services;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Infrastructure.Persistence;

public class AuditLogger : IAuditLogger
{
    private readonly AppDbContext _context;

    public AuditLogger(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int userId, string userFullName, string action, string entityName, int? entityId, string? details = null, CancellationToken ct = default)
    {
        // Written directly and saved immediately — audit entries are never batched with the
        // business-data SaveChanges they describe, so a rolled-back transaction never silently
        // erases the record that something was attempted.
        _context.AuditLogEntries.Add(new AuditLogEntry
        {
            UserId = userId,
            UserFullName = userFullName,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details
        });
        await _context.SaveChangesAsync(ct);
    }
}

public class AuditLogReader : IAuditLogReader
{
    private readonly AppDbContext _context;

    public AuditLogReader(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<AuditLogEntry>> GetRecentAsync(int count, CancellationToken ct = default) =>
        await _context.AuditLogEntries.OrderByDescending(e => e.TimestampUtc).Take(count).ToListAsync(ct);

    public async Task<List<AuditLogEntry>> GetForEntityAsync(string entityName, int entityId, CancellationToken ct = default) =>
        await _context.AuditLogEntries
            .Where(e => e.EntityName == entityName && e.EntityId == entityId)
            .OrderByDescending(e => e.TimestampUtc)
            .ToListAsync(ct);
}
