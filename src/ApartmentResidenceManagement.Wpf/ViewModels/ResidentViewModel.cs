using ApartmentResidenceManagement.Wpf.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class ResidentViewModel : ViewModelBase
{
    private readonly ResidentService _residentService;
    private readonly AccountService _accountService;
    private List<Resident> _allResidents = new();

    // Lọc & Tìm kiếm
    private string _searchText = string.Empty;

    // Quản lý DataGrid & Form Resident
    private ObservableCollection<Resident> _residents = new();
    private Resident? _selectedResident;
    private Resident _editingResident = new();
    private bool _isFormOpen;
    private string _formTitle = "THÊM CƯ DÂN MỚI";

    // Quản lý Panel cấp tài khoản
    private bool _isAccountPanelOpen;
    private string _accountUsername = string.Empty;
    private string _accountPassword = string.Empty;
    private bool _selectedResidentHasAccount;
    private UserAccount? _selectedResidentAccount;
    private string _accountStatusText = string.Empty;

    // Báo lỗi
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isLoading;

    #region Properties
    public ObservableCollection<Resident> Residents
    {
        get => _residents;
        set => SetProperty(ref _residents, value);
    }

    public Resident? SelectedResident
    {
        get => _selectedResident;
        set => SetProperty(ref _selectedResident, value);
    }

    public Resident EditingResident
    {
        get => _editingResident;
        set => SetProperty(ref _editingResident, value);
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

    // Tài khoản properties
    public bool IsAccountPanelOpen
    {
        get => _isAccountPanelOpen;
        set => SetProperty(ref _isAccountPanelOpen, value);
    }

    public string AccountUsername
    {
        get => _accountUsername;
        set => SetProperty(ref _accountUsername, value);
    }

    public string AccountPassword
    {
        get => _accountPassword;
        set => SetProperty(ref _accountPassword, value);
    }

    public bool SelectedResidentHasAccount
    {
        get => _selectedResidentHasAccount;
        set => SetProperty(ref _selectedResidentHasAccount, value);
    }

    public string AccountStatusText
    {
        get => _accountStatusText;
        set => SetProperty(ref _accountStatusText, value);
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

    // Giới tính options
    public List<ApartmentResidenceManagement.Domain.Enums.GenderType> GenderOptions { get; } = Enum.GetValues(typeof(ApartmentResidenceManagement.Domain.Enums.GenderType)).Cast<ApartmentResidenceManagement.Domain.Enums.GenderType>().ToList();
    #endregion

    #region Commands
    public ICommand LoadResidentsCommand { get; }
    public ICommand OpenAddFormCommand { get; }
    public ICommand OpenEditFormCommand { get; }
    public ICommand SaveResidentCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand DeleteResidentCommand { get; }
    
    // Account Commands
    public ICommand OpenAccountPanelCommand { get; }
    public ICommand CreateAccountCommand { get; }
    public ICommand ToggleAccountStatusCommand { get; }
    public ICommand CloseAccountPanelCommand { get; }
    #endregion

    public ResidentViewModel(ResidentService residentService, AccountService accountService)
    {
        _residentService = residentService;
        _accountService = accountService;

        LoadResidentsCommand = new AsyncRelayCommand(_ => LoadDataAsync());
        OpenAddFormCommand = new RelayCommand(_ => OpenAddForm());
        OpenEditFormCommand = new RelayCommand(r => { if (r is Resident res) OpenEditForm(res); });
        SaveResidentCommand = new AsyncRelayCommand(_ => SaveDataAsync());
        CancelFormCommand = new RelayCommand(_ => CloseForm());
        DeleteResidentCommand = new AsyncRelayCommand(DeleteDataAsync);

        OpenAccountPanelCommand = new AsyncRelayCommand(async r => { if (r is Resident res) { SelectedResident = res; await LoadAccountInfoAsync(res.Id); } });
        CreateAccountCommand = new AsyncRelayCommand(_ => CreateAccountAsync());
        ToggleAccountStatusCommand = new AsyncRelayCommand(_ => ToggleAccountStatusAsync());
        CloseAccountPanelCommand = new RelayCommand(_ => { IsAccountPanelOpen = false; SelectedResident = null; });
    }

    public async Task LoadDataAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            var list = await _residentService.GetAllResidentsAsync();
            _allResidents = list.ToList();
            ApplyFilters();
        }
        catch (Exception)
        {
            ErrorMessage = "Đã xảy ra lỗi khi lấy danh sách cư dân từ database.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allResidents.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string keyword = SearchText.Trim().ToLower();
            filtered = filtered.Where(r => 
                r.FullName.ToLower().Contains(keyword) || 
                (r.IdentityCard != null && r.IdentityCard.Contains(keyword)) ||
                (r.PhoneNumber != null && r.PhoneNumber.Contains(keyword)));
        }

        Residents.Clear();
        foreach (var item in filtered.OrderBy(r => r.FullName))
        {
            Residents.Add(item);
        }
    }

    private void OpenAddForm()
    {
        ErrorMessage = string.Empty;
        FormTitle = "THÊM CƯ DÂN MỚI";
        EditingResident = new Resident 
        { 
            Gender = ApartmentResidenceManagement.Domain.Enums.GenderType.Male,
            DateOfBirth = DateTime.Now.AddYears(-20) 
        };
        IsFormOpen = true;
        IsAccountPanelOpen = false;
    }

    private void OpenEditForm(Resident resident)
    {
        ErrorMessage = string.Empty;
        FormTitle = $"CẬP NHẬT CƯ DÂN {resident.FullName}";

        // Clone đối tượng
        EditingResident = new Resident
        {
            Id = resident.Id,
            FullName = resident.FullName,
            DateOfBirth = resident.DateOfBirth,
            Gender = resident.Gender,
            IdentityCard = resident.IdentityCard,
            PhoneNumber = resident.PhoneNumber,
            HomeTown = resident.HomeTown
        };
        IsFormOpen = true;
    }

    private void CloseForm()
    {
        IsFormOpen = false;
        ErrorMessage = string.Empty;
        SelectedResident = null;
        IsAccountPanelOpen = false;
    }

    private async Task SaveDataAsync()
    {
        ErrorMessage = string.Empty;

        // Validation
        if (string.IsNullOrWhiteSpace(EditingResident.FullName))
        {
            ErrorMessage = "Vui lòng nhập họ tên cư dân.";
            return;
        }

        try
        {
            if (EditingResident.Id == 0)
            {
                await _residentService.CreateResidentAsync(EditingResident);
            }
            else
            {
                await _residentService.UpdateResidentAsync(EditingResident);
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
            ErrorMessage = "Lỗi hệ thống trong quá trình lưu dữ liệu.";
        }
    }

    private async Task DeleteDataAsync(object? parameter)
    {
        if (parameter is not Resident resident) return;

        ErrorMessage = string.Empty;

        var result = NotificationService.Show(
            $"Bạn có chắc chắn muốn xóa cư dân {resident.FullName} (CCCD: {resident.IdentityCard}) không?",
            "Xác nhận xóa cư dân",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning
        );

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _residentService.DeleteResidentAsync(resident.Id);
            await LoadDataAsync();
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Cư dân này đã phát sinh dữ liệu lịch sử cư trú, không thể xóa.";
        }
    }

    #region Account Management Logic
    private async Task LoadAccountInfoAsync(int residentId)
    {
        ErrorMessage = string.Empty;
        AccountUsername = string.Empty;
        AccountPassword = string.Empty;

        try
        {
            _selectedResidentAccount = await _accountService.GetAccountByResidentIdAsync(residentId);
            
            if (_selectedResidentAccount != null)
            {
                SelectedResidentHasAccount = true;
                AccountUsername = _selectedResidentAccount.Username;
                AccountStatusText = _selectedResidentAccount.IsActive ? "ĐANG HOẠT ĐỘNG" : "ĐÃ BỊ KHÓA";
            }
            else
            {
                SelectedResidentHasAccount = false;
                AccountStatusText = "CHƯA CÓ TÀI KHOẢN";
            }

            IsAccountPanelOpen = true;
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi khi lấy thông tin tài khoản cư dân.";
        }
    }

    private async Task CreateAccountAsync()
    {
        if (SelectedResident == null) return;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(AccountUsername))
        {
            ErrorMessage = "Tên đăng nhập không được để trống.";
            return;
        }

        if (string.IsNullOrWhiteSpace(AccountPassword) || AccountPassword.Length < 6)
        {
            ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.";
            return;
        }

        try
        {
            var newAcc = await _accountService.CreateResidentAccountAsync(SelectedResident.Id, AccountUsername, AccountPassword);
            if (newAcc != null)
            {
                await LoadAccountInfoAsync(SelectedResident.Id);
            }
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi hệ thống khi cấp tài khoản.";
        }
    }

    private async Task ToggleAccountStatusAsync()
    {
        if (_selectedResidentAccount == null || SelectedResident == null) return;
        ErrorMessage = string.Empty;

        try
        {
            await _accountService.ToggleAccountStatusAsync(_selectedResidentAccount.Id);
            await LoadAccountInfoAsync(SelectedResident.Id);
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi khi thay đổi trạng thái tài khoản.";
        }
    }
    #endregion
}
