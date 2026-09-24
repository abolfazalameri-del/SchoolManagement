using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Services;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Infrastructure.Security;
using Xunit;

namespace SchoolManagement.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly TestDbFixture _fixture;
    private readonly AuthService _authService;
    private readonly UserService _userService;
    private readonly PasswordHasher _passwordHasher;

    public AuthServiceTests()
    {
        _fixture = new TestDbFixture();
        _fixture.SignInAsAdmin();
        _passwordHasher = new PasswordHasher();
        _authService = new AuthService(_fixture.UnitOfWork, _passwordHasher, _fixture.Audit);
        _userService = new UserService(_fixture.UnitOfWork, _fixture.Authorization, _fixture.CurrentUser, _passwordHasher, _fixture.Audit);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task Login_CorrectPassword_Succeeds()
    {
        await _userService.CreateAsync(new CreateUserRequest("teacher1", "معلم یک", "Passw0rd!", UserRoleType.Teacher));

        var result = await _authService.LoginAsync(new LoginRequest("teacher1", "Passw0rd!"));

        Assert.True(result.IsSuccess);
        Assert.Equal(UserRoleType.Teacher, result.Value.Role);
        Assert.True(result.Value.MustChangePassword); // CreateAsync always forces a change on first login
    }

    [Fact]
    public async Task Login_WrongPassword_FailsWithGenericMessage()
    {
        await _userService.CreateAsync(new CreateUserRequest("teacher2", "معلم دو", "Passw0rd!", UserRoleType.Teacher));

        var wrongPassword = await _authService.LoginAsync(new LoginRequest("teacher2", "WrongPass!"));
        var wrongUsername = await _authService.LoginAsync(new LoginRequest("no-such-user", "Passw0rd!"));

        Assert.True(wrongPassword.IsFailure);
        Assert.True(wrongUsername.IsFailure);
        // Section 19: must not reveal WHICH was wrong — both failure messages must be identical.
        Assert.Equal(wrongUsername.Error, wrongPassword.Error);
    }

    [Fact]
    public async Task Login_InactiveUser_Fails()
    {
        var created = await _userService.CreateAsync(new CreateUserRequest("teacher3", "معلم سه", "Passw0rd!", UserRoleType.Teacher));
        await _userService.SetActiveAsync(created.Value.Id, isActive: false);

        var result = await _authService.LoginAsync(new LoginRequest("teacher3", "Passw0rd!"));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void PasswordHasher_NeverStoresPlaintext()
    {
        var (hash, _) = _passwordHasher.Hash("MySecretPassword1");

        Assert.DoesNotContain("MySecretPassword1", hash);
        Assert.True(_passwordHasher.Verify("MySecretPassword1", hash, ""));
        Assert.False(_passwordHasher.Verify("WrongPassword", hash, ""));
    }
}
