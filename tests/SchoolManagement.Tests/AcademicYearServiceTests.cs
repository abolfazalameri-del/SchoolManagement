using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Services;
using Xunit;

namespace SchoolManagement.Tests;

public class AcademicYearServiceTests : IDisposable
{
    private readonly TestDbFixture _fixture;
    private readonly AcademicYearService _service;

    public AcademicYearServiceTests()
    {
        _fixture = new TestDbFixture();
        _fixture.SignInAsAdmin();
        _service = new AcademicYearService(_fixture.UnitOfWork, _fixture.Authorization, _fixture.CurrentUser, _fixture.Audit);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task Create_WithValidData_Succeeds()
    {
        var result = await _service.CreateAsync(new CreateAcademicYearRequest("1405", new DateTime(2026, 3, 21), new DateTime(2027, 3, 20)));

        Assert.True(result.IsSuccess);
        Assert.Equal("1405", result.Value.Name);
        Assert.False(result.Value.IsCurrent);
        Assert.False(result.Value.IsClosed);
    }

    [Fact]
    public async Task Create_DuplicateName_Fails()
    {
        await _service.CreateAsync(new CreateAcademicYearRequest("1405", new DateTime(2026, 3, 21), new DateTime(2027, 3, 20)));
        var second = await _service.CreateAsync(new CreateAcademicYearRequest("1405", new DateTime(2027, 3, 21), new DateTime(2028, 3, 20)));

        Assert.True(second.IsFailure);
    }

    [Fact]
    public async Task Create_EmptyName_Fails()
    {
        var result = await _service.CreateAsync(new CreateAcademicYearRequest("", DateTime.Today, DateTime.Today.AddYears(1)));
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Create_EndDateBeforeStartDate_Fails()
    {
        var result = await _service.CreateAsync(new CreateAcademicYearRequest("1406", new DateTime(2027, 3, 21), new DateTime(2026, 3, 20)));
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task SetCurrent_UnsetsPreviousCurrentYear()
    {
        var year1 = await _service.CreateAsync(new CreateAcademicYearRequest("1404", new DateTime(2025, 3, 21), new DateTime(2026, 3, 20)));
        var year2 = await _service.CreateAsync(new CreateAcademicYearRequest("1405", new DateTime(2026, 3, 21), new DateTime(2027, 3, 20)));

        await _service.SetCurrentAsync(year1.Value.Id);
        var afterFirst = await _service.GetCurrentAsync();
        Assert.Equal("1404", afterFirst!.Name);

        await _service.SetCurrentAsync(year2.Value.Id);
        var afterSecond = await _service.GetCurrentAsync();
        Assert.Equal("1405", afterSecond!.Name);

        // Only one year should ever be current — this is also enforced by a DB-level partial
        // unique index (Phase 3.6), this test checks the application-visible outcome.
        var all = await _service.GetAllAsync();
        Assert.Single(all, y => y.IsCurrent);
    }

    [Fact]
    public async Task SetCurrent_OnClosedYear_Fails()
    {
        var year = await _service.CreateAsync(new CreateAcademicYearRequest("1403", new DateTime(2024, 3, 21), new DateTime(2025, 3, 20)));
        await _service.SetCurrentAsync(year.Value.Id);
        // Move current elsewhere first so 1403 is no longer current, then close it.
        var other = await _service.CreateAsync(new CreateAcademicYearRequest("1404b", new DateTime(2025, 3, 21), new DateTime(2026, 3, 20)));
        await _service.SetCurrentAsync(other.Value.Id);
        await _service.CloseAsync(year.Value.Id);

        var result = await _service.SetCurrentAsync(year.Value.Id);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Close_CurrentYear_Fails()
    {
        var year = await _service.CreateAsync(new CreateAcademicYearRequest("1405", new DateTime(2026, 3, 21), new DateTime(2027, 3, 20)));
        await _service.SetCurrentAsync(year.Value.Id);

        var result = await _service.CloseAsync(year.Value.Id);
        Assert.True(result.IsFailure);
    }
}
