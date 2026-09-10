using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;
using Xunit;

namespace ApartmentResidenceManagement.Wpf.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class WpfTestCollection : ICollectionFixture<WpfDispatcherFixture>
{
    public const string Name = "WPF dispatcher";
}

public sealed class WpfDispatcherFixture : IDisposable
{
    private readonly Thread _thread;
    private readonly Dispatcher _dispatcher;
    private ExceptionDispatchInfo? _workerFailure;

    public WpfDispatcherFixture()
    {
        var ready = new TaskCompletionSource<Dispatcher>(TaskCreationOptions.RunContinuationsAsynchronously);
        _thread = new Thread(() =>
        {
            try
            {
                // Load shared resources without constructing the application's
                // App subclass, starting services, or connecting to a database.
                var application = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                foreach (var dictionary in new[] { "DesignSystem", "Controls" })
                {
                    application.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri($"/ApartmentResidenceManagement.Wpf;component/Themes/{dictionary}.xaml", UriKind.Relative)
                    });
                }

                ready.SetResult(Dispatcher.CurrentDispatcher);
                Dispatcher.Run();
            }
            catch (Exception exception)
            {
                Volatile.Write(ref _workerFailure, ExceptionDispatchInfo.Capture(exception));
                ready.TrySetException(exception);
            }
        }) { IsBackground = true, Name = "WPF regression test dispatcher" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _dispatcher = ready.Task.WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
    }

    public void Run(Action test)
    {
        Volatile.Read(ref _workerFailure)?.Throw();
        ExceptionDispatchInfo? failure = null;
        var operation = _dispatcher.InvokeAsync(() =>
        {
            try
            {
                test();
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        });
        try
        {
            operation.Task.WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        }
        catch
        {
            Volatile.Read(ref _workerFailure)?.Throw();
            throw;
        }

        failure?.Throw();
    }

    public void Dispose()
    {
        _dispatcher.InvokeAsync(() =>
        {
            System.Windows.Application.Current.Shutdown();
            // Dispatcher.Run, rather than Application.Run, owns this loop.
            // Allow queued application cleanup to finish before stopping it.
            _dispatcher.BeginInvokeShutdown(DispatcherPriority.ApplicationIdle);
        });
        if (!_thread.Join(TimeSpan.FromSeconds(10)))
        {
            throw new TimeoutException("The WPF test dispatcher did not stop.");
        }

        Volatile.Read(ref _workerFailure)?.Throw();
    }
}
