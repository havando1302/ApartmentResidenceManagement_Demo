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

public class ApartmentViewModel : ViewModelBase
{
    private readonly ApartmentService _apartmentService;
    private List<Apartment> _allApartments = new();
    
    // Properties cho Search và Lọc
    private string _searchText = string.Empty;
    private string _selectedStatusFilter = "Tất cả";
    private string _selectedFloorFilter = "Tất cả";
    private List<string> _availableFloors = new();

    // Properties cho DataGrid và Form
    private ObservableCollection<Apartment> _apartments = new();
    private Apartment? _selectedApartment;
    private Apartment _editingApartment = new();
    private bool _isFormOpen;
    private string _formTitle = "THÊM CĂN HỘ MỚI";
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isLoading;

    #region Binding Properties
    public ObservableCollection<Apartment> Apartments
    {
        get => _apartments;
        set => SetProperty(ref _apartments, value);
    }

    public List<string> AvailableFloors
    {
        get => _availableFloors;
        set => SetProperty(ref _availableFloors, value);
    }

    public Apartment? SelectedApartment
    {
        get => _selectedApartment;
        set => SetProperty(ref _selectedApartment, value);
    }

    public Apartment EditingApartment
    {
        get => _editingApartment;
        set => SetProperty(ref _editingApartment, value);
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

    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (SetProperty(ref _selectedStatusFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public string SelectedFloorFilter
    {
        get => _selectedFloorFilter;
        set
        {
            if (SetProperty(ref _selectedFloorFilter, value))
            {
                ApplyFilters();
            }
        }
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

    // Các danh sách phục vụ ComboBox
    public List<ApartmentStatus> StatusOptions { get; } = Enum.GetValues(typeof(ApartmentStatus)).Cast<ApartmentStatus>().ToList();

    public List<string> StatusFilterOptions { get; } = new() { "Tất cả", "Trống", "Đang ở", "Bảo trì" };
    #endregion

    #region Commands
    public ICommand LoadApartmentsCommand { get; }
    public ICommand OpenAddFormCommand { get; }
    public ICommand OpenEditFormCommand { get; }
    public ICommand SaveApartmentCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand DeleteApartmentCommand { get; }
    #endregion

    public ApartmentViewModel(ApartmentService apartmentService)
    {
        _apartmentService = apartmentService;

        LoadApartmentsCommand = new AsyncRelayCommand(_ => LoadDataAsync());
        OpenAddFormCommand = new RelayCommand(_ => OpenAddForm());
        OpenEditFormCommand = new RelayCommand(p => { if (p is Apartment apt) OpenEditForm(apt); });
        SaveApartmentCommand = new AsyncRelayCommand(_ => SaveDataAsync());
        CancelFormCommand = new RelayCommand(_ => CloseForm());
        DeleteApartmentCommand = new AsyncRelayCommand(DeleteDataAsync);

        _selectedStatusFilter = "Tất cả";
        _selectedFloorFilter = "Tất cả";
    }

    public async Task LoadDataAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            var list = await _apartmentService.GetAllApartmentsAsync();
            _allApartments = list.ToList();
            
            // Cập nhật danh sách Tầng khả dụng để lọc
            var floors = _allApartments.Select(a => a.Floor).Distinct().OrderBy(f => f).ToList();
            var floorList = new List<string> { "Tất cả" };
            floorList.AddRange(floors.Select(f => f.ToString()));
            AvailableFloors = floorList;

            ApplyFilters();
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi khi lấy dữ liệu căn hộ từ server.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allApartments.AsEnumerable();

        // 1. Lọc theo search text
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(a => a.ApartmentNumber.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        // 2. Lọc theo trạng thái
        if (SelectedStatusFilter != "Tất cả")
        {
            filtered = SelectedStatusFilter switch
            {
                "Trống"   => filtered.Where(a => a.Status == ApartmentStatus.Empty),
                "Đang ở" => filtered.Where(a => a.Status == ApartmentStatus.Occupied),
                "Bảo trì" => filtered.Where(a => a.Status == ApartmentStatus.UnderMaintenance),
                _         => filtered
            };
        }

        // 3. Lọc theo tầng
        if (SelectedFloorFilter != "Tất cả" && int.TryParse(SelectedFloorFilter, out int floorNum))
        {
            filtered = filtered.Where(a => a.Floor == floorNum);
        }

        // Cập nhật hiển thị lên UI
        Apartments.Clear();
        foreach (var item in filtered.OrderBy(a => a.ApartmentNumber))
        {
            Apartments.Add(item);
        }
    }

    private void OpenAddForm()
    {
        ErrorMessage = string.Empty;
        FormTitle = "THÊM CĂN HỘ MỚI";
        EditingApartment = new Apartment 
        { 
            Status = ApartmentStatus.Empty,
            Floor = 1 
        };
        IsFormOpen = true;
    }

    private void OpenEditForm(Apartment apartment)
    {
        ErrorMessage = string.Empty;
        FormTitle = $"CẬP NHẬT CĂN HỘ {apartment.ApartmentNumber}";
        
        // Clone đối tượng để tránh thay đổi trực tiếp trên DataGrid khi chưa bấm Save
        EditingApartment = new Apartment
        {
            Id = apartment.Id,
            ApartmentNumber = apartment.ApartmentNumber,
            Floor = apartment.Floor,
            Area = apartment.Area,
            Status = apartment.Status
        };
        IsFormOpen = true;
    }

    private void CloseForm()
    {
        IsFormOpen = false;
        ErrorMessage = string.Empty;
        SelectedApartment = null;
    }

    private async Task SaveDataAsync()
    {
        ErrorMessage = string.Empty;

        // Validation cơ bản đầu vào
        if (string.IsNullOrWhiteSpace(EditingApartment.ApartmentNumber))
        {
            ErrorMessage = "Vui lòng nhập số căn hộ.";
            return;
        }

        if (EditingApartment.Floor <= 0)
        {
            ErrorMessage = "Số tầng phải lớn hơn 0.";
            return;
        }

        if (EditingApartment.Area <= 0)
        {
            ErrorMessage = "Diện tích phải lớn hơn 0.";
            return;
        }

        try
        {
            if (EditingApartment.Id == 0)
            {
                // THÊM MỚI
                await _apartmentService.CreateApartmentAsync(EditingApartment);
            }
            else
            {
                // CẬP NHẬT
                await _apartmentService.UpdateApartmentAsync(EditingApartment);
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
            ErrorMessage = "Đã xảy ra lỗi hệ thống trong quá trình lưu dữ liệu.";
        }
    }

    private async Task DeleteDataAsync(object? parameter)
    {
        if (parameter is not Apartment apartment) return;

        ErrorMessage = string.Empty;

        var result = NotificationService.Show(
            $"Bạn có chắc chắn muốn xóa căn hộ số {apartment.ApartmentNumber} (Tầng {apartment.Floor}) không?",
            "Xác nhận xóa căn hộ",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning
        );

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _apartmentService.DeleteApartmentAsync(apartment.Id);
            await LoadDataAsync();
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Đã xảy ra lỗi hệ thống khi xóa căn hộ.";
        }
    }
}
