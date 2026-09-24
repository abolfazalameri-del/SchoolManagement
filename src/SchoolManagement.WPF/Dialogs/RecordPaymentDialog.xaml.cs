using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.DTOs;
using SchoolManagement.Application.Services;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.WPF.Dialogs;

public partial class RecordPaymentDialog : Window
{
    private readonly int _studentId;
    private List<FeeInvoiceDto> _invoices = new();

    public RecordPaymentDialog(int studentId, string studentName)
    {
        InitializeComponent();
        _studentId = studentId;
        StudentNameText.Text = studentName;
        Loaded += async (_, _) => await LoadInvoicesAsync();
    }

    private async System.Threading.Tasks.Task LoadInvoicesAsync()
    {
        using var scope = App.Services.CreateScope();
        var feeService = scope.ServiceProvider.GetRequiredService<IFeeService>();
        var allInvoices = await feeService.GetInvoicesForStudentAsync(_studentId);
        _invoices = allInvoices.Where(i => i.Status != FeeInvoiceStatus.Paid && i.Status != FeeInvoiceStatus.Waived).ToList();
        InvoicesGrid.ItemsSource = _invoices;
        if (_invoices.Count > 0) InvoicesGrid.SelectedIndex = 0;
    }

    private void InvoicesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (InvoicesGrid.SelectedItem is FeeInvoiceDto invoice)
            AmountBox.Text = invoice.Balance.ToString(CultureInfo.InvariantCulture);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        if (InvoicesGrid.SelectedItem is not FeeInvoiceDto invoice)
        {
            ShowError("لطفاً یک فاکتور را انتخاب کنید.");
            return;
        }
        if (!decimal.TryParse(AmountBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            ShowError("مبلغ پرداختی نامعتبر است.");
            return;
        }

        var methodTag = (MethodCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Cash";
        var method = Enum.Parse<PaymentMethod>(methodTag);

        SaveButton.IsEnabled = false;
        try
        {
            using var scope = App.Services.CreateScope();
            var feeService = scope.ServiceProvider.GetRequiredService<IFeeService>();
            var result = await feeService.RecordPaymentAsync(new RecordPaymentRequest(
                invoice.Id, amount, method, string.IsNullOrWhiteSpace(NoteBox.Text) ? null : NoteBox.Text.Trim()));

            if (result.IsFailure)
            {
                ShowError(result.Error ?? "خطا در ثبت پرداخت.");
                return;
            }

            MessageBox.Show($"پرداخت با موفقیت ثبت شد.\nشماره رسید: {result.Value.ReceiptNumber}", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
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
