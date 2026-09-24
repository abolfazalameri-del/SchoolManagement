using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Services;
using SchoolManagement.Infrastructure;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.WPF.Views;

namespace SchoolManagement.WPF;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static LoginResultDto? CurrentSession { get; private set; }

    public static void SetSession(LoginResultDto? session) => CurrentSession = session;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        services.AddInfrastructure();
        services.AddApplication();
        Services = services.BuildServiceProvider();

        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<SchoolManagement.Application.Interfaces.IPasswordHasher>();
            try
            {
                await DbInitializer.InitializeAsync(context, passwordHasher);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"اتصال به پایگاه داده ناموفق بود:\n{ex.Message}", "خطای راه‌اندازی", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
                return;
            }
        }

        var loginWindow = new LoginWindow();
        var loginResult = loginWindow.ShowDialog();

        if (loginResult != true || loginWindow.LoggedInUser is null)
        {
            Shutdown();
            return;
        }

        App.SetSession(loginWindow.LoggedInUser);

        if (CurrentSession.MustChangePassword)
        {
            var changePasswordWindow = new ChangePasswordWindow(CurrentSession.UserId, isForced: true);
            var changed = changePasswordWindow.ShowDialog();
            if (changed != true)
            {
                Shutdown();
                return;
            }
        }

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
