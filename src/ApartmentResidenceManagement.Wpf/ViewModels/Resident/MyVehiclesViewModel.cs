using ApartmentResidenceManagement.Wpf.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class MyVehiclesViewModel : ViewModelBase
{
    private UserAccount? _account;
    private readonly VehicleService _vehicleService;
    private readonly ResidenceService _residenceService;

    private List<Vehicle> _allMyVehicles = new();
    private ObservableCollection<Vehicle> _vehicles = new();

    // Lọc & Tìm kiếm
    private string _searchText = string.Empty;
    private string _selectedTypeFilter = "Tất cả";

    // Thao tác Form
    private Vehicle? _selectedVehicle;
    private Vehicle _editingVehicle = new();
    private bool _isFormOpen;
    private string _formTitle = "ĐĂNG KÝ XE CỦA TÔI";

    // Báo lỗi/Thành công
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isLoading;
    private bool _hasApartment;

    #region Properties
    public ObservableCollection<Vehicle> Vehicles
    {
        get => _vehicles;
        set => SetProperty(ref _vehicles, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    public string SelectedTypeFilter
    {
        get => _selectedTypeFilter;
        set
        {
            if (SetProperty(ref _selectedTypeFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public List<string> TypeFilterOptions { get; } = new() { "Tất cả", "Xe máy", "Ô tô", "Xe đạp" };

    public Vehicle? SelectedVehicle
    {
        get => _selectedVehicle;
        set => SetProperty(ref _selectedVehicle, value);
    }

    public Vehicle EditingVehicle
    {
        get => _editingVehicle;
        set => SetProperty(ref _editingVehicle, value);
    }

    public bool IsFormOpen
    {
        get => _isFormOpen;
        set => SetProperty(ref _isFormOpen, value);
    }

    public string FormTitle
    {
        get => _formTitle;
        set => SetProperty(ref _formTitle, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                HasError = !string.IsNullOrEmpty(value);
            }
        }
    }

    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool HasApartment
    {
        get => _hasApartment;
        set => SetProperty(ref _hasApartment, value);
    }

    public List<VehicleType> VehicleTypes { get; } = Enum.GetValues(typeof(VehicleType)).Cast<VehicleType>().ToList();
    #endregion

    #region Commands
    public ICommand LoadVehiclesCommand { get; }
    public ICommand OpenAddFormCommand { get; }
    public ICommand OpenEditFormCommand { get; }
    public ICommand SaveVehicleCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand DeleteVehicleCommand { get; }
    #endregion

    public MyVehiclesViewModel(VehicleService vehicleService, ResidenceService residenceService)
    {
        _vehicleService = vehicleService;
        _residenceService = residenceService;
        LoadVehiclesCommand = new RelayCommand(async _ => await LoadMyVehiclesAsync());
        OpenAddFormCommand = new RelayCommand(_ => OpenAddForm());
        OpenEditFormCommand = new RelayCommand(v => { if (v is Vehicle veh) OpenEditForm(veh); });
        SaveVehicleCommand = new RelayCommand(async _ => await SaveVehicleAsync());
        CancelFormCommand = new RelayCommand(_ => CloseForm());
        DeleteVehicleCommand = new RelayCommand(async v => await DeleteVehicleAsync(v));
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
        _ = LoadMyVehiclesAsync();
    }

    public async Task LoadMyVehiclesAsync()
    {
        if (_account?.Resident == null)
        {
            HasApartment = false;
            return;
        }

        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            var activeResidence = await _residenceService.GetActiveResidenceByResidentIdAsync(_account.Resident.Id);
            if (activeResidence != null && activeResidence.Apartment != null)
            {
                HasApartment = true;
                
                var list = await _vehicleService.GetVehiclesByOwnerIdAsync(_account.Resident.Id);
                _allMyVehicles = list.ToList();
                ApplyFilters();
            }
            else
            {
                HasApartment = false;
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Đã xảy ra lỗi khi lấy danh sách xe từ database.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allMyVehicles.AsEnumerable();

        if (SelectedTypeFilter == "Xe máy")
        {
            filtered = filtered.Where(v => v.VehicleType == VehicleType.Moto);
        }
        else if (SelectedTypeFilter == "Ô tô")
        {
            filtered = filtered.Where(v => v.VehicleType == VehicleType.Car);
        }
        else if (SelectedTypeFilter == "Xe đạp")
        {
            filtered = filtered.Where(v => v.VehicleType == VehicleType.Bicycle);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string keyword = SearchText.Trim().ToLower();
            filtered = filtered.Where(v => 
                v.LicensePlate.ToLower().Contains(keyword) || 
                (v.Brand != null && v.Brand.ToLower().Contains(keyword)));
        }

        Vehicles.Clear();
        foreach (var v in filtered)
        {
            Vehicles.Add(v);
        }
    }

    private void OpenAddForm()
    {
        ErrorMessage = string.Empty;
        FormTitle = "ĐĂNG KÝ PHƯƠNG TIỆN MỚI";
        EditingVehicle = new Vehicle
        {
            VehicleType = VehicleType.Moto,
            OwnerId = _account?.Resident?.Id ?? 0
        };
        IsFormOpen = true;
    }

    private void OpenEditForm(Vehicle vehicle)
    {
        ErrorMessage = string.Empty;
        FormTitle = $"SỬA PHƯƠNG TIỆN {vehicle.LicensePlate}";
        EditingVehicle = new Vehicle
        {
            Id = vehicle.Id,
            LicensePlate = vehicle.LicensePlate,
            VehicleType = vehicle.VehicleType,
            Brand = vehicle.Brand,
            OwnerId = vehicle.OwnerId
        };
        IsFormOpen = true;
    }

    private void CloseForm()
    {
        IsFormOpen = false;
        ErrorMessage = string.Empty;
        SelectedVehicle = null;
    }

    private async Task SaveVehicleAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(EditingVehicle.LicensePlate))
        {
            ErrorMessage = "Vui lòng nhập biển số xe.";
            return;
        }

        if (EditingVehicle.OwnerId == 0)
        {
            ErrorMessage = "Không thể xác định chủ sở hữu phương tiện.";
            return;
        }

        try
        {
            if (EditingVehicle.Id == 0)
            {
                await _vehicleService.CreateVehicleAsync(EditingVehicle);
            }
            else
            {
                await _vehicleService.UpdateVehicleAsync(EditingVehicle);
            }

            IsFormOpen = false;
            await LoadMyVehiclesAsync();
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi hệ thống khi đăng ký/cập nhật phương tiện.";
        }
    }

    private async Task DeleteVehicleAsync(object? parameter)
    {
        if (parameter is not Vehicle vehicle) return;
        ErrorMessage = string.Empty;

        var result = NotificationService.Show(
            $"Bạn có chắc chắn muốn hủy đăng ký phương tiện biển số {vehicle.LicensePlate} không?",
            "Xác nhận hủy đăng ký phương tiện",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning
        );

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _vehicleService.DeleteVehicleAsync(vehicle.Id);
            await LoadMyVehiclesAsync();
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi hệ thống khi xóa phương tiện.";
        }
    }
}
