using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Application.Services;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class ResidencyHistoryItem
{
    public string ApartmentNumber { get; set; } = "Chưa rõ";
    public string RelationshipText { get; set; } = "Chưa rõ";
    public string StartDateText { get; set; } = "Chưa rõ";
    public string EndDateText { get; set; } = "Hiện tại";
    public string StatusText { get; set; } = "Đang ở";
    public bool IsActive { get; set; }
}

public class ResidencyHistoryViewModel : ViewModelBase
{
    private UserAccount? _account;
    private readonly ResidenceService _residenceService;

    private ObservableCollection<ResidencyHistoryItem> _historyItems = new();
    private string _errorMessage = string.Empty;
    private bool _hasError;

    #region Properties
    public ObservableCollection<ResidencyHistoryItem> HistoryItems
    {
        get => _historyItems;
        set => SetProperty(ref _historyItems, value);
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
    #endregion

    public ResidencyHistoryViewModel(ResidenceService residenceService)
    {
        _residenceService = residenceService;
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
    }

    public async Task LoadHistoryAsync()
    {
        if (_account?.Resident == null) return;
        ErrorMessage = string.Empty;

        try
        {
            var list = await _residenceService.GetHistoryByResidentIdAsync(_account.Resident.Id);
            
            HistoryItems.Clear();
            foreach (var h in list.OrderByDescending(x => x.StartDate))
            {
                HistoryItems.Add(new ResidencyHistoryItem
                {
                    ApartmentNumber = h.Apartment?.ApartmentNumber ?? "Chưa rõ",
                    StartDateText = h.StartDate.ToString("dd/MM/yyyy"),
                    EndDateText = h.EndDate.HasValue ? h.EndDate.Value.ToString("dd/MM/yyyy") : "Hiện tại",
                    IsActive = h.IsActive,
                    StatusText = h.IsActive ? "Đang cư trú" : "Đã chuyển đi",
                    RelationshipText = h.RelationshipType switch
                    {
                        RelationshipType.Owner => "Chủ Hộ",
                        RelationshipType.FamilyMember => "Thành Viên",
                        RelationshipType.Tenant => "Khách Thuê",
                        RelationshipType.Temporary => "Tạm Trú",
                        _ => "Khác"
                    }
                });
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi hệ thống khi tải lịch sử cư trú.";
        }
    }
}
