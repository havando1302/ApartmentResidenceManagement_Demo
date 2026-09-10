using System;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Application.Services;

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
    #endregion

    public ResidentHomeViewModel(ResidenceService residenceService, VehicleService vehicleService)
    {
        _residenceService = residenceService;
        _vehicleService = vehicleService;
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
        _ = LoadStatsAsync();
    }

    public async Task LoadStatsAsync()
    {
        if (_account?.Resident == null) return;

        int residentId = _account.Resident.Id;
        ResidentName = _account.Resident.FullName;

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

                // Đổi quan hệ sang tiếng Việt (suy luận thông minh)
                if (activeResidence.RelationshipType == RelationshipType.Owner)
                {
                    RelationshipText = "Chủ Hộ";
                }
                else if (activeResidence.RelationshipType == RelationshipType.Tenant)
                {
                    RelationshipText = "Người ở ghép";
                }
                else if (activeResidence.RelationshipType == RelationshipType.Temporary)
                {
                    RelationshipText = "Tạm Trú";
                }
                else if (activeResidence.RelationshipType == RelationshipType.FamilyMember)
                {
                    var ownerResidence = familyMembers.FirstOrDefault(x => x.RelationshipType == RelationshipType.Owner);
                    var owner = ownerResidence?.Resident;
                    var resident = _account.Resident;

                    string relationText = "Thành viên";
                    if (owner != null)
                    {
                        int ageDiff = owner.DateOfBirth.Year - resident.DateOfBirth.Year;
                        if (owner.Gender == GenderType.Male && resident.Gender == GenderType.Female && Math.Abs(ageDiff) <= 10)
                        {
                            relationText = "Vợ";
                        }
                        else if (owner.Gender == GenderType.Female && resident.Gender == GenderType.Male && Math.Abs(ageDiff) <= 10)
                        {
                            relationText = "Chồng";
                        }
                        else if (ageDiff >= 16)
                        {
                            relationText = resident.Gender == GenderType.Male ? "Con trai" : "Con gái";
                        }
                        else if (ageDiff <= -16)
                        {
                            relationText = resident.Gender == GenderType.Male ? "Bố" : "Mẹ";
                        }
                        else if (ageDiff > 0)
                        {
                            relationText = resident.Gender == GenderType.Male ? "Em trai" : "Em gái";
                        }
                        else
                        {
                            relationText = resident.Gender == GenderType.Male ? "Anh trai" : "Chị gái";
                        }
                    }
                    RelationshipText = relationText;
                }
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
            VehicleCount = vehicles.Count();
        }
        catch (Exception)
        {
            // Ghi nhận lỗi chìm, không crash giao diện
        }
    }
}
