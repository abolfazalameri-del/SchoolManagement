using SchoolManagement.Application.Authorization;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Shared;

namespace SchoolManagement.Application.Services;

public interface IStudentService
{
    Task<Result<StudentDto>> CreateAsync(CreateStudentRequest request, CancellationToken ct = default);
    Task<Result<StudentDto>> UpdateAsync(UpdateStudentRequest request, CancellationToken ct = default);
    Task<Result> WithdrawAsync(int studentId, EnrollmentStatus reason, CancellationToken ct = default);
    Task<Result<StudentDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<StudentDto>> SearchAsync(int? schoolClassId, EnrollmentStatus? status, string? keyword, CancellationToken ct = default);
}

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthorizationService _authz;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _audit;

    public StudentService(IUnitOfWork uow, IAuthorizationService authz, ICurrentUserContext currentUser, IAuditLogger audit)
    {
        _uow = uow;
        _authz = authz;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<Result<StudentDto>> CreateAsync(CreateStudentRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageStudents);

        if (string.IsNullOrWhiteSpace(request.StudentNumber))
            return Result.Failure<StudentDto>("شماره شاگرد الزامی است.");
        if (string.IsNullOrWhiteSpace(request.FullName))
            return Result.Failure<StudentDto>("نام شاگرد الزامی است.");

        var duplicate = await _uow.Repository<Student>().FirstOrDefaultAsync(s => s.StudentNumber == request.StudentNumber, ct);
        if (duplicate is not null)
            return Result.Failure<StudentDto>("این شماره شاگرد قبلاً ثبت شده است.");

        var schoolClass = await _uow.Repository<SchoolClass>().GetByIdAsync(request.SchoolClassId, ct);
        if (schoolClass is null)
            return Result.Failure<StudentDto>("صنف انتخاب‌شده یافت نشد.");

        var currentInClass = await _uow.Repository<Student>().CountAsync(
            s => s.SchoolClassId == request.SchoolClassId && s.Status == EnrollmentStatus.Active, ct);
        if (schoolClass.Capacity > 0 && currentInClass >= schoolClass.Capacity)
            return Result.Failure<StudentDto>($"ظرفیت صنف «{schoolClass.Name}» تکمیل است ({schoolClass.Capacity} نفر).");

        var student = new Student
        {
            StudentNumber = request.StudentNumber,
            FullName = request.FullName,
            FatherName = request.FatherName,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            NationalId = request.NationalId,
            Address = request.Address,
            SchoolClassId = request.SchoolClassId,
            EnrollmentDate = DateTime.UtcNow,
            Status = EnrollmentStatus.Active,
            CreatedByUserId = _currentUser.UserId
        };

        await _uow.Repository<Student>().AddAsync(student, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "Created", nameof(Student), student.Id, student.FullName, ct);

        return Result.Success(ToDto(student, schoolClass.Name));
    }

    public async Task<Result<StudentDto>> UpdateAsync(UpdateStudentRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageStudents);

        var student = await _uow.Repository<Student>().GetByIdAsync(request.Id, ct);
        if (student is null) return Result.Failure<StudentDto>("شاگرد یافت نشد.");

        var schoolClass = await _uow.Repository<SchoolClass>().GetByIdAsync(request.SchoolClassId, ct);
        if (schoolClass is null) return Result.Failure<StudentDto>("صنف انتخاب‌شده یافت نشد.");

        student.FullName = request.FullName;
        student.FatherName = request.FatherName;
        student.Gender = request.Gender;
        student.DateOfBirth = request.DateOfBirth;
        student.Address = request.Address;
        student.SchoolClassId = request.SchoolClassId;
        student.Status = request.Status;
        student.ModifiedByUserId = _currentUser.UserId;

        _uow.Repository<Student>().Update(student);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "Updated", nameof(Student), student.Id, null, ct);

        return Result.Success(ToDto(student, schoolClass.Name));
    }

    public async Task<Result> WithdrawAsync(int studentId, EnrollmentStatus reason, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageStudents);

        var student = await _uow.Repository<Student>().GetByIdAsync(studentId, ct);
        if (student is null) return Result.Failure("شاگرد یافت نشد.");

        student.Status = reason;
        student.ModifiedByUserId = _currentUser.UserId;
        _uow.Repository<Student>().Update(student);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "StatusChanged", nameof(Student), student.Id, reason.ToString(), ct);

        return Result.Success();
    }

    public async Task<Result<StudentDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewStudents);
        var student = await _uow.Repository<Student>().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (student is null) return Result.Failure<StudentDto>("شاگرد یافت نشد.");
        var schoolClass = await _uow.Repository<SchoolClass>().GetByIdAsync(student.SchoolClassId, ct);
        return Result.Success(ToDto(student, schoolClass?.Name ?? ""));
    }

    public async Task<List<StudentDto>> SearchAsync(int? schoolClassId, EnrollmentStatus? status, string? keyword, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewStudents);

        var students = await _uow.Repository<Student>().FindAsync(s =>
            (!schoolClassId.HasValue || s.SchoolClassId == schoolClassId.Value) &&
            (!status.HasValue || s.Status == status.Value) &&
            (string.IsNullOrEmpty(keyword) || s.FullName.Contains(keyword) || s.StudentNumber.Contains(keyword)),
            ct);

        var classes = await _uow.Repository<SchoolClass>().GetAllAsync(ct);
        var classNames = classes.ToDictionary(c => c.Id, c => c.Name);

        return students
            .Select(s => ToDto(s, classNames.GetValueOrDefault(s.SchoolClassId, "")))
            .OrderBy(s => s.FullName)
            .ToList();
    }

    private static StudentDto ToDto(Student s, string className) => new(
        s.Id, s.StudentNumber, s.FullName, s.FatherName, s.Gender, s.DateOfBirth,
        s.Address, s.EnrollmentDate, s.Status, s.SchoolClassId, className);
}
