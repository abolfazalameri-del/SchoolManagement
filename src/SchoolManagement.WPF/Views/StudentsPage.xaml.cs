using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Services;
using SchoolManagement.WPF.Dialogs;

namespace SchoolManagement.WPF.Views;

public partial class StudentsPage : UserControl
{
    public StudentsPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async System.Threading.Tasks.Task LoadAsync(string? keyword = null)
    {
        using var scope = App.Services.CreateScope();
        var studentService = scope.ServiceProvider.GetRequiredService<IStudentService>();
        var students = await studentService.SearchAsync(null, null, keyword);
        StudentsGrid.ItemsSource = students;
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e) => await LoadAsync(SearchBox.Text.Trim());

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddStudentDialog { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
            await LoadAsync();
    }

    private async void StudentsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (StudentsGrid.SelectedItem is not StudentDto student) return;
        var dialog = new EditStudentDialog(student) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
            await LoadAsync();
    }
}
