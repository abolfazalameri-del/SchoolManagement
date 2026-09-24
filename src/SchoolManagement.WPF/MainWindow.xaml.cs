using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Application.Authorization;
using SchoolManagement.Domain.Enums;
using SchoolManagement.WPF.Views;

namespace SchoolManagement.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (App.CurrentSession is not null)
            CurrentUserText.Text = $"{App.CurrentSession.FullName} — {RoleLabel(App.CurrentSession.Role)}";

        using var scope = App.Services.CreateScope();
        var authz = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        NavClasses.Visibility = authz.HasPermission(Permission.ViewClasses) ? Visibility.Visible : Visibility.Collapsed;
        NavStudents.Visibility = authz.HasPermission(Permission.ViewStudents) ? Visibility.Visible : Visibility.Collapsed;
        NavAttendance.Visibility = authz.HasPermission(Permission.ViewStudentAttendance) ? Visibility.Visible : Visibility.Collapsed;
        NavFees.Visibility = authz.HasPermission(Permission.ViewFees) ? Visibility.Visible : Visibility.Collapsed;
        NavUsers.Visibility = authz.HasPermission(Permission.ViewUsers) ? Visibility.Visible : Visibility.Collapsed;

        NavDashboard_Click(this, new RoutedEventArgs());
    }

    private static string RoleLabel(UserRoleType role) => role switch
    {
        UserRoleType.Administrator => "مدیر مکتب",
        UserRoleType.Accountant => "محاسب",
        UserRoleType.Registrar => "منشی",
        UserRoleType.Teacher => "معلم",
        _ => role.ToString()
    };

    private void NavDashboard_Click(object sender, RoutedEventArgs e) => MainContent.Content = new DashboardPage();
    private void NavClasses_Click(object sender, RoutedEventArgs e) => MainContent.Content = new ClassesPage();
    private void NavStudents_Click(object sender, RoutedEventArgs e) => MainContent.Content = new StudentsPage();
    private void NavAttendance_Click(object sender, RoutedEventArgs e) => MainContent.Content = new AttendancePage();
    private void NavFees_Click(object sender, RoutedEventArgs e) => MainContent.Content = new FeesPage();
    private void NavUsers_Click(object sender, RoutedEventArgs e) => MainContent.Content = new UsersPage();

    private void NavLogout_Click(object sender, RoutedEventArgs e)
    {
        var currentUserContext = App.Services.GetRequiredService<Infrastructure.Security.CurrentUserContext>();
        currentUserContext.SignOut();
        App.SetSession(null);

        var loginWindow = new LoginWindow();
        var result = loginWindow.ShowDialog();
        if (result == true && loginWindow.LoggedInUser is not null)
        {
            App.SetSession(loginWindow.LoggedInUser);
            currentUserContext.SignIn(loginWindow.LoggedInUser.UserId, loginWindow.LoggedInUser.FullName, loginWindow.LoggedInUser.Role);
            MainWindow_Loaded(this, new RoutedEventArgs());
        }
        else
        {
            Application.Current.Shutdown();
        }
    }
}
