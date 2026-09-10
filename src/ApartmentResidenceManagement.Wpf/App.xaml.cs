using System;
using System.IO;
using System.Windows;
using ApartmentResidenceManagement.Domain.Interfaces;
using ApartmentResidenceManagement.Infrastructure.Data;
using ApartmentResidenceManagement.Infrastructure.Repositories;
using ApartmentResidenceManagement.Application.Security;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Wpf.ViewModels;
using ApartmentResidenceManagement.Wpf.Views;
using ApartmentResidenceManagement.Wpf.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApartmentResidenceManagement.Wpf;

public partial class App : System.Windows.Application
{
    private readonly IHost _host;

    public App()
    {
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, builder) =>
            {
                // Use the directory containing the executable (the WPF project folder) as the base path
                var exePath = System.AppContext.BaseDirectory;
                builder.SetBasePath(exePath);
                builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // 1. Đăng ký Cơ sở dữ liệu AppDbContext sử dụng Connection String từ appsettings.json
                string connectionString = context.Configuration.GetConnectionString("DefaultConnection") 
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
                
                var serverVersion = new MySqlServerVersion(new Version(8, 4, 8));
                services.AddDbContext<AppDbContext>(options =>
                    options.UseMySql(connectionString, serverVersion, mySqlOptions => mySqlOptions.EnableRetryOnFailure()),
                    ServiceLifetime.Transient,
                    ServiceLifetime.Singleton);

                // 2. Đăng ký Core & Infrastructure Services (DAL)
                services.AddTransient<IUnitOfWork, UnitOfWork>();

                // 3. Đăng ký Security Utility
                services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

                // 4. Đăng ký Business Services (BLL)
                services.AddTransient<ApartmentService>();
                services.AddTransient<ResidentService>();
                services.AddTransient<ResidenceService>();
                services.AddTransient<VehicleService>();
                services.AddTransient<AccountService>();

                // 5. Đăng ký Views và ViewModels (Presentation UI)
                services.AddTransient<LoginViewModel>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<ApartmentViewModel>();
                services.AddTransient<ResidentViewModel>();
                services.AddTransient<ResidencyViewModel>();
                services.AddTransient<VehicleViewModel>();
                services.AddTransient<StatisticsViewModel>();
                
                // Resident ViewModels
                services.AddTransient<ResidentHomeViewModel>();
                services.AddTransient<MyProfileViewModel>();
                services.AddTransient<MyApartmentViewModel>();
                services.AddTransient<FamilyMembersViewModel>();
                services.AddTransient<MyVehiclesViewModel>();
                services.AddTransient<ResidencyHistoryViewModel>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        // Tránh tự động tắt ứng dụng khi đóng LoginWindow để mở MainWindow
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // 1. Tự động chạy Migration và nạp dữ liệu Seed Data khi ứng dụng khởi động
        try
        {
            using var scope = _host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DatabaseInitializer.Initialize(dbContext);
        }
        catch (Exception ex)
        {
            NotificationService.Show("Không thể khởi tạo cơ sở dữ liệu. Vui lòng kiểm tra cấu hình kết nối và thử lại.",
                "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error, ex.ToString());
            Shutdown();
            return;
        }

        // 2. Vòng lặp hiển thị màn hình Đăng nhập
        RunLoginLoop();
    }

    private void RunLoginLoop()
    {
        // Lấy LoginWindow từ DI Container
        var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
        var loginResult = loginWindow.ShowDialog();

        if (loginResult == true)
        {
            // Đăng nhập thành công, lấy thông tin tài khoản đã xác thực
            var loginViewModel = (LoginViewModel)loginWindow.DataContext;
            var loggedInAccount = loginViewModel.LoggedInAccount;

            if (loggedInAccount != null)
            {
                // Lấy các con-ViewModels từ DI Container
                var dashboardVm = _host.Services.GetRequiredService<DashboardViewModel>();
                var apartmentVm = _host.Services.GetRequiredService<ApartmentViewModel>();
                var residentVm = _host.Services.GetRequiredService<ResidentViewModel>();
                var residencyVm = _host.Services.GetRequiredService<ResidencyViewModel>();
                var vehicleVm = _host.Services.GetRequiredService<VehicleViewModel>();
                var statisticsVm = _host.Services.GetRequiredService<StatisticsViewModel>();

                var residentHomeVm = _host.Services.GetRequiredService<ResidentHomeViewModel>();
                var myProfileVm = _host.Services.GetRequiredService<MyProfileViewModel>();
                var myApartmentVm = _host.Services.GetRequiredService<MyApartmentViewModel>();
                var familyMembersVm = _host.Services.GetRequiredService<FamilyMembersViewModel>();
                var myVehiclesVm = _host.Services.GetRequiredService<MyVehiclesViewModel>();
                var residencyHistoryVm = _host.Services.GetRequiredService<ResidencyHistoryViewModel>();

                // Khởi tạo MainWindow trước để có thể truyền action đóng cửa sổ
                var mainWindow = new MainWindow(null!); // sẽ gán DataContext sau
                
                // Khởi tạo MainViewModel động dựa trên tài khoản đăng nhập thành công
                var mainViewModel = new MainViewModel(
                    loggedInAccount, 
                    dashboardVm, 
                    apartmentVm, 
                    residentVm, 
                    residencyVm, 
                    vehicleVm, 
                    statisticsVm,
                    residentHomeVm,
                    myProfileVm,
                    myApartmentVm,
                    familyMembersVm,
                    myVehiclesVm,
                    residencyHistoryVm,
                    onLogout: () =>
                    {
                        // Đăng xuất: đặt DialogResult = false để RunLoginLoop nhận biết và mở lại LoginWindow
                        mainWindow.DialogResult = false;
                        mainWindow.Close();
                    });

                mainWindow.DataContext = mainViewModel;
                
                var mainResult = mainWindow.ShowDialog();

                if (mainResult == false)
                {
                    // Người dùng bấm Đăng xuất (Logout), mở lại vòng lặp đăng nhập
                    RunLoginLoop();
                }
                else
                {
                    // Đóng bình thường
                    Shutdown();
                }
            }
        }
        else
        {
            // Đóng cửa sổ đăng nhập mà không đăng nhập thành công (Thoát ứng dụng)
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync();
        }
        base.OnExit(e);
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var logPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "crash.log");
        System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] Dispatcher Exception:\n{e.Exception}\n\n");
        NotificationService.Show("Đã xảy ra lỗi khi xử lý thao tác. Vui lòng thử lại hoặc khởi động lại ứng dụng nếu lỗi tiếp diễn.",
            "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error, e.Exception.ToString());
        e.Handled = true; // Prevents the application from closing immediately, though it might still be in a bad state.
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            NotificationService.Show("Ứng dụng gặp lỗi nghiêm trọng. Vui lòng khởi động lại ứng dụng.",
                "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error, ex.ToString());
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
    {
        NotificationService.Show("Một tác vụ chạy nền chưa hoàn tất. Vui lòng tải lại dữ liệu và thử lại.",
            "Không thể hoàn tất tác vụ", MessageBoxButton.OK, MessageBoxImage.Error, e.Exception.ToString());
        e.SetObserved();
    }
}
