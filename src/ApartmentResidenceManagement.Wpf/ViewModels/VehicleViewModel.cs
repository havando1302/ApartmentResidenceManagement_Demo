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

public class VehicleItem
{
    public int Id => Vehicle.Id;
    public string LicensePlate => Vehicle.LicensePlate;
    public VehicleType VehicleType => Vehicle.VehicleType;
    public string Brand => Vehicle.Brand ?? "Chưa rõ";
    public int OwnerId => Vehicle.OwnerId;
    public string OwnerName => Vehicle.Owner?.FullName ?? "Chưa rõ";
    public string OwnerIdentityCard => Vehicle.Owner?.IdentityCard ?? "Chưa rõ";
    public string ApartmentNumber { get; set; } = "Chưa rõ";
    public VehicleRegistrationStatus RegistrationStatus => Vehicle.RegistrationStatus;
    public string RegistrationStatusText => RegistrationStatus switch
    {
        VehicleRegistrationStatus.Pending => "Chờ duyệt",
        VehicleRegistrationStatus.Approved => "Đã duyệt",
        VehicleRegistrationStatus.Rejected => "Từ chối",
        _ => "Không hợp lệ"
    };

    public Vehicle Vehicle { get; }

    public VehicleItem(Vehicle vehicle, string apartmentNumber)
    {
        Vehicle = vehicle;
        ApartmentNumber = apartmentNumber;
    }
}

public class VehicleViewModel : ViewModelBase
{
    private readonly VehicleService _vehicleService;
    private readonly ResidentService _residentService;
    private readonly ResidenceService _residenceService;

    private List<VehicleItem> _allVehicles = new();

    // Lọc & Tìm kiếm
    private string _searchText = string.Empty;
    private string _selectedTypeFilter = "Tất cả"; // Tất cả, Xe máy, Ô tô, Xe đạp, Khác

    // UI Collections
    private ObservableCollection<VehicleItem> _vehicles = new();
    private List<Resident> _activeResidents = new(); // Chỉ cư dân đang cư trú active mới được đăng ký xe

    // Thao tác Form
    private VehicleItem? _selectedVehicleItem;
    private Vehicle _editingVehicle = new();
    private bool _isFormOpen;
    private string _formTitle = "ĐĂNG KÝ PHƯƠNG TIỆN MỚI";

    // Báo lỗi
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isLoading;

    #region Properties
    public ObservableCollection<VehicleItem> Vehicles
    {
        get => _vehicles;
        set => SetProperty(ref _vehicles, value);
    }

    public List<Resident> ActiveResidents
    {
        get => _activeResidents;
        set => SetProperty(ref _activeResidents, value);
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

    public VehicleItem? SelectedVehicleItem
    {
        get => _selectedVehicleItem;
        set => SetProperty(ref _selectedVehicleItem, value);
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

    // Báo lỗi
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

    // Vehicle Types list
    public List<VehicleType> VehicleTypes { get; } = Enum.GetValues(typeof(VehicleType)).Cast<VehicleType>().ToList();
    #endregion

    #region Commands
    public ICommand LoadVehiclesCommand { get; }
    public ICommand OpenAddFormCommand { get; }
    public ICommand OpenEditFormCommand { get; }
    public ICommand SaveVehicleCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand DeleteVehicleCommand { get; }
    public ICommand ApproveVehicleCommand { get; }
    public ICommand RejectVehicleCommand { get; }
    #endregion

    public VehicleViewModel(VehicleService vehicleService, ResidentService residentService, ResidenceService residenceService)
    {
        _vehicleService = vehicleService;
        _residentService = residentService;
        _residenceService = residenceService;

        LoadVehiclesCommand = new AsyncRelayCommand(_ => LoadDataAsync());
        OpenAddFormCommand = new RelayCommand(_ => OpenAddForm());
        OpenEditFormCommand = new RelayCommand(v => { if (v is VehicleItem item) OpenEditForm(item); });
        SaveVehicleCommand = new AsyncRelayCommand(_ => SaveVehicleAsync());
        CancelFormCommand = new RelayCommand(_ => CloseForm());
        DeleteVehicleCommand = new AsyncRelayCommand(DeleteVehicleAsync);
        ApproveVehicleCommand = new AsyncRelayCommand(p => ReviewVehicleAsync(p, VehicleRegistrationStatus.Approved));
        RejectVehicleCommand = new AsyncRelayCommand(p => ReviewVehicleAsync(p, VehicleRegistrationStatus.Rejected));
    }

    public async Task LoadDataAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            // 1. Lấy mapping cư dân -> số căn hộ đang ở active
            var activeResidences = await _residenceService.GetAllResidencesAsync();
            var residentApartmentMap = activeResidences
                .Where(rh => rh.IsActive)
                .GroupBy(rh => rh.ResidentId)
                .ToDictionary(g => g.Key, g => g.First().Apartment?.ApartmentNumber ?? "Chưa rõ");

            // 2. Lấy danh sách cư dân đang cư trú active để chọn lựa trong Form đăng ký xe
            var activeResidentIds = residentApartmentMap.Keys.ToList();
            var allResidents = await _residentService.GetAllResidentsAsync();
            ActiveResidents = allResidents
                .Where(r => activeResidentIds.Contains(r.Id))
                .OrderBy(r => r.FullName)
                .ToList();

            // 3. Lấy danh sách xe
            var vehicles = await _vehicleService.GetAllVehiclesAsync();
            _allVehicles = vehicles.Select(v => 
            {
                residentApartmentMap.TryGetValue(v.OwnerId, out var aptNum);
                return new VehicleItem(v, aptNum ?? "Chưa rõ");
            }).ToList();

            ApplyFilters();
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi khi lấy danh sách phương tiện hoặc cư dân.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allVehicles.AsEnumerable();

        // 1. Lọc theo Loại xe
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

        // 2. Lọc theo Text tìm kiếm (Biển số, tên cư dân, số căn hộ)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string keyword = SearchText.Trim().ToLower();
            filtered = filtered.Where(v => 
                v.LicensePlate.ToLower().Contains(keyword) || 
                v.OwnerName.ToLower().Contains(keyword) || 
                v.ApartmentNumber.ToLower().Contains(keyword));
        }

        Vehicles.Clear();
        foreach (var item in filtered)
        {
            Vehicles.Add(item);
        }
    }

    private void OpenAddForm()
    {
        ErrorMessage = string.Empty;
        FormTitle = "ĐĂNG KÝ PHƯƠNG TIỆN MỚI";
        EditingVehicle = new Vehicle
        {
            VehicleType = VehicleType.Moto,
            OwnerId = ActiveResidents.FirstOrDefault()?.Id ?? 0
        };
        IsFormOpen = true;
    }

    private void OpenEditForm(VehicleItem item)
    {
        ErrorMessage = string.Empty;
        FormTitle = $"CẬP NHẬT PHƯƠNG TIỆN {item.LicensePlate}";
        EditingVehicle = new Vehicle
        {
            Id = item.Vehicle.Id,
            LicensePlate = item.Vehicle.LicensePlate,
            VehicleType = item.Vehicle.VehicleType,
            Brand = item.Vehicle.Brand,
            OwnerId = item.Vehicle.OwnerId,
            RegistrationStatus = item.Vehicle.RegistrationStatus
        };
        IsFormOpen = true;
    }

    private void CloseForm()
    {
        IsFormOpen = false;
        ErrorMessage = string.Empty;
        SelectedVehicleItem = null;
    }

    private async Task SaveVehicleAsync()
    {
        ErrorMessage = string.Empty;

        // Validation cơ bản
        if (string.IsNullOrWhiteSpace(EditingVehicle.LicensePlate))
        {
            ErrorMessage = "Vui lòng nhập biển số xe.";
            return;
        }

        if (EditingVehicle.OwnerId == 0)
        {
            ErrorMessage = "Vui lòng chọn cư dân sở hữu.";
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
            await LoadDataAsync();
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
        if (parameter is not VehicleItem item) return;
        ErrorMessage = string.Empty;

        var result = NotificationService.Show(
            $"Bạn có chắc chắn muốn xóa phương tiện có biển số {item.LicensePlate} của cư dân {item.OwnerName} không?",
            "Xác nhận xóa phương tiện",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning
        );

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _vehicleService.DeleteVehicleAsync(item.Id);
            await LoadDataAsync();
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

    private async Task ReviewVehicleAsync(object? parameter, VehicleRegistrationStatus status)
    {
        if (parameter is not VehicleItem item) return;
        ErrorMessage = string.Empty;

        try
        {
            await _vehicleService.ReviewVehicleAsync(item.Id, status);
            await LoadDataAsync();
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi hệ thống khi duyệt phương tiện.";
        }
    }
}
