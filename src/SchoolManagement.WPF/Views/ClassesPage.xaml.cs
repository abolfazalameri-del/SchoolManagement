using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.Services;
using SchoolManagement.WPF.Dialogs;

namespace SchoolManagement.WPF.Views;

public partial class ClassesPage : UserControl
{
    public ClassesPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        using var scope = App.Services.CreateScope();
        var academicsService = scope.ServiceProvider.GetRequiredService<IAcademicsService>();
        ClassesGrid.ItemsSource = await academicsService.GetAllClassesAsync();
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddClassDialog { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
            await LoadAsync();
    }
}
