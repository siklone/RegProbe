using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using RegProbe.App.Services;
using RegProbe.App.Utilities;
using RegProbe.Infrastructure;

namespace RegProbe.App.ViewModels;

public sealed class SettingsViewModel : ViewModelBase, IDisposable
{
    private readonly ISettingsStore _settingsStore;
    private readonly RelayCommand _saveCommand;
    private readonly IAppLogger _appLogger;
    private readonly ThemeManager _themeManager = new();
    private ThemePalette _currentThemePalette = ThemeManager.Nord;
    private bool _isDarkTheme = true;
    private bool _runStartupScanOnLaunch = true;
    private bool _showPreviewHint = true;
    private bool _isSaving;
    private string _statusMessage = "Settings loaded.";
    private bool _isDisposed;

    public SettingsViewModel()
    {
        var paths = AppPaths.FromEnvironment();
        _settingsStore = new SettingsStore(paths);
        _appLogger = new FileAppLogger(paths);
        _saveCommand = new RelayCommand(_ => _ = SaveSettingsAsync(), _ => !IsSaving);
        _ = LoadSettingsAsync();
    }

    public string Title => "Settings";
    public string AppVersion => AppInfo.Version;
    public string BuildConfiguration => AppInfo.BuildConfiguration;
    public string Framework => AppInfo.FrameworkLabel;

    public IEnumerable<ThemePalette> AvailableThemes => new[]
    {
        ThemeManager.Nord,
        ThemeManager.ElectricPurple,
        ThemeManager.SunsetOrange,
        ThemeManager.CyberGreen,
        ThemeManager.RubyRed
    };

    public ThemePalette CurrentThemePalette
    {
        get => _currentThemePalette;
        set
        {
            if (SetProperty(ref _currentThemePalette, value))
            {
                _themeManager.ApplyTheme(value);
                _ = SaveSettingsAsync();
            }
        }
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                _themeManager.SetBaseTheme(value);
                _ = SaveSettingsAsync();
            }
        }
    }

    public bool RunStartupScanOnLaunch
    {
        get => _runStartupScanOnLaunch;
        set => SetProperty(ref _runStartupScanOnLaunch, value);
    }

    public bool ShowPreviewHint
    {
        get => _showPreviewHint;
        set => SetProperty(ref _showPreviewHint, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        set
        {
            if (SetProperty(ref _isSaving, value))
            {
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand SaveCommand => _saveCommand;

    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _settingsStore.LoadAsync(CancellationToken.None);
            _isDarkTheme = settings.Theme != "Light";
            _runStartupScanOnLaunch = settings.RunStartupScanOnLaunch;
            _showPreviewHint = settings.ShowPreviewHint;

            var matchedTheme = AvailableThemes.FirstOrDefault(theme => theme.Name == settings.Theme) ?? ThemeManager.Nord;
            _currentThemePalette = matchedTheme;

            UiPreferences.Current.EnableCardShadows = false;
            UiPreferences.Current.IsCompactMode = false;
            _themeManager.SetCardShadows(false);
            _themeManager.SetCompactMode(false);
            _themeManager.SetBaseTheme(_isDarkTheme);
            _themeManager.ApplyTheme(_currentThemePalette);

            OnPropertyChanged(nameof(CurrentThemePalette));
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(RunStartupScanOnLaunch));
            OnPropertyChanged(nameof(ShowPreviewHint));

            StatusMessage = "Settings loaded successfully.";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Failed to load settings: {ex.Message}";
        }
    }

    private async Task SaveSettingsAsync()
    {
        IsSaving = true;
        StatusMessage = "Saving settings...";

        try
        {
            var settings = await _settingsStore.LoadAsync(CancellationToken.None);
            var previousTheme = settings.Theme;
            var previousStartupScan = settings.RunStartupScanOnLaunch;
            var previousPreviewHint = settings.ShowPreviewHint;

            settings.Theme = CurrentThemePalette.Name;
            settings.EnableCardShadows = false;
            settings.IsCompactMode = false;
            settings.RunStartupScanOnLaunch = RunStartupScanOnLaunch;
            settings.ShowPreviewHint = ShowPreviewHint;

            await _settingsStore.SaveAsync(settings, CancellationToken.None);
            LogSettingsChanges(previousTheme, previousStartupScan, previousPreviewHint, settings);
            StatusMessage = "Settings saved successfully.";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Failed to save settings: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void LogSettingsChanges(string previousTheme, bool previousStartupScan, bool previousPreviewHint, AppSettings current)
    {
        var changes = new List<string>();

        if (!string.Equals(previousTheme, current.Theme, System.StringComparison.OrdinalIgnoreCase))
        {
            changes.Add($"Theme={current.Theme}");
        }

        if (previousStartupScan != current.RunStartupScanOnLaunch)
        {
            changes.Add($"StartupScan={(current.RunStartupScanOnLaunch ? "On" : "Off")}");
        }

        if (previousPreviewHint != current.ShowPreviewHint)
        {
            changes.Add($"PreviewHint={(current.ShowPreviewHint ? "On" : "Off")}");
        }

        if (changes.Count == 0)
        {
            return;
        }

        _appLogger.Log(LogLevel.Info, $"Activity: Settings - {string.Join(", ", changes)}");
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
    }
}
