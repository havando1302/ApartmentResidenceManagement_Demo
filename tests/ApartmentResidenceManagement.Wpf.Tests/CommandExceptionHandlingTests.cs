using ApartmentResidenceManagement.Wpf.Commands;
using Xunit;

namespace ApartmentResidenceManagement.Wpf.Tests;

public class CommandExceptionHandlingTests
{
    [Fact]
    public async Task AsyncRelayCommand_WhenExecutionFails_InvokesErrorHandler()
    {
        var expected = new InvalidOperationException("test failure");
        var handledException = new TaskCompletionSource<Exception>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var command = new AsyncRelayCommand(
            _ => Task.FromException(expected),
            onException: exception => handledException.TrySetResult(exception));

        command.Execute(null);

        var actual = await handledException.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Same(expected, actual);
    }

    [Fact]
    public void RelayCommand_WhenExecutionFails_InvokesErrorHandlerWithoutRethrowing()
    {
        var expected = new InvalidOperationException("test failure");
        Exception? handledException = null;
        var command = new RelayCommand(
            _ => throw expected,
            onException: exception => handledException = exception);

        var escapedException = Record.Exception(() => command.Execute(null));

        Assert.Null(escapedException);
        Assert.Same(expected, handledException);
    }
}
