using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Services;

namespace SchoolManagement.WPF.Dialogs;

public partial class AddClassDialog : Window
{
    public AddClassDialog()
    {
        InitializeComponent();
        Loaded += async (_, _) => await PrefillCurrentYearAsync();
    }

    private async System.Threading.Tasks.Task PrefillCurrentYearAsync()
    {
        using var scope = App.Services.CreateScope();
        var academicYearService = scope.ServiceProvider.GetRequiredService<IAcademicYearService>();
        var current = await academicYearService.GetCurrentAsync();
        if (current is not null) AcademicYearBox.Text = current.Name;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            ShowError("نام صنف الزامی است.");
            return;
        }
        if (!int.TryParse(GradeLevelBox.Text, out var gradeLevel))
        {
            ShowError("مقطع باید یک عدد باشد.");
            return;
        }
        if (!int.TryParse(CapacityBox.Text, out var capacity) || capacity <= 0)
        {
            ShowError("ظرفیت باید یک عدد بزرگ‌تر از صفر باشد.");
            return;
        }

        SaveButton.IsEnabled = false;
        try
        {
            using var scope = App.Services.CreateScope();
            var academicsService = scope.ServiceProvider.GetRequiredService<IAcademicsService>();
            var result = await academicsService.CreateClassAsync(new CreateSchoolClassRequest(
                NameBox.Text.Trim(), gradeLevel, SectionBox.Text.Trim(), AcademicYearBox.Text.Trim(), capacity, null));

            if (result.IsFailure)
            {
                ShowError(result.Error ?? "خطا در ایجاد صنف.");
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
