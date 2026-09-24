using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Services;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.WPF.Views;

public class AttendanceRowViewModel : INotifyPropertyChanged
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;

    private AttendanceStatus _status;
    public AttendanceStatus Status
    {
        get => _status;
        set { _status = value; OnChanged(nameof(Status)); OnChanged(nameof(StatusDisplay)); }
    }

    public string StatusDisplay => Status switch
    {
        AttendanceStatus.Present => "حاضر",
        AttendanceStatus.Absent => "غایب",
        AttendanceStatus.Late => "تأخیر",
        AttendanceStatus.Excused => "رخصت",
        _ => Status.ToString()
    };

    private string? _note;
    public string? Note
    {
        get => _note;
        set { _note = value; OnChanged(nameof(Note)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class AttendancePage : UserControl
{
    private ObservableCollection<AttendanceRowViewModel> _rows = new();

    public AttendancePage()
    {
        InitializeComponent();
        DatePicker.SelectedDate = DateTime.Today;
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

    private async void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        if (ClassCombo.SelectedValue is not int classId || DatePicker.SelectedDate is not DateTime date)
        {
            MessageBox.Show("لطفاً صنف و تاریخ را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        using var scope = App.Services.CreateScope();
        var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();
        var existingRows = await attendanceService.GetClassAttendanceForDateAsync(classId, date);

        _rows = new ObservableCollection<AttendanceRowViewModel>(existingRows.Select(r => new AttendanceRowViewModel
        {
            StudentId = r.StudentId, StudentName = r.StudentName, Status = r.Status, Note = r.Note
        }));
        AttendanceGrid.ItemsSource = _rows;
    }

    private void MarkPresent_Click(object sender, RoutedEventArgs e) => ApplyToSelected(AttendanceStatus.Present);
    private void MarkAbsent_Click(object sender, RoutedEventArgs e) => ApplyToSelected(AttendanceStatus.Absent);
    private void MarkLate_Click(object sender, RoutedEventArgs e) => ApplyToSelected(AttendanceStatus.Late);
    private void MarkExcused_Click(object sender, RoutedEventArgs e) => ApplyToSelected(AttendanceStatus.Excused);

    private void ApplyToSelected(AttendanceStatus status)
    {
        foreach (var item in AttendanceGrid.SelectedItems)
        {
            if (item is AttendanceRowViewModel row) row.Status = status;
        }
        if (AttendanceGrid.SelectedItems.Count == 0)
            MessageBox.Show("ابتدا حداقل یک شاگرد را در جدول انتخاب کنید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (ClassCombo.SelectedValue is not int classId || DatePicker.SelectedDate is not DateTime date || _rows.Count == 0)
        {
            MessageBox.Show("چیزی برای ذخیره وجود ندارد — ابتدا حاضری را بارگذاری کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SaveButton.IsEnabled = false;
        try
        {
            using var scope = App.Services.CreateScope();
            var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();
            var entries = _rows.Select(r => new StudentAttendanceEntry(r.StudentId, r.Status, r.Note)).ToList();
            var result = await attendanceService.RecordClassAttendanceAsync(new RecordClassAttendanceRequest(classId, date, entries));

            if (result.IsFailure)
                MessageBox.Show(result.Error, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            else
                MessageBox.Show("حاضری با موفقیت ذخیره شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطای غیرمنتظره: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }
}
