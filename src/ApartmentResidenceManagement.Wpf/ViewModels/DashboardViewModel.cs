using ApartmentResidenceManagement.Application.Services;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly ApartmentService _apartmentService;
    private readonly ResidentService _residentService;
    private readonly VehicleService _vehicleService;

    // Các thuộc tính thống kê
    private int _totalApartments;
    public int TotalApartments
    {
        get => _totalApartments;
        set => SetProperty(ref _totalApartments, value);
    }

    private int _occupiedApartments;
    public int OccupiedApartments
    {
        get => _occupiedApartments;
        set => SetProperty(ref _occupiedApartments, value);
    }

    private int _emptyApartments;
    public int EmptyApartments
    {
        get => _emptyApartments;
        set => SetProperty(ref _emptyApartments, value);
    }

    private int _maintenanceApartments;
    public int MaintenanceApartments
    {
        get => _maintenanceApartments;
        set => SetProperty(ref _maintenanceApartments, value);
    }

    private int _totalResidents;
    public int TotalResidents
    {
        get => _totalResidents;
        set => SetProperty(ref _totalResidents, value);
    }

    private int _totalVehicles;
    public int TotalVehicles
    {
        get => _totalVehicles;
        set => SetProperty(ref _totalVehicles, value);
    }

    public DashboardViewModel(ApartmentService apartmentService, ResidentService residentService, VehicleService vehicleService)
    {
        _apartmentService = apartmentService;
        _residentService = residentService;
        _vehicleService = vehicleService;
    }

    public async Task LoadStatsAsync()
    {
        try
        {
            var apartments = await _apartmentService.GetAllApartmentsAsync();
            TotalApartments = apartments.Count();
            OccupiedApartments = apartments.Count(a => a.Status == Domain.Enums.ApartmentStatus.Occupied);
            EmptyApartments = apartments.Count(a => a.Status == Domain.Enums.ApartmentStatus.Empty);
            MaintenanceApartments = apartments.Count(a => a.Status == Domain.Enums.ApartmentStatus.UnderMaintenance);

            var residents = await _residentService.GetAllResidentsAsync();
            TotalResidents = residents.Count();

            var vehicles = await _vehicleService.GetAllVehiclesAsync();
            TotalVehicles = vehicles.Count(v =>
                v.RegistrationStatus == Domain.Enums.VehicleRegistrationStatus.Approved);
        }
        catch
        {
            // Bỏ qua lỗi hoặc thiết lập về 0
        }
    }
}
