using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using WindowsOptimizer.App.Services;

namespace WindowsOptimizer.App.ViewModels;

public sealed class PolicyReferencePanelViewModel : ViewModelBase
{
    private readonly PolicyReferenceCatalogBuilder _builder = new();
    private readonly Action<PolicyReferenceEntry> _openAction;
    private readonly ObservableCollection<PolicyReferenceItemViewModel> _items = new();
    private readonly ICollectionView _itemsView;
    private string _searchText = string.Empty;
    private int _policyBackedSettingCount;
    private int _componentCount;
    private int _machineScopedSettingCount;
    private int _userScopedSettingCount;

    public PolicyReferencePanelViewModel(Action<PolicyReferenceEntry> openAction)
    {
        _openAction = openAction ?? throw new ArgumentNullException(nameof(openAction));
        _itemsView = CollectionViewSource.GetDefaultView(_items);
        _itemsView.Filter = FilterItems;
    }

    public string Headline => "Windows Policy Reference";

    public string Detail => "Policy-backed settings help explain which parts of Windows and installed components are controlled through official policy paths.";

    public string Context => $"{PolicyBackedSettingCount} policy-backed settings across {ComponentCount} components.";

    public ObservableCollection<PolicyReferenceItemViewModel> Items => _items;

    public ICollectionView ItemsView => _itemsView;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _itemsView.Refresh();
                OnPropertyChanged(nameof(HasItems));
            }
        }
    }

    public int PolicyBackedSettingCount
    {
        get => _policyBackedSettingCount;
        private set
        {
            if (SetProperty(ref _policyBackedSettingCount, value))
            {
                OnPropertyChanged(nameof(Context));
            }
        }
    }

    public int ComponentCount
    {
        get => _componentCount;
        private set
        {
            if (SetProperty(ref _componentCount, value))
            {
                OnPropertyChanged(nameof(Context));
            }
        }
    }

    public int MachineScopedSettingCount
    {
        get => _machineScopedSettingCount;
        private set => SetProperty(ref _machineScopedSettingCount, value);
    }

    public int UserScopedSettingCount
    {
        get => _userScopedSettingCount;
        private set => SetProperty(ref _userScopedSettingCount, value);
    }

    public bool HasItems => _itemsView.Cast<object>().Any();

    public void Refresh(IEnumerable<TweakItemViewModel> tweaks)
    {
        var sourceItems = (tweaks ?? Enumerable.Empty<TweakItemViewModel>())
            .Select(static tweak => new PolicyReferenceSourceItem
            {
                Name = tweak.Name,
                Category = tweak.Category,
                Description = tweak.Description,
                EffectSummary = tweak.EffectSummary,
                RegistryPath = tweak.RegistryPath,
                Risk = tweak.Risk,
                HasDetectedState = tweak.HasDetectedState,
                IsApplied = tweak.IsApplied
            });

        var catalog = _builder.Build(sourceItems);
        _items.Clear();
        foreach (var entry in catalog.Entries)
        {
            _items.Add(new PolicyReferenceItemViewModel(entry, _openAction));
        }

        PolicyBackedSettingCount = catalog.PolicyBackedSettingCount;
        ComponentCount = catalog.ComponentCount;
        MachineScopedSettingCount = catalog.MachineScopedSettingCount;
        UserScopedSettingCount = catalog.UserScopedSettingCount;
        _itemsView.Refresh();
        OnPropertyChanged(nameof(HasItems));
    }

    private bool FilterItems(object obj)
    {
        if (obj is not PolicyReferenceItemViewModel item)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_searchText))
        {
            return true;
        }

        return item.ComponentName.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.ExampleSummary.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.PrimaryCategory.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.SearchFragment.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
    }
}
