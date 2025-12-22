using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace WindowsOptimizer.App.ViewModels;

/// <summary>
/// Represents a collapsible category group containing tweaks.
/// </summary>
public sealed class CategoryGroupViewModel : ViewModelBase
{
    private bool _isExpanded = true;
    private readonly ObservableCollection<TweakItemViewModel> _tweaks;

    public CategoryGroupViewModel(string categoryName, string categoryIcon)
    {
        CategoryName = categoryName;
        CategoryIcon = categoryIcon;
        _tweaks = new ObservableCollection<TweakItemViewModel>();
        ToggleExpandCommand = new RelayCommand(_ => IsExpanded = !IsExpanded);
    }

    public string CategoryName { get; }

    public string CategoryIcon { get; }

    public string DisplayName => $"{CategoryIcon} {CategoryName}";

    public ObservableCollection<TweakItemViewModel> Tweaks => _tweaks;

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
        _tweaks.Add(tweak);
        OnPropertyChanged(nameof(TweakCount));
        OnPropertyChanged(nameof(CountBadge));
        OnPropertyChanged(nameof(VisibleTweakCount));
    }

    public void ClearTweaks()
    {
        _tweaks.Clear();
        OnPropertyChanged(nameof(TweakCount));
        OnPropertyChanged(nameof(CountBadge));
        OnPropertyChanged(nameof(VisibleTweakCount));
    }
}
