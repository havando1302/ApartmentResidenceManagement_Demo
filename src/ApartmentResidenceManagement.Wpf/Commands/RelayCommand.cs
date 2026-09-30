using System;
using System.Windows.Input;
using ApartmentResidenceManagement.Wpf.Services;

namespace ApartmentResidenceManagement.Wpf.Commands;

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;
    private readonly Action<Exception> _onException;

    public RelayCommand(
        Action<object?> execute,
        Predicate<object?>? canExecute = null,
        Action<Exception>? onException = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _onException = onException ?? ExceptionHandlingService.Handle;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    public void Execute(object? parameter)
    {
        try
        {
            _execute(parameter);
        }
        catch (Exception ex)
        {
            _onException(ex);
        }
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
