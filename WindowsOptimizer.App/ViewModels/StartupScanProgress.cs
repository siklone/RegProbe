namespace WindowsOptimizer.App.ViewModels;

public sealed class StartupScanProgress
{
    public StartupScanProgress(int current, int total, string? currentName = null)
    {
        Current = current;
        Total = total;
        CurrentName = currentName ?? string.Empty;
    }

    public int Current { get; }

    public int Total { get; }

    public string CurrentName { get; }

    public double Percent => Total <= 0 ? 0 : (double)Current / Total * 100;
}
