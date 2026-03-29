using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using RegProbe.App.Services;
using RegProbe.Infrastructure;

namespace RegProbe.App.ViewModels;

public sealed class WorkspaceCollectionCoordinator
{
    private readonly IFavoritesStore _favoritesStore;
    private readonly Func<TweakItemViewModel, ConfigurationWorkspaceKind> _workspaceKindProvider;
    private readonly HashSet<TweakItemViewModel> _trackedTweaks = [];

    public WorkspaceCollectionCoordinator(
        IFavoritesStore favoritesStore,
        Func<TweakItemViewModel, ConfigurationWorkspaceKind> workspaceKindProvider)
    {
        _favoritesStore = favoritesStore ?? throw new ArgumentNullException(nameof(favoritesStore));
        _workspaceKindProvider = workspaceKindProvider ?? throw new ArgumentNullException(nameof(workspaceKindProvider));
    }

    public ObservableCollection<RepairsItemViewModel> RepairsRows { get; } = new();

    public void HandleFavoriteChanged(TweakItemViewModel tweak, bool isFavorite)
    {
        ArgumentNullException.ThrowIfNull(tweak);

        if (isFavorite)
        {
            _favoritesStore.AddFavorite(tweak.Id);
        }
        else
        {
            _favoritesStore.RemoveFavorite(tweak.Id);
        }
    }

    public void HandleCollectionChanged(
        IEnumerable<TweakItemViewModel> allTweaks,
        NotifyCollectionChangedEventArgs e,
        PropertyChangedEventHandler propertyChangedHandler,
        Action<TweakItemViewModel, bool> favoriteChangedHandler)
    {
        ArgumentNullException.ThrowIfNull(allTweaks);
        ArgumentNullException.ThrowIfNull(e);
        ArgumentNullException.ThrowIfNull(propertyChangedHandler);
        ArgumentNullException.ThrowIfNull(favoriteChangedHandler);

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            ResetTrackedTweaks(allTweaks, propertyChangedHandler, favoriteChangedHandler);
            return;
        }

        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<TweakItemViewModel>())
            {
                SubscribeItem(item, propertyChangedHandler, favoriteChangedHandler);
                AddRepairsRow(item);
            }
        }

        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<TweakItemViewModel>())
            {
                UnsubscribeItem(item, propertyChangedHandler, favoriteChangedHandler);
                RemoveRepairsRow(item);
            }
        }
    }

    public void Dispose(
        PropertyChangedEventHandler propertyChangedHandler,
        Action<TweakItemViewModel, bool> favoriteChangedHandler)
    {
        ArgumentNullException.ThrowIfNull(propertyChangedHandler);
        ArgumentNullException.ThrowIfNull(favoriteChangedHandler);

        foreach (var tweak in _trackedTweaks.ToList())
        {
            UnsubscribeItem(tweak, propertyChangedHandler, favoriteChangedHandler);
        }

        foreach (var row in RepairsRows)
        {
            row.Dispose();
        }

        RepairsRows.Clear();
    }

    private void ResetTrackedTweaks(
        IEnumerable<TweakItemViewModel> allTweaks,
        PropertyChangedEventHandler propertyChangedHandler,
        Action<TweakItemViewModel, bool> favoriteChangedHandler)
    {
        foreach (var tweak in _trackedTweaks.ToList())
        {
            UnsubscribeItem(tweak, propertyChangedHandler, favoriteChangedHandler);
        }

        foreach (var row in RepairsRows)
        {
            row.Dispose();
        }

        RepairsRows.Clear();

        foreach (var tweak in allTweaks)
        {
            SubscribeItem(tweak, propertyChangedHandler, favoriteChangedHandler);
            AddRepairsRow(tweak);
        }
    }

    private void SubscribeItem(
        TweakItemViewModel item,
        PropertyChangedEventHandler propertyChangedHandler,
        Action<TweakItemViewModel, bool> favoriteChangedHandler)
    {
        if (!_trackedTweaks.Add(item))
        {
            return;
        }

        item.PropertyChanged += propertyChangedHandler;
        item.FavoriteChanged += favoriteChangedHandler;
        item.IsFavorite = _favoritesStore.IsFavorite(item.Id);
    }

    private void UnsubscribeItem(
        TweakItemViewModel item,
        PropertyChangedEventHandler propertyChangedHandler,
        Action<TweakItemViewModel, bool> favoriteChangedHandler)
    {
        if (!_trackedTweaks.Remove(item))
        {
            return;
        }

        item.PropertyChanged -= propertyChangedHandler;
        item.FavoriteChanged -= favoriteChangedHandler;
    }

    private void AddRepairsRow(TweakItemViewModel item)
    {
        if (_workspaceKindProvider(item) != ConfigurationWorkspaceKind.Maintenance)
        {
            return;
        }

        if (RepairsRows.Any(row => ReferenceEquals(row.Source, item)))
        {
            return;
        }

        RepairsRows.Add(new RepairsItemViewModel(item));
    }

    private void RemoveRepairsRow(TweakItemViewModel item)
    {
        var existing = RepairsRows.FirstOrDefault(row => ReferenceEquals(row.Source, item));
        if (existing is null)
        {
            return;
        }

        existing.Dispose();
        RepairsRows.Remove(existing);
    }
}
