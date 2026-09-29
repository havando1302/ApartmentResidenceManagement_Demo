using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class StatisticsViewModel : ViewModelBase
{
    private readonly ApartmentService _apartmentService;
    private readonly ResidentService _residentService;
    private readonly ResidenceService _residenceService;
    private readonly VehicleService _vehicleService;

    // KPI Cards
    private int _totalApartments;
    private int _totalResidents;
    private int _activeResidentsCount;
    private int _totalVehicles;

    // Chi tiết Căn hộ
    private int _occupiedApartments;
    private int _emptyApartments;
    private int _maintenanceApartments;

    // Chi tiết Cư dân (Giới tính)
    private int _maleResidents;
    private int _femaleResidents;
    private int _otherResidents;

    // Chi tiết Phương tiện
    private int _motorbikesCount;
    private int _carsCount;
    private int _bicyclesCount;
    private int _otherVehiclesCount;

    // Báo lỗi
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isLoading;

    #region Properties
    public int TotalApartments
    {
        get => _totalApartments;
        set => SetProperty(ref _totalApartments, value);
    }

    public int TotalResidents
    {
        get => _totalResidents;
        set => SetProperty(ref _totalResidents, value);
    }

    public int ActiveResidentsCount
    {
        get => _activeResidentsCount;
        set => SetProperty(ref _activeResidentsCount, value);
    }

    public int TotalVehicles
    {
        get => _totalVehicles;
        set => SetProperty(ref _totalVehicles, value);
    }

    // Apartment details
    public int OccupiedApartments
    {
        get => _occupiedApartments;
        set => SetProperty(ref _occupiedApartments, value);
    }

    public int EmptyApartments
    {
        get => _emptyApartments;
        set => SetProperty(ref _emptyApartments, value);
    }

    public int MaintenanceApartments
    {
        get => _maintenanceApartments;
        set => SetProperty(ref _maintenanceApartments, value);
    }

    // Resident details
    public int MaleResidents
    {
        get => _maleResidents;
        set => SetProperty(ref _maleResidents, value);
    }

    public int FemaleResidents
    {
        get => _femaleResidents;
        set => SetProperty(ref _femaleResidents, value);
    }

    public int OtherResidents
    {
        get => _otherResidents;
        set => SetProperty(ref _otherResidents, value);
    }

    // Vehicle details
    public int MotorbikesCount
    {
        get => _motorbikesCount;
        set => SetProperty(ref _motorbikesCount, value);
    }

    public int CarsCount
    {
        get => _carsCount;
        set => SetProperty(ref _carsCount, value);
    }

    public int BicyclesCount
    {
        get => _bicyclesCount;
        set => SetProperty(ref _bicyclesCount, value);
    }

    public int OtherVehiclesCount
    {
        get => _otherVehiclesCount;
        set => SetProperty(ref _otherVehiclesCount, value);
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
    #endregion

    public ICommand RefreshCommand { get; }

    public StatisticsViewModel(
        ApartmentService apartmentService,
        ResidentService residentService,
        ResidenceService residenceService,
        VehicleService vehicleService)
    {
        _apartmentService = apartmentService;
        _residentService = residentService;
        _residenceService = residenceService;
        _vehicleService = vehicleService;

        RefreshCommand = new AsyncRelayCommand(_ => LoadStatisticsAsync());
    }

    public async Task LoadStatisticsAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            // 1. Thống kê Căn hộ
            var apartments = (await _apartmentService.GetAllApartmentsAsync()).ToList();
            TotalApartments = apartments.Count;
            OccupiedApartments = apartments.Count(a => a.Status == ApartmentStatus.Occupied);
            EmptyApartments = apartments.Count(a => a.Status == ApartmentStatus.Empty);
            MaintenanceApartments = apartments.Count(a => a.Status == ApartmentStatus.UnderMaintenance);

            // 2. Thống kê Cư dân
            var residents = (await _residentService.GetAllResidentsAsync()).ToList();
            TotalResidents = residents.Count;
            MaleResidents = residents.Count(r => r.Gender == GenderType.Male);
            FemaleResidents = residents.Count(r => r.Gender == GenderType.Female);
            OtherResidents = residents.Count(r => r.Gender == GenderType.Other);

            // 3. Cư dân đang cư trú hoạt động
            var residences = (await _residenceService.GetAllResidencesAsync()).ToList();
            ActiveResidentsCount = residences.Count(rh => rh.IsActive);

            // 4. Thống kê Phương tiện
            var vehicles = (await _vehicleService.GetAllVehiclesAsync())
                .Where(v => v.RegistrationStatus == VehicleRegistrationStatus.Approved)
                .ToList();
            TotalVehicles = vehicles.Count;
            MotorbikesCount = vehicles.Count(v => v.VehicleType == VehicleType.Moto);
            CarsCount = vehicles.Count(v => v.VehicleType == VehicleType.Car);
            BicyclesCount = vehicles.Count(v => v.VehicleType == VehicleType.Bicycle);
            OtherVehiclesCount = 0;
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi hệ thống khi tải dữ liệu thống kê.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
