using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly UserAccount _account;
    private ViewModelBase _currentViewModel;

    // Các ViewModels con được tiêm vào (Admin)
    public DashboardViewModel DashboardVm { get; }
    public ApartmentViewModel ApartmentVm { get; }
    public ResidentViewModel ResidentVm { get; }
    public ResidencyViewModel ResidencyVm { get; }
    public VehicleViewModel VehicleVm { get; }
    public StatisticsViewModel StatisticsVm { get; }

    // Các ViewModels con được tiêm vào (Resident)
    public ResidentHomeViewModel ResidentHomeVm { get; }
    public MyProfileViewModel MyProfileVm { get; }
    public MyApartmentViewModel MyApartmentVm { get; }
    public FamilyMembersViewModel FamilyMembersVm { get; }
    public MyVehiclesViewModel MyVehiclesVm { get; }
    public ResidencyHistoryViewModel ResidencyHistoryVm { get; }

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set
        {
            if (SetProperty(ref _currentViewModel, value))
            {
                OnPropertyChanged(nameof(ActiveTab));
            }
        }
    }

    public string ActiveTab => CurrentViewModel?.GetType().Name ?? string.Empty;

    public string WelcomeMessage { get; }
    public string RoleMessage { get; }
    public bool IsAdmin => _account.Role == UserRole.Admin;
    public bool IsResident => !IsAdmin;

    // Navigation Commands (Admin)
    public ICommand ShowDashboardCommand { get; }
    public ICommand ShowApartmentCommand { get; }
    public ICommand ShowResidentCommand { get; }
    public ICommand ShowResidencyCommand { get; }
    public ICommand ShowVehicleCommand { get; }
    public ICommand ShowStatisticsCommand { get; }

    // Navigation Commands (Resident)
    public ICommand ShowResidentHomeCommand { get; }
    public ICommand ShowMyProfileCommand { get; }
    public ICommand ShowMyApartmentCommand { get; }
    public ICommand ShowFamilyMembersCommand { get; }
    public ICommand ShowMyVehiclesCommand { get; }
    public ICommand ShowResidencyHistoryCommand { get; }
    public ICommand LogoutCommand { get; }

    public MainViewModel(
        UserAccount account, 
        DashboardViewModel dashboardVm, 
        ApartmentViewModel apartmentVm, 
        ResidentViewModel residentVm,
        ResidencyViewModel residencyVm,
        VehicleViewModel vehicleVm,
        StatisticsViewModel statisticsVm,
        ResidentHomeViewModel residentHomeVm,
        MyProfileViewModel myProfileVm,
        MyApartmentViewModel myApartmentVm,
        FamilyMembersViewModel familyMembersVm,
        MyVehiclesViewModel myVehiclesVm,
        ResidencyHistoryViewModel residencyHistoryVm,
        Action? onLogout = null)
    {
        _account = account;
        DashboardVm = dashboardVm;
        ApartmentVm = apartmentVm;
        ResidentVm = residentVm;
        ResidencyVm = residencyVm;
        VehicleVm = vehicleVm;
        StatisticsVm = statisticsVm;

        ResidentHomeVm = residentHomeVm;
        MyProfileVm = myProfileVm;
        MyApartmentVm = myApartmentVm;
        FamilyMembersVm = familyMembersVm;
        MyVehiclesVm = myVehiclesVm;
        ResidencyHistoryVm = residencyHistoryVm;

        // Khởi tạo thông tin UserAccount cho Resident ViewModels
        ResidentHomeVm.Initialize(_account);
        MyProfileVm.Initialize(_account);
        MyApartmentVm.Initialize(_account);
        FamilyMembersVm.Initialize(_account);
        MyVehiclesVm.Initialize(_account);
        ResidencyHistoryVm.Initialize(_account);

        if (_account.Role == UserRole.Admin)
        {
            WelcomeMessage = $"Xin chào, {_account.Username}!";
            RoleMessage = "Ban quản lý Tòa nhà (Admin)";
        }
        else
        {
            string displayName = _account.Resident?.FullName ?? _account.Username;
            WelcomeMessage = $"Xin chào cư dân, {displayName}!";
            RoleMessage = $"Cư dân ({_account.Username})";
        }

        // Đặt Tab mặc định ban đầu dựa vào Role
        if (IsAdmin)
        {
            _currentViewModel = DashboardVm;
            _ = DashboardVm.LoadStatsAsync();
        }
        else
        {
            _currentViewModel = ResidentHomeVm;
            _ = ResidentHomeVm.LoadStatsAsync();
        }

        // Khởi tạo các command điều hướng Admin
        ShowDashboardCommand = new RelayCommand(_ => {
            CurrentViewModel = DashboardVm;
            _ = DashboardVm.LoadStatsAsync(); 
        });
        ShowApartmentCommand = new RelayCommand(_ => {
            CurrentViewModel = ApartmentVm;
            _ = ApartmentVm.LoadDataAsync();
        });
        ShowResidentCommand = new RelayCommand(_ => {
            CurrentViewModel = ResidentVm;
            _ = ResidentVm.LoadDataAsync();
        });
        ShowResidencyCommand = new RelayCommand(_ => {
            CurrentViewModel = ResidencyVm;
            _ = ResidencyVm.LoadDataAsync();
        });
        ShowVehicleCommand = new RelayCommand(_ => {
            CurrentViewModel = VehicleVm;
            _ = VehicleVm.LoadDataAsync();
        });
        ShowStatisticsCommand = new RelayCommand(_ => {
            CurrentViewModel = StatisticsVm;
            _ = StatisticsVm.LoadStatisticsAsync();
        });

        // Khởi tạo các command điều hướng Resident
        ShowResidentHomeCommand = new RelayCommand(_ => {
            CurrentViewModel = ResidentHomeVm;
            _ = ResidentHomeVm.LoadStatsAsync();
        });
        ShowMyProfileCommand = new RelayCommand(_ => {
            CurrentViewModel = MyProfileVm;
            _ = MyProfileVm.LoadProfileAsync();
        });
        ShowMyApartmentCommand = new RelayCommand(_ => {
            CurrentViewModel = MyApartmentVm;
            _ = MyApartmentVm.LoadApartmentDataAsync();
        });
        ShowFamilyMembersCommand = new RelayCommand(_ => {
            CurrentViewModel = FamilyMembersVm;
            _ = FamilyMembersVm.LoadFamilyMembersAsync();
        });
        ShowMyVehiclesCommand = new RelayCommand(_ => {
            CurrentViewModel = MyVehiclesVm;
            _ = MyVehiclesVm.LoadMyVehiclesAsync();
        });
        ShowResidencyHistoryCommand = new RelayCommand(_ => {
            CurrentViewModel = ResidencyHistoryVm;
            _ = ResidencyHistoryVm.LoadHistoryAsync();
        });

        // Command Đăng xuất: gọi callback để đóng MainWindow từ App.xaml.cs
        LogoutCommand = new RelayCommand(_ => onLogout?.Invoke());
    }
}
