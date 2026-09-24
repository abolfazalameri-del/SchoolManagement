using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Services;
using SchoolManagement.Domain.Enums;
using Xunit;

namespace SchoolManagement.Tests;

/// <summary>
/// Master prompt section 20: permission must be enforced in the Application layer, not only by
/// hiding a button in the UI. These tests call services directly — no UI involved at all — and
/// confirm a forbidden call is actually blocked.
/// </summary>
public class AuthorizationTests : IDisposable
{
    private readonly TestDbFixture _fixture;

    public AuthorizationTests()
    {
        _fixture = new TestDbFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task Teacher_CannotManageFees()
    {
        _fixture.SignInAs(UserRoleType.Teacher);
        var feeService = new FeeService(_fixture.UnitOfWork, _fixture.Authorization, _fixture.CurrentUser, _fixture.Audit);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            feeService.CreateFeeStructureAsync(new CreateFeeStructureRequest("فیس ماهانه", 1, "1405", 500, true)));
    }

    [Fact]
    public async Task Accountant_CannotManageStudents()
    {
        _fixture.SignInAs(UserRoleType.Accountant);
        var studentService = new StudentService(_fixture.UnitOfWork, _fixture.Authorization, _fixture.CurrentUser, _fixture.Audit);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            studentService.CreateAsync(new CreateStudentRequest("X-1", "نام", null, Gender.Male, null, null, null, 1)));
    }

    [Fact]
    public async Task Administrator_CanManageEverything()
    {
        _fixture.SignInAs(UserRoleType.Administrator);
        var academicsService = new AcademicsService(_fixture.UnitOfWork, _fixture.Authorization, _fixture.CurrentUser, _fixture.Audit);

        var result = await academicsService.CreateClassAsync(new CreateSchoolClassRequest("صنف تست", 1, "الف", "1405", 20, null));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UnauthenticatedContext_HasNoPermissions()
    {
        // CurrentUser never signed in — simulates calling a service with no active session at all.
        var studentService = new StudentService(_fixture.UnitOfWork, _fixture.Authorization, _fixture.CurrentUser, _fixture.Audit);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            studentService.CreateAsync(new CreateStudentRequest("X-2", "نام", null, Gender.Male, null, null, null, 1)));
    }
}
