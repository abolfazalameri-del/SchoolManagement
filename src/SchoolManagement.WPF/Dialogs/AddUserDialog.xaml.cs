using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Services;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.WPF.Dialogs;

public partial class AddUserDialog : Window
{
    public AddUserDialog()
    {
        InitializeComponent();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(UsernameBox.Text) || string.IsNullOrWhiteSpace(FullNameBox.Text))
        {
            ShowError("نام کاربری و نام کامل الزامی است.");
            return;
        }
        if (PasswordBox.Password.Length < 6)
        {
            ShowError("رمز عبور باید حداقل ۶ کاراکتر باشد.");
            return;
        }

        var roleTag = (RoleCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Registrar";
        var role = Enum.Parse<UserRoleType>(roleTag);

        SaveButton.IsEnabled = false;
        try
        {
            using var scope = App.Services.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var result = await userService.CreateAsync(new CreateUserRequest(UsernameBox.Text.Trim(), FullNameBox.Text.Trim(), PasswordBox.Password, role));

            if (result.IsFailure)
            {
                ShowError(result.Error ?? "خطا در ایجاد کاربر.");
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
            SaveButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
