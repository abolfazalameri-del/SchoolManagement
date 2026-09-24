using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Services;

namespace SchoolManagement.WPF.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly int _userId;

    public ChangePasswordWindow(int userId, bool isForced)
    {
        InitializeComponent();
        _userId = userId;
        if (isForced)
        {
            TitleText.Text = "این یک رمز موقت است. لطفاً پیش از ادامه، رمز عبور خود را تغییر دهید.";
        }
    }

    private async void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
        {
            ShowError("رمز عبور جدید و تکرار آن مطابقت ندارند.");
            return;
        }

        SubmitButton.IsEnabled = false;
        try
        {
            using var scope = App.Services.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var result = await userService.ChangePasswordAsync(new ChangePasswordRequest(_userId, CurrentPasswordBox.Password, NewPasswordBox.Password));

            if (result.IsFailure)
            {
                ShowError(result.Error ?? "خطا در تغییر رمز عبور.");
                return;
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"خطای غیرمنتظره: {ex.Message}");
        }
        finally
        {
            SubmitButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
