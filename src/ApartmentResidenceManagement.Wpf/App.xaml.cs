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
                builder.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
                builder.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // 1. Đăng ký Cơ sở dữ liệu AppDbContext sử dụng Connection String từ appsettings.json
                string connectionString = context.Configuration.GetConnectionString("DefaultConnection") 
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
                
                var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));
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
            bool seedDemoData = _host.Services.GetRequiredService<IConfiguration>()
                .GetValue("Database:SeedDemoData", true);
            await DatabaseInitializer.InitializeAsync(dbContext, seedDemoData);
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
        while (true)
        {
            Domain.Entities.UserAccount? loggedInAccount;
            using (var loginScope = _host.Services.CreateScope())
            {
                var loginWindow = loginScope.ServiceProvider.GetRequiredService<LoginWindow>();
                if (loginWindow.ShowDialog() != true)
                {
                    Shutdown();
                    return;
                }

                loggedInAccount = ((LoginViewModel)loginWindow.DataContext).LoggedInAccount;
            }

            if (loggedInAccount == null)
            {
                Shutdown();
                return;
            }

            // Mỗi phiên đăng nhập có DI scope riêng, giúp giải phóng DbContext khi đăng xuất.
            using var sessionScope = _host.Services.CreateScope();
            var services = sessionScope.ServiceProvider;
            var mainWindow = new MainWindow();
            var mainViewModel = new MainViewModel(
                loggedInAccount,
                services.GetRequiredService<DashboardViewModel>(),
                services.GetRequiredService<ApartmentViewModel>(),
                services.GetRequiredService<ResidentViewModel>(),
                services.GetRequiredService<ResidencyViewModel>(),
                services.GetRequiredService<VehicleViewModel>(),
                services.GetRequiredService<StatisticsViewModel>(),
                services.GetRequiredService<ResidentHomeViewModel>(),
                services.GetRequiredService<MyProfileViewModel>(),
                services.GetRequiredService<MyApartmentViewModel>(),
                services.GetRequiredService<FamilyMembersViewModel>(),
                services.GetRequiredService<MyVehiclesViewModel>(),
                services.GetRequiredService<ResidencyHistoryViewModel>(),
                onLogout: () =>
                {
                    mainWindow.DialogResult = false;
                    mainWindow.Close();
                });

            mainWindow.DataContext = mainViewModel;

            // false là đăng xuất; null là người dùng đóng cửa sổ và muốn thoát ứng dụng.
            if (mainWindow.ShowDialog() != false)
            {
                Shutdown();
                return;
            }
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
