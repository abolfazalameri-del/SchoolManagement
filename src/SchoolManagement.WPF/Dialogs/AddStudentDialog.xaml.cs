using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Services;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.WPF.Dialogs;

public partial class AddStudentDialog : Window
{
    public AddStudentDialog()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadClassesAsync();
    }

    private async System.Threading.Tasks.Task LoadClassesAsync()
    {
        using var scope = App.Services.CreateScope();
        var academicsService = scope.ServiceProvider.GetRequiredService<IAcademicsService>();
        var classes = await academicsService.GetAllClassesAsync();
        ClassCombo.ItemsSource = classes;
        if (classes.Count > 0) ClassCombo.SelectedIndex = 0;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(StudentNumberBox.Text) || string.IsNullOrWhiteSpace(FullNameBox.Text))
        {
            ShowError("شماره شاگرد و نام کامل الزامی است.");
            return;
        }
        if (ClassCombo.SelectedValue is not int classId)
        {
            ShowError("لطفاً یک صنف انتخاب کنید. (ابتدا باید حداقل یک صنف ایجاد شده باشد)");
            return;
        }

        var gender = (GenderCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() == "Female" ? Gender.Female : Gender.Male;

        SaveButton.IsEnabled = false;
        try
        {
            using var scope = App.Services.CreateScope();
            var studentService = scope.ServiceProvider.GetRequiredService<IStudentService>();
            var result = await studentService.CreateAsync(new CreateStudentRequest(
                StudentNumberBox.Text.Trim(), FullNameBox.Text.Trim(),
                string.IsNullOrWhiteSpace(FatherNameBox.Text) ? null : FatherNameBox.Text.Trim(),
                gender, DateOfBirthPicker.SelectedDate, null,
                string.IsNullOrWhiteSpace(AddressBox.Text) ? null : AddressBox.Text.Trim(),
                classId));

            if (result.IsFailure)
            {
                ShowError(result.Error ?? "خطا در ثبت شاگرد.");
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
