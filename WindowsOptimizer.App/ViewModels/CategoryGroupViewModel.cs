using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WindowsOptimizer.App.ViewModels;

/// <summary>
/// Represents a collapsible category group containing tweaks.
/// </summary>
public sealed class CategoryGroupViewModel : ViewModelBase
{
    private bool _isExpanded = false; // Start collapsed for performance
    private readonly ObservableCollection<TweakItemViewModel> _tweaks;
    private bool _hasDetected = false;
    private readonly ObservableCollection<CategoryGroupViewModel> _subGroups = new();
    private bool _isNested = false;
    private bool _isDense = false;
    private CancellationTokenSource? _detectionCts;
    private bool _isDetecting = false;

    public CategoryGroupViewModel(string categoryName, string categoryIcon)
    {
        CategoryName = categoryName;
        CategoryIcon = categoryIcon;
        _tweaks = new ObservableCollection<TweakItemViewModel>();
        ToggleExpandCommand = new AsyncRelayCommand(ToggleExpand);
        LogToFile($"CategoryGroupViewModel created: Name='{CategoryName}', Icon='{CategoryIcon}'");
    }

    public string CategoryName { get; }

    public string CategoryIcon { get; }

    public string DisplayName => $"{CategoryIcon} {CategoryName}";

    public ObservableCollection<TweakItemViewModel> Tweaks => _tweaks;

    /// <summary>
    /// Returns tweaks only when expanded, empty otherwise. This improves performance
    /// by not creating UI elements for collapsed categories.
    /// </summary>
    public IEnumerable<TweakItemViewModel> VisibleTweaks => _isExpanded ? _tweaks : Enumerable.Empty<TweakItemViewModel>();

    public ObservableCollection<CategoryGroupViewModel> SubGroups => _subGroups;

    public bool HasSubGroups => SubGroups.Any();

    public CategoryGroupViewModel? Parent { get; set; }

    public bool IsNested
    {
        get => _isNested;
        set => SetProperty(ref _isNested, value);
    }

    public bool IsDense
    {
        get => _isDense;
        set => SetProperty(ref _isDense, value);
    }

    public int TweakCount => _tweaks.Count;

    public int AggregateTweakCount => _tweaks.Count + _subGroups.Sum(g => g.AggregateTweakCount);

    public int VisibleTweakCount => _tweaks.Count(t => true); // Can add filter logic

    public string CountBadge => $"{AggregateTweakCount}";

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value))
            {
                OnPropertyChanged(nameof(ExpanderIcon));
                OnPropertyChanged(nameof(VisibleTweaks)); // Notify UI to update items
            }
        }
    }

    public string ExpanderIcon => IsExpanded ? "▼" : "▶";

    public ICommand ToggleExpandCommand { get; }

    public void AddTweak(TweakItemViewModel tweak)
    {
        if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() ?? true)
        {
            _tweaks.Add(tweak);
            OnPropertyChanged(nameof(TweakCount));
            OnPropertyChanged(nameof(AggregateTweakCount));
            OnPropertyChanged(nameof(CountBadge));
            OnPropertyChanged(nameof(VisibleTweakCount));
            Parent?.NotifyAggregateCountsChanged();
        }
        else
        {
            System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() => AddTweak(tweak));
        }
    }

    public void NotifyAggregateCountsChanged()
    {
        OnPropertyChanged(nameof(AggregateTweakCount));
        OnPropertyChanged(nameof(CountBadge));
        Parent?.NotifyAggregateCountsChanged();
    }

    public void ClearTweaks()
    {
        if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() ?? true)
        {
            _tweaks.Clear();
            OnPropertyChanged(nameof(TweakCount));
            OnPropertyChanged(nameof(AggregateTweakCount));
            OnPropertyChanged(nameof(CountBadge));
            OnPropertyChanged(nameof(VisibleTweakCount));
            Parent?.NotifyAggregateCountsChanged();
        }
        else
        {
            System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() => ClearTweaks());
        }
    }

    public void MarkDetected()
    {
        _hasDetected = true;
    }

    private async Task ToggleExpand()
    {
        try
        {
            LogToFile($"ToggleExpand: Category '{CategoryName}' IsExpanded changing to {!IsExpanded}");
            IsExpanded = !IsExpanded;

            // Cancel any pending detection when collapsing
            if (!IsExpanded && _isDetecting)
            {
                LogToFile($"ToggleExpand: Cancelling detection for '{CategoryName}'");
                CancelDetection();
                return;
            }

            // Auto-detect tweak status when first expanded
            if (IsExpanded && !_hasDetected)
            {
                _hasDetected = true;
                LogToFile($"ToggleExpand: Starting DetectAllTweaksAsync for '{CategoryName}' with {_tweaks.Count} tweaks");
                try
                {
                    await DetectAllTweaksAsync();
                    LogToFile($"ToggleExpand: DetectAllTweaksAsync completed for '{CategoryName}'");
                }
                catch (System.OperationCanceledException)
                {
                    LogToFile($"ToggleExpand: DetectAllTweaksAsync CANCELLED for '{CategoryName}'");
                    _hasDetected = false; // Allow retry when re-expanded
                }
                catch (System.Exception ex)
                {
                    LogToFile($"ToggleExpand: DetectAllTweaksAsync FAILED for '{CategoryName}': {ex.Message}");
                    LogToFile($"Stack: {ex.StackTrace}");
                }
            }
        }
        catch (System.Exception ex)
        {
            LogToFile($"CRASH in ToggleExpand for '{CategoryName}': {ex.Message}");
            LogToFile($"Stack: {ex.StackTrace}");
        }
    }

    private void CancelDetection()
    {
        try
        {
            _detectionCts?.Cancel();
            _detectionCts?.Dispose();
            _detectionCts = null;
            _isDetecting = false;
        }
        catch (System.Exception ex)
        {
            LogToFile($"CancelDetection error: {ex.Message}");
        }
    }

    private async Task DetectAllTweaksAsync()
    {
        // Create new cancellation token for this detection run
        CancelDetection(); // Cancel any previous detection
        _detectionCts = new CancellationTokenSource();
        var ct = _detectionCts.Token;
        _isDetecting = true;

        try
        {
            foreach (var tweak in _tweaks)
            {
                // Check cancellation before each tweak
                ct.ThrowIfCancellationRequested();

                if (!tweak.IsScanFriendly)
                {
                    LogToFile($"DetectAllTweaksAsync: '{tweak.Name}' SKIPPED (expensive)");
                    continue;
                }

                try
                {
                    LogToFile($"DetectAllTweaksAsync: Detecting '{tweak.Name}'");

                    // Add timeout to prevent hanging (with cancellation support)
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeoutCts.CancelAfter(5000); // 5 second timeout

                    await tweak.DetectStatusAsync(timeoutCts.Token);
                    LogToFile($"DetectAllTweaksAsync: '{tweak.Name}' completed");
                }
                catch (System.OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw; // Re-throw cancellation
                }
                catch (System.OperationCanceledException)
                {
                    LogToFile($"DetectAllTweaksAsync: '{tweak.Name}' TIMEOUT (5s)");
                }
                catch (System.Exception ex)
                {
                    LogToFile($"DetectAllTweaksAsync: '{tweak.Name}' FAILED: {ex.Message}");
                    // Continue with other tweaks even if one fails
                }
            }
        }
        finally
        {
            _isDetecting = false;
            LogToFile($"DetectAllTweaksAsync: Detection finished for '{CategoryName}'");
        }
    }

    private static void LogToFile(string message)
    {
        try
        {
            var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WindowsOptimizer_Debug.log");
            var timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            System.IO.File.AppendAllText(logPath, $"[{timestamp}] {message}\n");
        }
        catch
        {
            // Ignore logging errors
        }
    }
}
