using SchoolManagement.Application.Authorization;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Shared;

namespace SchoolManagement.Application.Services;

public interface IAuthService
{
    Task<Result<LoginResultDto>> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

/// <summary>
/// Deliberately does not depend on ICurrentUserContext/IAuthorizationService — there is no
/// logged-in user yet at the point this runs. It IS the thing that establishes who the user is.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogger _audit;

    public AuthService(IUnitOfWork uow, IPasswordHasher passwordHasher, IAuditLogger audit)
    {
        _uow = uow; _passwordHasher = passwordHasher; _audit = audit;
    }

    public async Task<Result<LoginResultDto>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return Result.Failure<LoginResultDto>("نام کاربری و رمز عبور الزامی است.");

        var user = await _uow.Repository<User>().FirstOrDefaultAsync(u => u.Username == request.Username, ct);
        // Same generic message whether the username doesn't exist or the password is wrong —
        // never reveal which one it was, that's a basic account-enumeration protection.
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
            return Result.Failure<LoginResultDto>("نام کاربری یا رمز عبور نادرست است.");

        if (!user.IsActive)
            return Result.Failure<LoginResultDto>("این حساب کاربری غیرفعال شده است. با مدیر سیستم تماس بگیرید.");

        user.LastLoginAtUtc = DateTime.UtcNow;
        _uow.Repository<User>().Update(user);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(user.Id, user.FullName, "LoggedIn", nameof(User), user.Id, null, ct);

        return Result.Success(new LoginResultDto(user.Id, user.FullName, user.Role, user.MustChangePasswordOnNextLogin));
    }
}

public interface IUserService
{
    Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
    Task<Result> SetActiveAsync(int userId, bool isActive, CancellationToken ct = default);
    Task<List<UserDto>> GetAllAsync(CancellationToken ct = default);
}

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthorizationService _authz;
    private readonly ICurrentUserContext _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogger _audit;

    public UserService(IUnitOfWork uow, IAuthorizationService authz, ICurrentUserContext currentUser, IPasswordHasher passwordHasher, IAuditLogger audit)
    {
        _uow = uow; _authz = authz; _currentUser = currentUser; _passwordHasher = passwordHasher; _audit = audit;
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageUsers);
        if (string.IsNullOrWhiteSpace(request.Username)) return Result.Failure<UserDto>("نام کاربری الزامی است.");
        if (request.InitialPassword.Length < 6) return Result.Failure<UserDto>("رمز عبور باید حداقل ۶ کاراکتر باشد.");

        var duplicate = await _uow.Repository<User>().FirstOrDefaultAsync(u => u.Username == request.Username, ct);
        if (duplicate is not null) return Result.Failure<UserDto>("این نام کاربری قبلاً استفاده شده است.");

        var (hash, salt) = _passwordHasher.Hash(request.InitialPassword);
        var user = new User
        {
            Username = request.Username, FullName = request.FullName, PasswordHash = hash, PasswordSalt = salt,
            Role = request.Role, IsActive = true, MustChangePasswordOnNextLogin = true, CreatedByUserId = _currentUser.UserId
        };
        await _uow.Repository<User>().AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "Created", nameof(User), user.Id, $"{user.Username} ({user.Role})", ct);

        return Result.Success(ToDto(user));
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _uow.Repository<User>().GetByIdAsync(request.UserId, ct);
        if (user is null) return Result.Failure("کاربر یافت نشد.");
        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            return Result.Failure("رمز عبور فعلی نادرست است.");
        if (request.NewPassword.Length < 6) return Result.Failure("رمز عبور جدید باید حداقل ۶ کاراکتر باشد.");

        var (hash, salt) = _passwordHasher.Hash(request.NewPassword);
        user.PasswordHash = hash; user.PasswordSalt = salt; user.MustChangePasswordOnNextLogin = false;
        _uow.Repository<User>().Update(user);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(user.Id, user.FullName, "ChangedOwnPassword", nameof(User), user.Id, null, ct);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageUsers);
        var user = await _uow.Repository<User>().GetByIdAsync(request.UserId, ct);
        if (user is null) return Result.Failure("کاربر یافت نشد.");

        var (hash, salt) = _passwordHasher.Hash(request.NewTemporaryPassword);
        user.PasswordHash = hash; user.PasswordSalt = salt; user.MustChangePasswordOnNextLogin = true;
        _uow.Repository<User>().Update(user);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "ResetPassword", nameof(User), user.Id, null, ct);
        return Result.Success();
    }

    public async Task<Result> SetActiveAsync(int userId, bool isActive, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageUsers);
        var user = await _uow.Repository<User>().GetByIdAsync(userId, ct);
        if (user is null) return Result.Failure("کاربر یافت نشد.");
        if (userId == _currentUser.UserId && !isActive) return Result.Failure("نمی‌توانید حساب خودتان را غیرفعال کنید.");

        user.IsActive = isActive;
        _uow.Repository<User>().Update(user);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", isActive ? "Activated" : "Deactivated", nameof(User), user.Id, null, ct);
        return Result.Success();
    }

    public async Task<List<UserDto>> GetAllAsync(CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewUsers);
        var users = await _uow.Repository<User>().GetAllAsync(ct);
        return users.OrderBy(u => u.FullName).Select(ToDto).ToList();
    }

    private static UserDto ToDto(User u) => new(u.Id, u.Username, u.FullName, u.Role, u.IsActive, u.LastLoginAtUtc);
}
