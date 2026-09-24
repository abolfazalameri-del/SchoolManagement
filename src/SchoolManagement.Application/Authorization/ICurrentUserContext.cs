using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.Authorization;

/// <summary>Who is using the app right now. Implemented in WPF (backed by the login session), injected into every use case.</summary>
public interface ICurrentUserContext
{
    int? UserId { get; }
    string? FullName { get; }
    UserRoleType? Role { get; }
    bool IsAuthenticated { get; }
}

/// <summary>Central place every use case calls before performing a protected operation.</summary>
public interface IAuthorizationService
{
    bool HasPermission(Permission permission);
    void EnsurePermission(Permission permission); // throws UnauthorizedAccessException if missing
}
