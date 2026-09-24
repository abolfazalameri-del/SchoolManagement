using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.Services;

namespace SchoolManagement.WPF.Views;

public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        InitializeComponent();
        Loaded += DashboardPage_Loaded;
    }

    private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardService>();
            var summary = await dashboardService.GetSummaryAsync();

            StudentsText.Text = summary.TotalActiveStudents.ToString();
            TeachersText.Text = summary.TotalTeachers.ToString();
            ClassesText.Text = summary.TotalClasses.ToString();
            AttendanceText.Text = $"{summary.TodayAttendanceRatePercent:0.#}٪";
            FeesText.Text = summary.ThisMonthFeesCollected.ToString("N0");
            DebtText.Text = summary.TotalOutstandingDebt.ToString("N0");
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"خطا در بارگذاری داشبورد: {ex.Message}";
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
