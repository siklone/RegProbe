namespace RegProbe.App.ViewModels;

internal sealed class WorkspaceSearchDebouncer : IDisposable
{
    private CancellationTokenSource? _searchCts;

    public void Trigger(Action refreshFilteredViews)
    {
        ArgumentNullException.ThrowIfNull(refreshFilteredViews);

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        Task.Delay(300, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                System.Windows.Application.Current?.Dispatcher?.BeginInvoke(refreshFilteredViews);
            }
        }, token);
    }

    public void Dispose()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
    }
}
