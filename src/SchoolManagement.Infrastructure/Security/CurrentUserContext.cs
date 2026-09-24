using SchoolManagement.Application.Authorization;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Infrastructure.Security;

/// <summary>
/// Registered as a Singleton: one desktop app process has exactly one logged-in user at a time.
/// WPF's login screen calls SignIn() once AuthService.LoginAsync succeeds; every other service
/// in the app then reads the same instance via ICurrentUserContext.
/// </summary>
public class CurrentUserContext : ICurrentUserContext
{
    public int? UserId { get; private set; }
    public string? FullName { get; private set; }
    public UserRoleType? Role { get; private set; }
    public bool IsAuthenticated => UserId.HasValue;

    public void SignIn(int userId, string fullName, UserRoleType role)
    {
        UserId = userId;
        FullName = fullName;
        Role = role;
    }

    public void SignOut()
    {
        UserId = null;
        FullName = null;
        Role = null;
    }
}
