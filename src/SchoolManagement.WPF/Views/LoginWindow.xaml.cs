using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Services;
using SchoolManagement.Infrastructure.Security;

namespace SchoolManagement.WPF.Views;

public partial class LoginWindow : Window
{
    public LoginResultDto? LoggedInUser { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
        UsernameBox.Focus();
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) LoginButton_Click(sender, e);
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        LoginButton.IsEnabled = false;

        try
        {
            using var scope = App.Services.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

            var result = await authService.LoginAsync(new LoginRequest(UsernameBox.Text.Trim(), PasswordBox.Password));

            if (result.IsFailure)
            {
                ErrorText.Text = result.Error;
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            // The session (CurrentUserContext) is a Singleton, so signing in here makes the
            // logged-in identity visible to every service resolved afterward, in any scope.
            var currentUserContext = App.Services.GetRequiredService<CurrentUserContext>();
            currentUserContext.SignIn(result.Value.UserId, result.Value.FullName, result.Value.Role);

            LoggedInUser = result.Value;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"خطای غیرمنتظره: {ex.Message}";
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }
}
