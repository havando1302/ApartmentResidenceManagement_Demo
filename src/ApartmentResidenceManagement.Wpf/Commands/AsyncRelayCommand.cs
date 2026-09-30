using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ApartmentResidenceManagement.Wpf.Services;

namespace ApartmentResidenceManagement.Wpf.Commands;

/// <summary>
/// ICommand dành cho tác vụ bất đồng bộ, tự khóa trong lúc đang chạy để tránh gửi lặp thao tác.
/// </summary>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Predicate<object?>? _canExecute;
    private readonly Action<Exception> _onException;
    private bool _isExecuting;

    public AsyncRelayCommand(
        Func<object?, Task> execute,
        Predicate<object?>? canExecute = null,
        Action<Exception>? onException = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _onException = onException ?? ExceptionHandlingService.Handle;
    }

    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        try
        {
            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();
            await _execute(parameter);
        }
        catch (Exception ex)
        {
            // ICommand.Execute is async void, so an exception that escapes here
            // becomes a dispatcher-level unhandled exception and Visual Studio
            // stops at its source line. Convert it to a UI notification instead.
            _onException(ex);
        }
        finally
        {
            _isExecuting = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
