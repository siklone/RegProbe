using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WindowsOptimizer.App.HardwareDb;
using WindowsOptimizer.App.Models;
using WindowsOptimizer.App.Services;

namespace WindowsOptimizer.App.ViewModels.Base;

public abstract class HardwareDetailViewModelBase : ViewModelBase
{
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private ImageSource? _iconSource;
    private bool _isLoading = true;
    private string _loadingMessage = "Loading hardware data...";

    protected readonly MetricCacheService Cache = MetricCacheService.Instance;

    // Indicates whether the last attempted cache load produced data
    protected bool WasCacheHit { get; set; }

    /// <summary>
    /// Public load entry point. Attempts to populate the view from cache first.
    /// If the cache was missed, performs a synchronous fallback load (blocking).
    /// </summary>
    public void Load()
    {
        WasCacheHit = false;
        // Try populate from cache first
        LoadFromCache();

        if (!WasCacheHit)
        {
            // Cache miss - perform direct blocking preload then re-attempt load from cache
            LoadDirect();
        }
    }

    /// <summary>
    /// Override to provide a direct fallback preload when cache is empty. The default
    /// implementation runs the HardwareDataPreloader asynchronously on a background
    /// thread so the UI thread is not blocked. After preloading completes the cache
    /// is re-read on the UI thread and the view updated.
    /// </summary>
    protected virtual void LoadDirect()
    {
        // Show a loading indicator while the fallback runs
        IsLoading = true;
        LoadingMessage = "Loading hardware data (fallback)...";

        Task.Run(async () =>
        {
            try
            {
                var preloader = new HardwareDataPreloader(Cache, AppServices.OsDetectionService, AppServices.MotherboardProvider);
                await preloader.PreloadAllAsync().ConfigureAwait(false);

                // After preloading, re-attempt to load from cache on the UI thread
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    try
                    {
                        LoadFromCache();
                    }
                    catch
                    {
                        // Swallow exceptions from derived implementations to avoid crashing background work
                    }
                });
            }
            catch
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    ClearSpecs();
                    AddRow("Status", "Direct load failed");
                    SetLoadingComplete();
                });
            }
            finally
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    IsLoading = false;
                    LoadingMessage = "Ready";
                });
            }
        });
    }

    public abstract HardwareType HardwareType { get; }
    
    public abstract void LoadFromCache();

    public string Title
    {
        get => _title;
        protected set => SetProperty(ref _title, value);
    }

    public string Subtitle
    {
        get => _subtitle;
        protected set => SetProperty(ref _subtitle, value);
    }

    public ImageSource? IconSource
    {
        get => _iconSource;
        protected set => SetProperty(ref _iconSource, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        protected set => SetProperty(ref _isLoading, value);
    }

    public string LoadingMessage
    {
        get => _loadingMessage;
        protected set => SetProperty(ref _loadingMessage, value);
    }

    public ObservableCollection<SpecItem> Specs { get; } = new();

    protected void ResolveIcon(string? vendor, string? model)
    {
        IconSource = IconResolver.Resolve(HardwareType, vendor, model);
    }

    protected void ResolveIcon(HardwareType type, string? vendor, string? model)
    {
        IconSource = IconResolver.Resolve(type, vendor, model);
    }

    protected void AddHeader(string label)
    {
        Specs.Add(SpecItem.Header(label));
    }

    protected void AddRow(string label, string? value)
    {
        Specs.Add(SpecItem.Row(label, value));
    }

    protected void AddRow(string label, int? value)
    {
        Specs.Add(SpecItem.Row(label, value));
    }

    protected void AddRow(string label, long? value)
    {
        Specs.Add(SpecItem.Row(label, value));
    }

    protected void AddRow(string label, double? value, string format = "F1")
    {
        Specs.Add(SpecItem.Row(label, value, format));
    }

    protected void AddRowIf(string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            Specs.Add(SpecItem.Row(label, value));
        }
    }

    protected void AddRowIf(string label, int? value)
    {
        if (value.HasValue && value.Value > 0)
        {
            Specs.Add(SpecItem.Row(label, value));
        }
    }

    protected void AddRowIf(string label, long? value)
    {
        if (value.HasValue && value.Value > 0)
        {
            Specs.Add(SpecItem.Row(label, value));
        }
    }

    protected void AddRowIf(string label, double? value, string format = "F1")
    {
        if (value.HasValue && value.Value > 0)
        {
            Specs.Add(SpecItem.Row(label, value, format));
        }
    }

    protected void ClearSpecs()
    {
        Specs.Clear();
    }

    protected static string FormatBytes(long bytes)
    {
        const long kb = 1024L;
        const long mb = kb * 1024L;
        const long gb = mb * 1024L;
        const long tb = gb * 1024L;

        if (bytes <= 0) return "N/A";
        if (bytes >= tb) return $"{bytes / (double)tb:F2} TB";
        if (bytes >= gb) return $"{bytes / (double)gb:F1} GB";
        if (bytes >= mb) return $"{bytes / (double)mb:F1} MB";
        if (bytes >= kb) return $"{bytes / (double)kb:F1} KB";
        return $"{bytes} B";
    }

    protected static string FormatHz(int mhz)
    {
        if (mhz <= 0) return "N/A";
        if (mhz >= 1000) return $"{mhz / 1000.0:F2} GHz";
        return $"{mhz} MHz";
    }

    protected void SetLoadingComplete()
    {
        IsLoading = false;
        LoadingMessage = "Ready";
    }
}
