using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class ResidentHomeViewModel : ViewModelBase
{
    private UserAccount? _account;
    private readonly ResidenceService _residenceService;
    private readonly VehicleService _vehicleService;

    private string _residentName = string.Empty;
    private string _apartmentNumber = "Chưa có";
    private string _relationshipText = "Chưa rõ";
    private int _familyCount;
    private int _vehicleCount;
    private string _residencyStatus = "Không hoạt động";
    private string _errorMessage = string.Empty;
    private bool _isLoading;

    #region Properties
    public string ResidentName
    {
        get => _residentName;
        set => SetProperty(ref _residentName, value);
    }

    public string ApartmentNumber
    {
        get => _apartmentNumber;
        set => SetProperty(ref _apartmentNumber, value);
    }

    public string RelationshipText
    {
        get => _relationshipText;
        set => SetProperty(ref _relationshipText, value);
    }

    public int FamilyCount
    {
        get => _familyCount;
        set => SetProperty(ref _familyCount, value);
    }

    public int VehicleCount
    {
        get => _vehicleCount;
        set => SetProperty(ref _vehicleCount, value);
    }

    public string ResidencyStatus
    {
        get => _residencyStatus;
        set => SetProperty(ref _residencyStatus, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    #endregion

    public ICommand RefreshCommand { get; }

    public ResidentHomeViewModel(ResidenceService residenceService, VehicleService vehicleService)
    {
        _residenceService = residenceService;
        _vehicleService = vehicleService;
        RefreshCommand = new AsyncRelayCommand(_ => LoadStatsAsync());
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
    }

    public async Task LoadStatsAsync()
    {
        if (_account?.Resident == null) return;

        int residentId = _account.Resident.Id;
        ResidentName = _account.Resident.FullName;
        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            // 1. Lấy thông tin cư trú hoạt động của cư dân này
            var activeResidence = await _residenceService.GetActiveResidenceByResidentIdAsync(residentId);
            if (activeResidence != null)
            {
                ApartmentNumber = activeResidence.Apartment?.ApartmentNumber ?? "Chưa rõ";
                ResidencyStatus = "Đang cư trú";
                
                // Lấy số thành viên trong căn hộ
                var familyMembers = await _residenceService.GetActiveResidencesByApartmentIdAsync(activeResidence.ApartmentId);
                FamilyCount = familyMembers.Count();

                RelationshipText = activeResidence.RelationshipType switch
                {
                    RelationshipType.Owner => "Chủ hộ",
                    RelationshipType.FamilyMember => "Thành viên hộ gia đình",
                    RelationshipType.Tenant => "Khách thuê",
                    RelationshipType.Temporary => "Tạm trú",
                    _ => "Chưa rõ"
                };
            }
            else
            {
                ApartmentNumber = "Chưa đăng ký";
                ResidencyStatus = "Chưa cư trú";
                RelationshipText = "N/A";
                FamilyCount = 0;
            }

            // 2. Lấy số lượng xe của cư dân
            var vehicles = await _vehicleService.GetVehiclesByOwnerIdAsync(residentId);
            VehicleCount = vehicles.Count(v => v.RegistrationStatus == VehicleRegistrationStatus.Approved);
        }
        catch (Exception)
        {
            ErrorMessage = "Không thể tải thông tin tổng quan. Vui lòng thử lại.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
