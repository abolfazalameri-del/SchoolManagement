namespace SchoolManagement.Application.Common;

/// <summary>Records who did what. Every Create/Update/SoftDelete/Login use case calls this after a successful save.</summary>
public interface IAuditLogger
{
    Task LogAsync(int userId, string userFullName, string action, string entityName, int? entityId, string? details = null, CancellationToken ct = default);
}
