using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.Services;

namespace SchoolManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IParentService, ParentService>();
        services.AddScoped<ITeacherService, TeacherService>();
        services.AddScoped<IAcademicsService, AcademicsService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IExamService, ExamService>();
        services.AddScoped<IFeeService, FeeService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<ILedgerService, LedgerService>();
        services.AddScoped<IFacilitiesService, FacilitiesService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAcademicYearService, AcademicYearService>();

        return services;
    }
}
