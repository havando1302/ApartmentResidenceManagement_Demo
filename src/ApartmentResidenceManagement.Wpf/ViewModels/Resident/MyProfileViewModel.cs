using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Enums;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class MyProfileViewModel : ViewModelBase
{
    private UserAccount? _account;
    private readonly ResidentService _residentService;

    // Các thuộc tính chỉ xem
    private string _residentId = string.Empty;
    private string _fullName = string.Empty;
    private string _dobText = string.Empty;
    private string _genderText = string.Empty;
    private string _identityCard = string.Empty;

    // Các thuộc tính có thể sửa
    private string _phoneNumber = string.Empty;
    private string _homeTown = string.Empty;

    // Trạng thái thông báo
    private string _successMessage = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasSuccess;
    private bool _hasError;

    #region Properties
    public string ResidentId
    {
        get => _residentId;
        set => SetProperty(ref _residentId, value);
    }

    public string FullName
    {
        get => _fullName;
        set => SetProperty(ref _fullName, value);
    }

    public string DobText
    {
        get => _dobText;
        set => SetProperty(ref _dobText, value);
    }

    public string GenderText
    {
        get => _genderText;
        set => SetProperty(ref _genderText, value);
    }

    public string IdentityCard
    {
        get => _identityCard;
        set => SetProperty(ref _identityCard, value);
    }

    public string PhoneNumber
    {
        get => _phoneNumber;
        set => SetProperty(ref _phoneNumber, value);
    }

    public string HomeTown
    {
        get => _homeTown;
        set => SetProperty(ref _homeTown, value);
    }

    public string SuccessMessage
    {
        get => _successMessage;
        set
        {
            if (SetProperty(ref _successMessage, value))
            {
                HasSuccess = !string.IsNullOrEmpty(value);
            }
        }
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

    public bool HasSuccess
    {
        get => _hasSuccess;
        set => SetProperty(ref _hasSuccess, value);
    }

    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }
    #endregion

    public ICommand SaveProfileCommand { get; }

    public MyProfileViewModel(ResidentService residentService)
    {
        _residentService = residentService;
        SaveProfileCommand = new RelayCommand(async _ => await SaveProfileAsync());
    }

    public void Initialize(UserAccount account)
    {
        _account = account;
        LoadProfileData();
    }

    private void LoadProfileData()
    {
        if (_account?.Resident == null) return;

        var r = _account.Resident;
        ResidentId = $"CD-{r.Id:D4}";
        FullName = r.FullName;
        DobText = r.DateOfBirth.ToString("dd/MM/yyyy");
        GenderText = r.Gender switch
        {
            GenderType.Male => "Nam",
            GenderType.Female => "Nữ",
            _ => "Khác"
        };
        IdentityCard = r.IdentityCard ?? "Chưa có";
        
        PhoneNumber = r.PhoneNumber ?? string.Empty;
        HomeTown = r.HomeTown ?? string.Empty;
    }

    private async Task SaveProfileAsync()
    {
        SuccessMessage = string.Empty;
        ErrorMessage = string.Empty;

        if (_account?.Resident == null) return;

        try
        {
            // Lấy thực thể Resident hiện tại
            var currentResident = _account.Resident;

            // Cập nhật các trường được phép thay đổi
            currentResident.PhoneNumber = PhoneNumber;
            currentResident.HomeTown = HomeTown;

            // Gọi service để cập nhật xuống DB
            await _residentService.UpdateResidentAsync(currentResident);

            SuccessMessage = "Cập nhật thông tin liên hệ thành công!";
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Lỗi hệ thống khi cập nhật thông tin.";
        }
    }
}
