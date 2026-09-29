using System;
using System.Windows.Controls;
using System.Windows.Input;
using ApartmentResidenceManagement.Domain.Entities;
using ApartmentResidenceManagement.Domain.Exceptions;
using ApartmentResidenceManagement.Application.Services;
using ApartmentResidenceManagement.Wpf.Commands;

namespace ApartmentResidenceManagement.Wpf.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly AccountService _accountService;
    private string _username = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError;

    public event EventHandler? OnLoginSuccess;
    public UserAccount? LoggedInAccount { get; private set; }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
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

    public ICommand LoginCommand { get; }

    public LoginViewModel(AccountService accountService)
    {
        _accountService = accountService;
        LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, CanExecuteLogin);
    }

    private bool CanExecuteLogin(object? parameter)
    {
        return !string.IsNullOrWhiteSpace(Username);
    }

    private async Task ExecuteLoginAsync(object? parameter)
    {
        ErrorMessage = string.Empty;

        if (parameter is not PasswordBox passwordBox)
        {
            ErrorMessage = "Không thể đọc thông tin mật khẩu.";
            return;
        }

        string password = passwordBox.Password;

        try
        {
            var account = await _accountService.LoginAsync(Username, password);
            if (account != null)
            {
                LoggedInAccount = account;
                // Phát sự kiện đăng nhập thành công
                OnLoginSuccess?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (BusinessRuleException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Đã xảy ra lỗi không mong muốn trong quá trình đăng nhập. Vui lòng thử lại.";
        }
    }
}
