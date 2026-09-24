using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Application.Authorization;

/// <summary>
/// Static, single source of truth for "which role can do what". Changing access rules means
/// editing this one file — never scattered checks in the UI. Administrator implicitly has
/// every permission and is not listed explicitly to avoid the list going stale.
/// </summary>
public static class RolePermissions
{
    private static readonly IReadOnlyDictionary<UserRoleType, HashSet<Permission>> Map = new Dictionary<UserRoleType, HashSet<Permission>>
    {
        [UserRoleType.Accountant] = new HashSet<Permission>
        {
            Permission.ViewStudents, Permission.ViewParents, Permission.ViewTeachers,
            Permission.ViewClasses,
            Permission.ViewFees, Permission.ManageFees,
            Permission.ViewPayments, Permission.RecordPayments,
            Permission.ViewDebtors,
            Permission.ViewPayroll, Permission.ManagePayroll,
            Permission.ViewExpenses, Permission.ManageExpenses,
            Permission.ViewIncomes, Permission.ManageIncomes,
            Permission.ViewReports
        },
        [UserRoleType.Registrar] = new HashSet<Permission>
        {
            Permission.ViewStudents, Permission.ManageStudents,
            Permission.ViewParents, Permission.ManageParents,
            Permission.ViewTeachers,
            Permission.ViewClasses, Permission.ViewSubjects,
            Permission.ViewTimetable,
            Permission.ViewStudentAttendance,
            Permission.ViewFees, Permission.ViewDebtors,
            Permission.ViewNotices, Permission.ManageNotices,
            Permission.ViewDocuments, Permission.ManageDocuments,
            Permission.ViewLibrary, Permission.ManageLibrary,
            Permission.ViewReportCards,
            Permission.ViewReports,
            Permission.ViewAcademicYears
        },
        [UserRoleType.Teacher] = new HashSet<Permission>
        {
            Permission.ViewStudents,
            Permission.ViewClasses, Permission.ViewSubjects,
            Permission.ViewTimetable,
            Permission.ViewStudentAttendance, Permission.RecordStudentAttendance,
            Permission.ViewExams, Permission.ViewGrades, Permission.RecordGrades,
            Permission.ViewReportCards,
            Permission.ViewNotices,
            Permission.ViewLibrary
        }
    };

    public static bool Has(UserRoleType role, Permission permission)
    {
        if (role == UserRoleType.Administrator) return true; // full access, always
        return Map.TryGetValue(role, out var perms) && perms.Contains(permission);
    }

    public static IReadOnlySet<Permission> GetPermissions(UserRoleType role)
    {
        if (role == UserRoleType.Administrator) return new HashSet<Permission>(Enum.GetValues<Permission>());
        return Map.TryGetValue(role, out var perms) ? perms : new HashSet<Permission>();
    }
}
