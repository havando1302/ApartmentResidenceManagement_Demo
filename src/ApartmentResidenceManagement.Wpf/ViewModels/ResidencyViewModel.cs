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

public class ResidencyViewModel : ViewModelBase
{
    private readonly ResidenceService _residenceService;
    private readonly ApartmentService _apartmentService;
    private readonly ResidentService _residentService;
    
    private List<ResidenceHistory> _allResidences = new();

    // Lọc & Tìm kiếm
    private string _searchText = string.Empty;
    private string _selectedStatusFilter = "Tất cả"; // Tất cả, Đang cư trú, Đã chuyển đi
    
    // UI Collections
    private ObservableCollection<ResidenceHistory> _residences = new();
    private List<Apartment> _availableApartments = new();
    private List<Resident> _availableResidents = new();
    
    // Thao tác Form
    private ResidenceHistory? _selectedResidence;
    private bool _isFormOpen;
    private string _formTitle = string.Empty;
    private string _actionType = "Register"; // Register, Terminate, Transfer
    
    // Form Inputs
    private Apartment? _selectedApartmentInput;
    private Resident? _selectedResidentInput;
    private RelationshipType _selectedRelationshipType = RelationshipType.FamilyMember;
    private DateTime _startDateInput = DateTime.Today;
    private DateTime _endDateInput = DateTime.Today;
    
    // Báo lỗi
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isLoading;

    #region Properties
    public ObservableCollection<ResidenceHistory> Residences
    {
        get => _residences;
        set => SetProperty(ref _residences, value);
    }

    public List<Apartment> AvailableApartments
    {
        get => _availableApartments;
        set => SetProperty(ref _availableApartments, value);
    }

    public List<Resident> AvailableResidents
    {
        get => _availableResidents;
        set => SetProperty(ref _availableResidents, value);
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

    public List<string> StatusFilterOptions { get; } = new() { "Tất cả", "Đang cư trú", "Đã chuyển đi" };

    public ResidenceHistory? SelectedResidence
    {
        get => _selectedResidence;
        set => SetProperty(ref _selectedResidence, value);
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

    public string ActionType
    {
        get => _actionType;
        set => SetProperty(ref _actionType, value);
    }

    // Form inputs properties
    public Apartment? SelectedApartmentInput
    {
        get => _selectedApartmentInput;
        set => SetProperty(ref _selectedApartmentInput, value);
    }

    public Resident? SelectedResidentInput
    {
        get => _selectedResidentInput;
        set => SetProperty(ref _selectedResidentInput, value);
    }

    public RelationshipType SelectedRelationshipType
    {
        get => _selectedRelationshipType;
        set => SetProperty(ref _selectedRelationshipType, value);
    }

    public List<RelationshipType> RelationshipTypes { get; } = Enum.GetValues(typeof(RelationshipType)).Cast<RelationshipType>().ToList();

    public DateTime StartDateInput
    {
        get => _startDateInput;
        set => SetProperty(ref _startDateInput, value);
    }

    public DateTime EndDateInput
    {
        get => _endDateInput;
        set => SetProperty(ref _endDateInput, value);
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
    #endregion

    #region Commands
    public ICommand LoadResidencesCommand { get; }
    public ICommand OpenRegisterFormCommand { get; }
    public ICommand OpenTerminateFormCommand { get; }
    public ICommand OpenTransferFormCommand { get; }
    public ICommand SaveResidencyCommand { get; }
    public ICommand CancelFormCommand { get; }
    #endregion

    public ResidencyViewModel(ResidenceService residenceService, ApartmentService apartmentService, ResidentService residentService)
    {
        _residenceService = residenceService;
        _apartmentService = apartmentService;
        _residentService = residentService;

        LoadResidencesCommand = new RelayCommand(async _ => await LoadDataAsync());
        OpenRegisterFormCommand = new RelayCommand(_ => OpenRegisterForm());
        OpenTerminateFormCommand = new RelayCommand(r => OpenTerminateForm(r));
        OpenTransferFormCommand = new RelayCommand(r => OpenTransferForm(r));
        SaveResidencyCommand = new RelayCommand(async _ => await SaveResidencyAsync());
        CancelFormCommand = new RelayCommand(_ => CloseForm());
    }

    public async Task LoadDataAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            var list = await _residenceService.GetAllResidencesAsync();
            _allResidences = list.ToList();

            var apartments = await _apartmentService.GetAllApartmentsAsync();
            AvailableApartments = apartments.OrderBy(a => a.ApartmentNumber).ToList();

            var residents = await _residentService.GetAllResidentsAsync();
            AvailableResidents = residents.OrderBy(r => r.FullName).ToList();

            ApplyFilters();
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi khi lấy dữ liệu cư trú hoặc danh mục căn hộ/cư dân.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allResidences.AsEnumerable();

        // 1. Lọc theo trạng thái
        if (SelectedStatusFilter == "Đang cư trú")
        {
            filtered = filtered.Where(rh => rh.IsActive);
        }
        else if (SelectedStatusFilter == "Đã chuyển đi")
        {
            filtered = filtered.Where(rh => !rh.IsActive);
        }

        // 2. Lọc theo text tìm kiếm
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string keyword = SearchText.Trim().ToLower();
            filtered = filtered.Where(rh => 
                (rh.Resident != null && rh.Resident.FullName.ToLower().Contains(keyword)) ||
                (rh.Resident != null && rh.Resident.IdentityCard != null && rh.Resident.IdentityCard.Contains(keyword)) ||
                (rh.Apartment != null && rh.Apartment.ApartmentNumber.ToLower().Contains(keyword)));
        }

        Residences.Clear();
        foreach (var item in filtered)
        {
            Residences.Add(item);
        }
    }

    private void OpenRegisterForm()
    {
        ErrorMessage = string.Empty;
        FormTitle = "ĐĂNG KÝ CƯ TRÚ MỚI";
        ActionType = "Register";
        SelectedApartmentInput = AvailableApartments.FirstOrDefault();
        SelectedResidentInput = AvailableResidents.FirstOrDefault();
        SelectedRelationshipType = RelationshipType.FamilyMember;
        StartDateInput = DateTime.Today;
        IsFormOpen = true;
    }

    private void OpenTerminateForm(object? parameter)
    {
        if (parameter is not ResidenceHistory rh) return;
        SelectedResidence = rh;
        ErrorMessage = string.Empty;
        FormTitle = $"KẾT THÚC CƯ TRÚ - {rh.Resident?.FullName}";
        ActionType = "Terminate";
        EndDateInput = DateTime.Today;
        IsFormOpen = true;
    }

    private void OpenTransferForm(object? parameter)
    {
        if (parameter is not ResidenceHistory rh) return;
        SelectedResidence = rh;
        ErrorMessage = string.Empty;
        FormTitle = $"CHUYỂN CĂN HỘ - {rh.Resident?.FullName}";
        ActionType = "Transfer";
        SelectedApartmentInput = AvailableApartments.FirstOrDefault(a => a.Id != rh.ApartmentId);
        SelectedRelationshipType = rh.RelationshipType;
        StartDateInput = DateTime.Today;
        IsFormOpen = true;
    }

    private void CloseForm()
    {
        IsFormOpen = false;
        ErrorMessage = string.Empty;
        SelectedResidence = null;
    }

    private async Task SaveResidencyAsync()
    {
        ErrorMessage = string.Empty;

        try
        {
            if (ActionType == "Register")
            {
                if (SelectedApartmentInput == null)
                {
                    ErrorMessage = "Vui lòng chọn căn hộ.";
                    return;
                }
                if (SelectedResidentInput == null)
                {
                    ErrorMessage = "Vui lòng chọn cư dân.";
                    return;
                }

                await _residenceService.RegisterResidenceAsync(
                    SelectedApartmentInput.Id,
                    SelectedResidentInput.Id,
                    SelectedRelationshipType,
                    StartDateInput
                );
            }
            else if (ActionType == "Terminate")
            {
                if (SelectedResidence == null) return;
                await _residenceService.TerminateResidenceAsync(SelectedResidence.Id, EndDateInput);
            }
            else if (ActionType == "Transfer")
            {
                if (SelectedResidence == null) return;
                if (SelectedApartmentInput == null)
                {
                    ErrorMessage = "Vui lòng chọn căn hộ mới.";
                    return;
                }

                await _residenceService.TransferApartmentAsync(
                    SelectedResidence.ResidentId,
                    SelectedApartmentInput.Id,
                    SelectedRelationshipType,
                    StartDateInput
                );
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
            ErrorMessage = "Lỗi hệ thống khi xử lý nghiệp vụ cư trú.";
        }
    }
}
