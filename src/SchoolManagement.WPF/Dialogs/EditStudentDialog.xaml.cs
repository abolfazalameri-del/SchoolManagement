using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Services;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.WPF.Dialogs;

public partial class EditStudentDialog : Window
{
    private readonly StudentDto _student;

    public EditStudentDialog(StudentDto student)
    {
        InitializeComponent();
        _student = student;
        Loaded += async (_, _) => await InitializeAsync();
    }

    private async System.Threading.Tasks.Task InitializeAsync()
    {
        FullNameBox.Text = _student.FullName;
        FatherNameBox.Text = _student.FatherName;
        AddressBox.Text = _student.Address;

        using var scope = App.Services.CreateScope();
        var academicsService = scope.ServiceProvider.GetRequiredService<IAcademicsService>();
        var classes = await academicsService.GetAllClassesAsync();
        ClassCombo.ItemsSource = classes;
        ClassCombo.SelectedValue = _student.SchoolClassId;

        foreach (ComboBoxItem item in StatusCombo.Items)
        {
            if (item.Tag?.ToString() == _student.Status.ToString())
            {
                StatusCombo.SelectedItem = item;
                break;
            }
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(FullNameBox.Text))
        {
            ShowError("نام کامل الزامی است.");
            return;
        }
        if (ClassCombo.SelectedValue is not int classId)
        {
            ShowError("لطفاً یک صنف انتخاب کنید.");
            return;
        }
        var status = Enum.Parse<EnrollmentStatus>((StatusCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Active");

        SaveButton.IsEnabled = false;
        try
        {
            using var scope = App.Services.CreateScope();
            var studentService = scope.ServiceProvider.GetRequiredService<IStudentService>();
            var result = await studentService.UpdateAsync(new UpdateStudentRequest(
                _student.Id, FullNameBox.Text.Trim(),
                string.IsNullOrWhiteSpace(FatherNameBox.Text) ? null : FatherNameBox.Text.Trim(),
                _student.Gender, _student.DateOfBirth,
                string.IsNullOrWhiteSpace(AddressBox.Text) ? null : AddressBox.Text.Trim(),
                classId, status));

            if (result.IsFailure)
            {
                ShowError(result.Error ?? "خطا در ذخیره تغییرات.");
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
