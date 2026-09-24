using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.DTOs;

public record LoginRequest(string Username, string Password);
public record LoginResultDto(int UserId, string FullName, UserRoleType Role, bool MustChangePassword);

public record CreateUserRequest(string Username, string FullName, string InitialPassword, UserRoleType Role);
public record UserDto(int Id, string Username, string FullName, UserRoleType Role, bool IsActive, DateTime? LastLoginAtUtc);
public record ChangePasswordRequest(int UserId, string CurrentPassword, string NewPassword);
public record ResetPasswordRequest(int UserId, string NewTemporaryPassword);

public record UpdateSettingRequest(string Key, string Value);
public record SettingDto(string Key, string? Value, string? Description);

public record AuditLogRowDto(DateTime TimestampUtc, string UserFullName, string Action, string EntityName, int? EntityId, string? Details);

public record DashboardSummaryDto(
    int TotalActiveStudents, int TotalTeachers, int TotalClasses,
    double TodayAttendanceRatePercent, decimal ThisMonthFeesCollected, decimal TotalOutstandingDebt);
