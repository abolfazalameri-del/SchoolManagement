using SchoolManagement.Application.Authorization;
using SchoolManagement.Application.Common;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Interfaces;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Shared;

namespace SchoolManagement.Application.Services;

public interface IParentService
{
    Task<Result<ParentDto>> CreateAsync(CreateParentRequest request, CancellationToken ct = default);
    Task<Result> LinkToStudentAsync(LinkParentToStudentRequest request, CancellationToken ct = default);
    Task<List<ParentDto>> GetForStudentAsync(int studentId, CancellationToken ct = default);
    Task<List<ParentDto>> SearchAsync(string? keyword, CancellationToken ct = default);
}

public class ParentService : IParentService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuthorizationService _authz;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _audit;

    public ParentService(IUnitOfWork uow, IAuthorizationService authz, ICurrentUserContext currentUser, IAuditLogger audit)
    {
        _uow = uow; _authz = authz; _currentUser = currentUser; _audit = audit;
    }

    public async Task<Result<ParentDto>> CreateAsync(CreateParentRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageParents);
        if (string.IsNullOrWhiteSpace(request.FullName))
            return Result.Failure<ParentDto>("نام والدین/سرپرست الزامی است.");

        var parent = new Parent
        {
            FullName = request.FullName,
            Relationship = request.Relationship,
            PhoneNumber = request.PhoneNumber,
            SecondaryPhoneNumber = request.SecondaryPhoneNumber,
            Address = request.Address,
            Occupation = request.Occupation,
            CreatedByUserId = _currentUser.UserId
        };
        await _uow.Repository<Parent>().AddAsync(parent, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync(_currentUser.UserId ?? 0, _currentUser.FullName ?? "?", "Created", nameof(Parent), parent.Id, parent.FullName, ct);

        return Result.Success(ToDto(parent));
    }

    public async Task<Result> LinkToStudentAsync(LinkParentToStudentRequest request, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ManageParents);

        var student = await _uow.Repository<Student>().GetByIdAsync(request.StudentId, ct);
        if (student is null) return Result.Failure("شاگرد یافت نشد.");
        var parent = await _uow.Repository<Parent>().GetByIdAsync(request.ParentId, ct);
        if (parent is null) return Result.Failure("والدین/سرپرست یافت نشد.");

        var existing = await _uow.Repository<StudentParent>().FirstOrDefaultAsync(
            sp => sp.StudentId == request.StudentId && sp.ParentId == request.ParentId, ct);
        if (existing is not null) return Result.Failure("این ارتباط قبلاً ثبت شده است.");

        await _uow.Repository<StudentParent>().AddAsync(new StudentParent
        {
            StudentId = request.StudentId,
            ParentId = request.ParentId,
            IsPrimaryContact = request.IsPrimaryContact,
            CreatedByUserId = _currentUser.UserId
        }, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<List<ParentDto>> GetForStudentAsync(int studentId, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewParents);
        var links = await _uow.Repository<StudentParent>().FindAsync(sp => sp.StudentId == studentId, ct);
        var parentIds = links.Select(l => l.ParentId).ToHashSet();
        var parents = await _uow.Repository<Parent>().FindAsync(p => parentIds.Contains(p.Id), ct);
        return parents.Select(ToDto).ToList();
    }

    public async Task<List<ParentDto>> SearchAsync(string? keyword, CancellationToken ct = default)
    {
        _authz.EnsurePermission(Permission.ViewParents);
        var parents = await _uow.Repository<Parent>().FindAsync(
            p => string.IsNullOrEmpty(keyword) || p.FullName.Contains(keyword) || (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)), ct);
        return parents.OrderBy(p => p.FullName).Select(ToDto).ToList();
    }

    private static ParentDto ToDto(Parent p) => new(p.Id, p.FullName, p.Relationship, p.PhoneNumber, p.Address);
}
