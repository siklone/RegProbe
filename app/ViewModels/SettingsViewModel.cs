using System.Collections.Generic;
using System.ComponentModel;
using RegProbe.App.Services;

namespace RegProbe.App.ViewModels;

public sealed class SettingsViewModel : ViewModelBase, IDisposable
{
    private readonly SettingsWorkspaceCoordinator _coordinator = new();
    private bool _isDisposed;

    public SettingsViewModel()
    {
        _coordinator.PropertyChanged += OnCoordinatorPropertyChanged;
    }

    public string Title => _coordinator.Title;

    public IEnumerable<ThemePalette> AvailableThemes => _coordinator.AvailableThemes;

    public ThemePalette CurrentThemePalette
    {
        get => _coordinator.CurrentThemePalette;
        set => _coordinator.CurrentThemePalette = value;
    }

    public string StatusMessage => _coordinator.StatusMessage;

    public bool IsSaving => _coordinator.IsSaving;

    private void OnCoordinatorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _coordinator.PropertyChanged -= OnCoordinatorPropertyChanged;
        _coordinator.Dispose();
    }
}
