using System.Collections.ObjectModel;
using System.Linq;
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

    public CategoryGroupViewModel(string categoryName, string categoryIcon)
    {
        CategoryName = categoryName;
        CategoryIcon = categoryIcon;
        _tweaks = new ObservableCollection<TweakItemViewModel>();
        ToggleExpandCommand = new RelayCommand(_ => ToggleExpand());
    }

    public string CategoryName { get; }

    public string CategoryIcon { get; }

    public string DisplayName => $"{CategoryIcon} {CategoryName}";

    public ObservableCollection<TweakItemViewModel> Tweaks => _tweaks;

    public ObservableCollection<CategoryGroupViewModel> SubGroups => _subGroups;

    public bool HasSubGroups => SubGroups.Any();

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

    public int VisibleTweakCount => _tweaks.Count(t => true); // Can add filter logic

    public string CountBadge => $"{TweakCount}";

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value))
            {
                OnPropertyChanged(nameof(ExpanderIcon));
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
            OnPropertyChanged(nameof(CountBadge));
            OnPropertyChanged(nameof(VisibleTweakCount));
        }
        else
        {
            System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() => AddTweak(tweak));
        }
    }

    public void ClearTweaks()
    {
        if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() ?? true)
        {
            _tweaks.Clear();
            OnPropertyChanged(nameof(TweakCount));
            OnPropertyChanged(nameof(CountBadge));
            OnPropertyChanged(nameof(VisibleTweakCount));
        }
        else
        {
            System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() => ClearTweaks());
        }
    }

    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
        
        // Auto-detect tweak status when first expanded
        if (IsExpanded && !_hasDetected)
        {
            _hasDetected = true;
            _ = DetectAllTweaksAsync();
        }
    }

    private async Task DetectAllTweaksAsync()
    {
        try
        {
            foreach (var tweak in _tweaks)
            {
                try
                {
                    await tweak.DetectStatusAsync();
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to detect status for {tweak.Name}: {ex.Message}");
                    // Continue with other tweaks even if one fails
                }
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DetectAllTweaksAsync failed: {ex.Message}");
        }
    }
}
