using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Application.Services;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class FamilyMemberItem
{
    public string FullName { get; set; } = string.Empty;
    public string GenderText { get; set; } = string.Empty;
    public string DobText { get; set; } = string.Empty;
    public string IdentityCard { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string RelationshipText { get; set; } = string.Empty;
    public string StartDateText { get; set; } = string.Empty;
}

public class FamilyMembersViewModel : ViewModelBase
{
    private UserAccount? _account;
    private readonly ResidenceService _residenceService;

    private ObservableCollection<FamilyMemberItem> _members = new();
    private bool _hasApartment;
    private string _apartmentNumber = string.Empty;

    #region Properties
    public ObservableCollection<FamilyMemberItem> Members
    {
        get => _members;
        set => SetProperty(ref _members, value);
    }

    public bool HasApartment
    {
        get => _hasApartment;
        set => SetProperty(ref _hasApartment, value);
    }

    public string ApartmentNumber
    {
        get => _apartmentNumber;
        set => SetProperty(ref _apartmentNumber, value);
    }
    #endregion

    public FamilyMembersViewModel(ResidenceService residenceService)
    {
        _residenceService = residenceService;
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
        _ = LoadFamilyMembersAsync();
    }

    private async Task LoadFamilyMembersAsync()
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
                HasApartment = true;
                ApartmentNumber = activeResidence.Apartment.ApartmentNumber;

                // Lấy toàn bộ cư trú hoạt động của căn hộ này
                var list = await _residenceService.GetActiveResidencesByApartmentIdAsync(activeResidence.ApartmentId);

                // Lấy thông tin chủ hộ trước để so sánh tuổi/giới tính
                var ownerResidence = list.FirstOrDefault(x => x.RelationshipType == RelationshipType.Owner);
                var owner = ownerResidence?.Resident;

                Members.Clear();
                foreach (var rHistory in list.OrderBy(x => x.RelationshipType))
                {
                    var resident = rHistory.Resident;
                    if (resident == null) continue;

                    string relationText = "Thành viên";
                    if (rHistory.RelationshipType == RelationshipType.Owner)
                    {
                        relationText = "Chủ hộ";
                    }
                    else if (rHistory.RelationshipType == RelationshipType.Tenant)
                    {
                        relationText = "Người ở ghép";
                    }
                    else if (rHistory.RelationshipType == RelationshipType.Temporary)
                    {
                        relationText = "Tạm trú";
                    }
                    else if (rHistory.RelationshipType == RelationshipType.FamilyMember && owner != null)
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

                    Members.Add(new FamilyMemberItem
                    {
                        FullName = resident.FullName,
                        DobText = resident.DateOfBirth.ToString("dd/MM/yyyy"),
                        IdentityCard = resident.IdentityCard ?? "Chưa có",
                        PhoneNumber = resident.PhoneNumber ?? "Chưa có",
                        GenderText = resident.Gender switch
                        {
                            GenderType.Male => "Nam",
                            GenderType.Female => "Nữ",
                            _ => "Khác"
                        },
                        RelationshipText = relationText,
                        StartDateText = rHistory.StartDate.ToString("dd/MM/yyyy")
                    });
                }
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
