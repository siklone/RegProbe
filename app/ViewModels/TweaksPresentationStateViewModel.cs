using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RegProbe.App.ViewModels;

public sealed class TweaksPresentationStateViewModel : ViewModelBase
{
    private string _filterSummary = "Showing 0 of 0 settings.";
    private bool _hasVisibleTweaks;
    private int _totalTweaksAvailable;
    private int _tweaksApplied;
    private int _tweaksRolledBack;

    public ObservableCollection<CategoryGroupViewModel> CategoryGroups { get; } = new();

    public string FilterSummary
    {
        get => _filterSummary;
        private set => SetProperty(ref _filterSummary, value);
    }

    public bool HasVisibleTweaks
    {
        get => _hasVisibleTweaks;
        private set => SetProperty(ref _hasVisibleTweaks, value);
    }

    public int TotalTweaksAvailable
    {
        get => _totalTweaksAvailable;
        private set => SetProperty(ref _totalTweaksAvailable, value);
    }

    public int TweaksApplied
    {
        get => _tweaksApplied;
        private set => SetProperty(ref _tweaksApplied, value);
    }

    public int TweaksRolledBack
    {
        get => _tweaksRolledBack;
        private set => SetProperty(ref _tweaksRolledBack, value);
    }

    public void SetFilterSummary(string summary, bool hasVisibleTweaks)
    {
        FilterSummary = summary;
        HasVisibleTweaks = hasVisibleTweaks;
    }

    public void SetInventoryCounts(int totalTweaksAvailable, int tweaksApplied, int tweaksRolledBack)
    {
        TotalTweaksAvailable = totalTweaksAvailable;
        TweaksApplied = tweaksApplied;
        TweaksRolledBack = tweaksRolledBack;
    }

    public void ReplaceCategoryGroups(IEnumerable<CategoryGroupViewModel> groups)
    {
        CategoryGroups.Clear();
        foreach (var group in groups)
        {
            CategoryGroups.Add(group);
        }
    }
}
