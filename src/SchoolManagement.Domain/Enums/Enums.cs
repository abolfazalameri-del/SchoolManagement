namespace SchoolManagement.Domain.Enums;

/// <summary>System-wide user roles. Drives RBAC via RolePermissions in the Application layer.</summary>
public enum UserRoleType
{
    Administrator = 1,  // مدیر مکتب — full access
    Accountant = 2,     // محاسب — fees, payments, payroll, expenses, incomes
    Registrar = 3,      // منشی — students, parents, enrollment, documents, notices
    Teacher = 4         // معلم — own classes: attendance, grades, timetable (read)
}

public enum Gender
{
    Male = 1,
    Female = 2
}

public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3,
    Excused = 4
}

public enum EnrollmentStatus
{
    Active = 1,
    Suspended = 2,
    Withdrawn = 3,
    Graduated = 4,
    Expelled = 5
}

public enum ExamType
{
    Quiz = 1,
    Midterm = 2,
    Final = 3,
    Makeup = 4
}

public enum FeeInvoiceStatus
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Waived = 4,
    Overdue = 5
}

public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    MobileWallet = 3,
    Cheque = 4
}

public enum PayrollStatus
{
    Pending = 1,
    Paid = 2,
    Cancelled = 3
}

public enum StaffEmploymentStatus
{
    Active = 1,
    OnLeave = 2,
    Terminated = 3
}

public enum AssetCondition
{
    New = 1,
    Good = 2,
    NeedsRepair = 3,
    Disposed = 4
}

public enum LibraryLoanStatus
{
    Borrowed = 1,
    Returned = 2,
    Overdue = 3,
    Lost = 4
}

public enum NoticeAudience
{
    AllStaff = 1,
    AllParents = 2,
    SpecificClass = 3,
    Everyone = 4
}

public enum DocumentOwnerType
{
    Student = 1,
    Staff = 2,
    General = 3
}

public enum Weekday
{
    Saturday = 1,
    Sunday = 2,
    Monday = 3,
    Tuesday = 4,
    Wednesday = 5,
    Thursday = 6,
    Friday = 7
}

/// <summary>How a student's year ended — recorded once, during the promotion process, never changed casually.</summary>
public enum PromotionOutcome
{
    Promoted = 1,
    Repeated = 2,
    Graduated = 3,
    Withdrawn = 4,
    Transferred = 5
}

/// <summary>Every distinct, checkable permission in the system. RolePermissions maps each UserRoleType to a set of these.</summary>
public enum Permission
{
    // Students & people
    ViewStudents, ManageStudents,
    ViewParents, ManageParents,
    ViewTeachers, ManageTeachers,
    ViewUsers, ManageUsers,

    // Academics
    ViewClasses, ManageClasses,
    ViewSubjects, ManageSubjects,
    ViewTimetable, ManageTimetable,
    ViewStudentAttendance, RecordStudentAttendance,
    ViewStaffAttendance, RecordStaffAttendance,
    ViewExams, ManageExams,
    ViewGrades, RecordGrades,
    ViewReportCards, IssueReportCards,

    // Finance
    ViewFees, ManageFees,
    ViewPayments, RecordPayments,
    ViewDebtors,
    ViewPayroll, ManagePayroll,
    ViewExpenses, ManageExpenses,
    ViewIncomes, ManageIncomes,

    // Facilities
    ViewNotices, ManageNotices,
    ViewDocuments, ManageDocuments,
    ViewLibrary, ManageLibrary,
    ViewAssets, ManageAssets,

    // System
    ViewReports,
    ViewAuditLog,
    ManageBackup,
    ManageSettings,
    ViewAcademicYears, ManageAcademicYears, PromoteStudents
}
