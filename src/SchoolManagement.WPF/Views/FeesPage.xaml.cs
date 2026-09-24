using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Services;
using SchoolManagement.WPF.Dialogs;

namespace SchoolManagement.WPF.Views;

public partial class FeesPage : UserControl
{
    public FeesPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        using var scope = App.Services.CreateScope();
        var feeService = scope.ServiceProvider.GetRequiredService<IFeeService>();
        var debtors = await feeService.GetDebtorsReportAsync(null);
        DebtorsGrid.ItemsSource = debtors;
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void RecordPaymentButton_Click(object sender, RoutedEventArgs e)
    {
        if (DebtorsGrid.SelectedItem is not DebtorRowDto debtor)
        {
            MessageBox.Show("لطفاً یک شاگرد بدهکار را از جدول انتخاب کنید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new RecordPaymentDialog(debtor.StudentId, debtor.StudentName) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
            await LoadAsync();
    }
}
