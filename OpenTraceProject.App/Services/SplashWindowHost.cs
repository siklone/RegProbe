using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using OpenTraceProject.App.ViewModels;

namespace OpenTraceProject.App.Services;

internal sealed class SplashWindowHost : IDisposable
{
    private readonly TaskCompletionSource<bool> _shownTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _closedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private Thread? _thread;
    private Dispatcher? _dispatcher;
    private StartupWindow? _window;
    private int _started;

    public Task ShowAsync()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return _shownTcs.Task;
        }

        _thread = new Thread(ThreadMain)
        {
            IsBackground = true,
            Name = "SplashWindowThread"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();

        return _shownTcs.Task;
    }

    public void UpdatePreloadProgress(PreloadProgress progress)
    {
        var dispatcher = _dispatcher;
        var window = _window;
        if (dispatcher == null || window == null)
        {
            return;
        }

        _ = dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => window.UpdatePreloadProgress(progress)));
    }

    public void UpdateScanProgress(StartupScanProgress progress)
    {
        var dispatcher = _dispatcher;
        var window = _window;
        if (dispatcher == null || window == null)
        {
            return;
        }

        _ = dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => window.UpdateScanProgress(progress)));
    }

    public async Task CompleteAndCloseAsync()
    {
        var dispatcher = _dispatcher;
        var window = _window;
        if (dispatcher == null || window == null)
        {
            return;
        }

        await dispatcher.InvokeAsync(() => window.CompleteAndCloseAsync(), DispatcherPriority.Send).Task.Unwrap();
        await _closedTcs.Task;
    }

    public void CloseImmediately()
    {
        var dispatcher = _dispatcher;
        if (dispatcher == null)
        {
            return;
        }

        _ = dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
        {
            _window?.Close();
        }));
    }

    public void Dispose()
    {
        CloseImmediately();
    }

    private void ThreadMain()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;

        var window = new StartupWindow();
        _window = window;

        window.Loaded += (_, _) => _shownTcs.TrySetResult(true);
        window.Closed += (_, _) =>
        {
            _closedTcs.TrySetResult(true);
            Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
        };

        window.Show();
        Dispatcher.Run();
    }
}
