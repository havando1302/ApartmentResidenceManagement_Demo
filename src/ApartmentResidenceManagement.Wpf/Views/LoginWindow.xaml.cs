using System.Windows;
using ApartmentResidenceManagement.Wpf.ViewModels;

namespace ApartmentResidenceManagement.Wpf.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Đăng ký sự kiện đóng Window khi login thành công (được gọi từ ViewModel)
        viewModel.OnLoginSuccess += (sender, e) => {
            DialogResult = true;
            Close();
        };
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }
}
