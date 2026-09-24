using SchoolManagement.Application.Authorization;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Shared;

namespace SchoolManagement.Application.Services;

public interface ITeacherService
{
    Task<Result<TeacherDto>> CreateAsync(CreateTeacherRequest request, CancellationToken ct = default);
    Task<Result<TeacherDto>> UpdateAsync(UpdateTeacherRequest request, CancellationToken ct = default);
    Task<List<TeacherDto>> SearchAsync(string? keyword, CancellationToken ct = default);
    Task<Result<TeacherDto>> GetByIdAsync(int id, CancellationToken ct = default);
}

public class TeacherService : ITeacherService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthorizationService _authz;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _audit;

    public TeacherService(IUnitOfWork uow, IAuthorizationService authz, ICurrentUserContext currentUser, IAuditLogger audit)
    {
        _uow = uow; _authz = authz; _currentUser = currentUser; _audit = audit;
    }

    public async Task<Result<TeacherDto>> CreateAsync(CreateTeacherRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageTeachers);
        if (string.IsNullOrWhiteSpace(request.FullName))
            return Result.Failure<TeacherDto>("نام معلم الزامی است.");
        if (request.MonthlySalary < 0)
            return Result.Failure<TeacherDto>("معاش نمی‌تواند منفی باشد.");

        var teacher = new Teacher
        {
            FullName = request.FullName,
            Gender = request.Gender,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            Qualification = request.Qualification,
            MonthlySalary = request.MonthlySalary,
            HireDate = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId
        };
        await _uow.Repository<Teacher>().AddAsync(teacher, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "Created", nameof(Teacher), teacher.Id, teacher.FullName, ct);

        return Result.Success(ToDto(teacher));
    }

    public async Task<Result<TeacherDto>> UpdateAsync(UpdateTeacherRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageTeachers);
        var teacher = await _uow.Repository<Teacher>().GetByIdAsync(request.Id, ct);
        if (teacher is null) return Result.Failure<TeacherDto>("معلم یافت نشد.");

        teacher.FullName = request.FullName;
        teacher.PhoneNumber = request.PhoneNumber;
        teacher.Qualification = request.Qualification;
        teacher.MonthlySalary = request.MonthlySalary;
        teacher.Status = request.Status;
        teacher.ModifiedByUserId = _currentUser.UserId;

        _uow.Repository<Teacher>().Update(teacher);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "Updated", nameof(Teacher), teacher.Id, null, ct);

        return Result.Success(ToDto(teacher));
    }

    public async Task<List<TeacherDto>> SearchAsync(string? keyword, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewTeachers);
        var teachers = await _uow.Repository<Teacher>().FindAsync(
            t => string.IsNullOrEmpty(keyword) || t.FullName.Contains(keyword), ct);
        return teachers.OrderBy(t => t.FullName).Select(ToDto).ToList();
    }

    public async Task<Result<TeacherDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewTeachers);
        var teacher = await _uow.Repository<Teacher>().GetByIdAsync(id, ct);
        return teacher is null ? Result.Failure<TeacherDto>("معلم یافت نشد.") : Result.Success(ToDto(teacher));
    }

    private static TeacherDto ToDto(Teacher t) => new(t.Id, t.FullName, t.Gender, t.PhoneNumber, t.Qualification, t.MonthlySalary, t.Status);
}
