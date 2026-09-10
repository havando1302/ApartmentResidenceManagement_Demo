using System.Windows;
using ApartmentResidenceManagement.Wpf.ViewModels;

namespace ApartmentResidenceManagement.Wpf.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel? viewModel = null)
    {
        InitializeComponent();
        if (viewModel != null)
            DataContext = viewModel;
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        // Khi nhấn đăng xuất, đóng cửa sổ này và mở lại LoginWindow
        DialogResult = false; 
        Close();
    }
}
