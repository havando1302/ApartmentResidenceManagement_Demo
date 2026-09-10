using System;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Application.Services;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class MyApartmentViewModel : ViewModelBase
{
    private UserAccount? _account;
    private readonly ResidenceService _residenceService;

    // Chi tiết Căn hộ
    private string _apartmentNumber = "Chưa đăng ký";
    private string _floorText = "Chưa rõ";
    private string _areaText = "Chưa rõ";
    private string _apartmentStatusText = "Chưa rõ";
    
    // Chi tiết Cư trú
    private string _relationshipText = "Chưa rõ";
    private string _startDateText = "Chưa rõ";
    private bool _hasApartment;

    #region Properties
    public string ApartmentNumber
    {
        get => _apartmentNumber;
        set => SetProperty(ref _apartmentNumber, value);
    }

    public string FloorText
    {
        get => _floorText;
        set => SetProperty(ref _floorText, value);
    }

    public string AreaText
    {
        get => _areaText;
        set => SetProperty(ref _areaText, value);
    }

    public string ApartmentStatusText
    {
        get => _apartmentStatusText;
        set => SetProperty(ref _apartmentStatusText, value);
    }

    public string RelationshipText
    {
        get => _relationshipText;
        set => SetProperty(ref _relationshipText, value);
    }

    public string StartDateText
    {
        get => _startDateText;
        set => SetProperty(ref _startDateText, value);
    }

    public bool HasApartment
    {
        get => _hasApartment;
        set => SetProperty(ref _hasApartment, value);
    }
    #endregion

    public MyApartmentViewModel(ResidenceService residenceService)
    {
        _residenceService = residenceService;
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
        _ = LoadApartmentDataAsync();
    }

    private async Task LoadApartmentDataAsync()
    {
        if (_account?.Resident == null)
        {
            HasApartment = false;
            return;
        }

        try
        {
            var activeResidence = await _residenceService.GetActiveResidenceByResidentIdAsync(_account.Resident.Id);
            if (activeResidence != null && activeResidence.Apartment != null)
            {
                var apt = activeResidence.Apartment;
                ApartmentNumber = apt.ApartmentNumber;
                FloorText = $"Tầng {apt.Floor}";
                AreaText = $"{apt.Area} m²";
                
                ApartmentStatusText = apt.Status switch
                {
                    ApartmentResidenceManagement.Domain.Enums.ApartmentStatus.Empty => "Trống",
                    ApartmentResidenceManagement.Domain.Enums.ApartmentStatus.Occupied => "Đang Cư Trú (Occupied)",
                    ApartmentResidenceManagement.Domain.Enums.ApartmentStatus.UnderMaintenance => "Bảo Trì / Sửa Chữa",
                    _ => "Khác"
                };

                RelationshipText = activeResidence.RelationshipType switch
                {
                    RelationshipType.Owner => "Chủ Hộ (Owner)",
                    RelationshipType.FamilyMember => "Thành Viên Hộ Gia Đình",
                    RelationshipType.Tenant => "Khách Thuê (Tenant)",
                    RelationshipType.Temporary => "Tạm Trú (Temporary)",
                    _ => "Khác"
                };

                StartDateText = activeResidence.StartDate.ToString("dd/MM/yyyy");
                HasApartment = true;
            }
            else
            {
                HasApartment = false;
            }
        }
        catch (Exception)
        {
            HasApartment = false;
        }
    }
}
