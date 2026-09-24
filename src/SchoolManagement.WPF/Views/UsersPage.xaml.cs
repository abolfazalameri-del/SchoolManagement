using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Services;
using SchoolManagement.WPF.Dialogs;

namespace SchoolManagement.WPF.Views;

public partial class UsersPage : UserControl
{
    public UsersPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        using var scope = App.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var users = await userService.GetAllAsync();
        UsersGrid.ItemsSource = users;
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddUserDialog { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
            await LoadAsync();
    }

    private async void ToggleActiveButton_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not UserDto user)
        {
            MessageBox.Show("لطفاً یک کاربر را انتخاب کنید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var scope = App.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var result = await userService.SetActiveAsync(user.Id, !user.IsActive);

        if (result.IsFailure)
            MessageBox.Show(result.Error, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        else
            await LoadAsync();
    }
}
