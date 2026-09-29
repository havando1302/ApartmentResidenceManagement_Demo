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
    }

    public async Task LoadFamilyMembersAsync()
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

                Members.Clear();
                foreach (var rHistory in list.OrderBy(x => x.RelationshipType))
                {
                    var resident = rHistory.Resident;
                    if (resident == null) continue;

                    string relationText = rHistory.RelationshipType switch
                    {
                        RelationshipType.Owner => "Chủ hộ",
                        RelationshipType.FamilyMember => "Thành viên hộ gia đình",
                        RelationshipType.Tenant => "Khách thuê",
                        RelationshipType.Temporary => "Tạm trú",
                        _ => "Chưa rõ"
                    };

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
