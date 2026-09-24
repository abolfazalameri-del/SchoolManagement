using SchoolManagement.Application.Authorization;

namespace SchoolManagement.Infrastructure.Security;

public class AuthorizationService : IAuthorizationService
{
    private readonly ICurrentUserContext _currentUser;

    public AuthorizationService(ICurrentUserContext currentUser)
    {
        _currentUser = currentUser;
    }

    public bool HasPermission(Permission permission)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Role is null) return false;
        return RolePermissions.Has(_currentUser.Role.Value, permission);
    }

    public void EnsurePermission(Permission permission)
    {
        if (!HasPermission(permission))
            throw new UnauthorizedAccessException($"شما اجازه انجام این عملیات را ندارید ({permission}).");
    }
}
