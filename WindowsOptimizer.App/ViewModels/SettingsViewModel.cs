using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.Utilities;
using WindowsOptimizer.Infrastructure;

namespace WindowsOptimizer.App.ViewModels;

public sealed class SettingsViewModel : ViewModelBase, IDisposable
{
    private bool _isDisposed;
    private readonly ISettingsStore _settingsStore;
    private readonly RelayCommand _saveCommand;
    private readonly RelayCommand _testWebhookCommand;
    private readonly RelayCommand _resetMonitorLayoutCommand;
    private readonly RelayCommand _applyDnsCommand;
    private readonly RelayCommand _flushDnsCommand;
    private string _discordWebhookUrl = string.Empty;
    private bool _discordNotificationsEnabled;
    private bool _discordAutoPatchEnabled;
    private string _statusMessage = "Settings loaded.";
    private bool _isSaving;
    private bool _isTesting;
    private readonly RelayCommand _openUrlCommand;
    private bool _isDarkTheme = true;
    private bool _enableCardShadows;
    private bool _runStartupScanOnLaunch = true;
    private bool _showPreviewHint = true;
    private readonly IAppLogger _appLogger;
    private readonly ThemeManager _themeManager = new();
    private readonly AutoUpdateService _autoUpdateService = new();
    private readonly ConfigExportService _configService = new();
    private readonly DnsService _dnsService = new();
    private AppSettings _settings = new();
    private ThemePalette _currentThemePalette = ThemeManager.Nord;

    public SettingsViewModel()
    {
        var paths = AppPaths.FromEnvironment();
        _settingsStore = new SettingsStore(paths);
        _appLogger = new FileAppLogger(paths);

        _saveCommand = new RelayCommand(_ => _ = SaveSettingsAsync(), _ => !IsSaving);
        _testWebhookCommand = new RelayCommand(_ => _ = TestWebhookAsync(), _ => !IsTesting && !string.IsNullOrWhiteSpace(DiscordWebhookUrl));
        _resetMonitorLayoutCommand = new RelayCommand(_ => _ = ResetMonitorLayoutAsync(), _ => !IsSaving);
        _openUrlCommand = new RelayCommand(parameter =>
        {
            if (parameter is not string url || string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore link launch failures
            }
        });

        CheckUpdatesCommand = new RelayCommand(async _ => await CheckUpdatesAsync());
        ExportConfigCommand = new RelayCommand(async _ => await ExportConfigAsync());
        ImportConfigCommand = new RelayCommand(async _ => await ImportConfigAsync());
        
        _applyDnsCommand = new RelayCommand(async _ => await ApplyDnsAsync(), _ => SelectedDnsProvider != null && !IsSaving);
        _flushDnsCommand = new RelayCommand(async _ => await FlushDnsAsync(), _ => !IsSaving);
        
        DnsProviders = new System.Collections.ObjectModel.ObservableCollection<Models.DnsProvider>(DnsService.GetProviders());
        _ = LoadSettingsAsync();
        _ = LoadDnsInfoAsync();
    }

    public string Title => "Settings";

    public string AppVersion => AppInfo.Version;

    public string BuildConfiguration => AppInfo.BuildConfiguration;

    public string Framework => AppInfo.FrameworkLabel;

    public string RepositoryUrl => AppInfo.RepositoryUrl;

    public string DiscordWebhookUrl
    {
        get => _discordWebhookUrl;
        set
        {
            if (SetProperty(ref _discordWebhookUrl, value))
            {
                _testWebhookCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool DiscordNotificationsEnabled
    {
        get => _discordNotificationsEnabled;
        set => SetProperty(ref _discordNotificationsEnabled, value);
    }

    public bool DiscordAutoPatchEnabled
    {
        get => _discordAutoPatchEnabled;
        set => SetProperty(ref _discordAutoPatchEnabled, value);
    }

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

    public bool IsCompactMode
    {
        get => UiPreferences.Current.IsCompactMode;
        set
        {
            if (UiPreferences.Current.IsCompactMode != value)
            {
                UiPreferences.Current.IsCompactMode = value;
                _themeManager.SetCompactMode(value);
                OnPropertyChanged();
                _ = SaveSettingsAsync();
            }
        }
    }

    public bool EnableCardShadows
    {
        get => _enableCardShadows;
        set
        {
            if (SetProperty(ref _enableCardShadows, value))
            {
                UiPreferences.Current.EnableCardShadows = value;
                _themeManager.SetCardShadows(value);
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

    public bool IsTesting
    {
        get => _isTesting;
        set
        {
            if (SetProperty(ref _isTesting, value))
            {
                _testWebhookCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand SaveCommand => _saveCommand;

    public ICommand TestWebhookCommand => _testWebhookCommand;

    public ICommand ResetMonitorLayoutCommand => _resetMonitorLayoutCommand;

    public ICommand OpenUrlCommand => _openUrlCommand;

    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _settingsStore.LoadAsync(CancellationToken.None);
            _settings = settings;
            DiscordWebhookUrl = settings.DiscordWebhookUrl ?? string.Empty;
            DiscordNotificationsEnabled = settings.DiscordNotificationsEnabled;
            DiscordAutoPatchEnabled = settings.DiscordAutoPatchEnabled;
            _isDarkTheme = settings.Theme != "Light";
            _enableCardShadows = settings.EnableCardShadows;
            RunStartupScanOnLaunch = settings.RunStartupScanOnLaunch;
            ShowPreviewHint = settings.ShowPreviewHint;
            
            // Map saved theme name to palette
            var savedThemeName = settings.Theme; // "Nord (Default)" or "Dark"
            var matchedTheme = System.Linq.Enumerable.FirstOrDefault(AvailableThemes, t => t.Name == savedThemeName) ?? ThemeManager.Nord;
            _currentThemePalette = matchedTheme;
            _themeManager.ApplyTheme(_currentThemePalette);
            
            OnPropertyChanged(nameof(CurrentThemePalette));
            OnPropertyChanged(nameof(CurrentThemePalette));
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(EnableCardShadows));
            OnPropertyChanged(nameof(IsCompactMode));
            UiPreferences.Current.EnableCardShadows = settings.EnableCardShadows;
            UiPreferences.Current.IsCompactMode = settings.IsCompactMode;
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
            var previous = new AppSettings
            {
                DiscordWebhookUrl = settings.DiscordWebhookUrl,
                DiscordNotificationsEnabled = settings.DiscordNotificationsEnabled,
                DiscordAutoPatchEnabled = settings.DiscordAutoPatchEnabled,
                Theme = settings.Theme,
                EnableCardShadows = settings.EnableCardShadows,
                RunStartupScanOnLaunch = settings.RunStartupScanOnLaunch,
                ShowPreviewHint = settings.ShowPreviewHint
            };
            settings.DiscordWebhookUrl = string.IsNullOrWhiteSpace(DiscordWebhookUrl) ? null : DiscordWebhookUrl;
            settings.DiscordNotificationsEnabled = DiscordNotificationsEnabled;
            settings.DiscordAutoPatchEnabled = DiscordAutoPatchEnabled;
            settings.Theme = CurrentThemePalette.Name; // Save the palette name instead of just "Dark/Light"
            settings.DiscordAutoPatchEnabled = DiscordAutoPatchEnabled;
            settings.Theme = CurrentThemePalette.Name; // Save the palette name instead of just "Dark/Light"
            settings.Theme = CurrentThemePalette.Name; // Save the palette name instead of just "Dark/Light"
            settings.EnableCardShadows = EnableCardShadows;
            settings.IsCompactMode = IsCompactMode;
            settings.RunStartupScanOnLaunch = RunStartupScanOnLaunch;
            settings.ShowPreviewHint = ShowPreviewHint;

            await _settingsStore.SaveAsync(settings, CancellationToken.None);
            _settings = settings;
            LogSettingsChanges(previous, settings);
            StatusMessage = "Settings saved successfully!";
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

    private async Task TestWebhookAsync()
    {
        IsTesting = true;
        StatusMessage = "Testing Discord webhook...";

        try
        {
            using var client = new Infrastructure.Discord.DiscordWebhookClient();
            var result = await client.SendMessageAsync(
                DiscordWebhookUrl,
                "🧪 Test message from Windows Optimizer Suite! Webhook is working correctly.",
                CancellationToken.None);

            if (result.Success)
            {
                StatusMessage = "✅ Webhook test successful!";
            }
            else
            {
                StatusMessage = $"❌ Webhook test failed: HTTP {result.StatusCode} - {result.ResponseBody}";
            }
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"❌ Webhook test failed: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    private async Task ResetMonitorLayoutAsync()
    {
        IsSaving = true;
        StatusMessage = "Resetting monitor layout...";

        try
        {
            var settings = await _settingsStore.LoadAsync(CancellationToken.None);
            settings.MonitorSections.Clear();
            await _settingsStore.SaveAsync(settings, CancellationToken.None);
            _settings = settings;
            StatusMessage = "Monitor layout reset. Reopen Monitor to reload defaults.";
            _appLogger.Log(LogLevel.Info, "Activity: Settings - Monitor layout reset");
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Failed to reset monitor layout: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void LogSettingsChanges(AppSettings before, AppSettings after)
    {
        var changes = new List<string>();

        if (!string.Equals(before.Theme, after.Theme, System.StringComparison.OrdinalIgnoreCase))
        {
            changes.Add($"Theme={after.Theme}");
        }

        if (before.EnableCardShadows != after.EnableCardShadows)
        {
            changes.Add($"CardShadows={(after.EnableCardShadows ? "On" : "Off")}");
        }

        if (before.IsCompactMode != after.IsCompactMode)
        {
            changes.Add($"CompactMode={(after.IsCompactMode ? "On" : "Off")}");
        }

        if (before.RunStartupScanOnLaunch != after.RunStartupScanOnLaunch)
        {
            changes.Add($"StartupScan={(after.RunStartupScanOnLaunch ? "On" : "Off")}");
        }

        if (before.ShowPreviewHint != after.ShowPreviewHint)
        {
            changes.Add($"PreviewHint={(after.ShowPreviewHint ? "On" : "Off")}");
        }

        if (before.DiscordNotificationsEnabled != after.DiscordNotificationsEnabled)
        {
            changes.Add($"DiscordNotifications={(after.DiscordNotificationsEnabled ? "On" : "Off")}");
        }

        if (before.DiscordAutoPatchEnabled != after.DiscordAutoPatchEnabled)
        {
            changes.Add($"DiscordAutoPatch={(after.DiscordAutoPatchEnabled ? "On" : "Off")}");
        }

        var beforeWebhook = string.IsNullOrWhiteSpace(before.DiscordWebhookUrl) ? "Empty" : "Set";
        var afterWebhook = string.IsNullOrWhiteSpace(after.DiscordWebhookUrl) ? "Empty" : "Set";
        if (!string.Equals(beforeWebhook, afterWebhook, System.StringComparison.Ordinal))
        {
            changes.Add($"Webhook={afterWebhook}");
        }

        if (changes.Count == 0)
        {
            return;
        }

        _appLogger.Log(LogLevel.Info, $"Activity: Settings - {string.Join(", ", changes)}");
    }
    public ICommand CheckUpdatesCommand { get; }
    public ICommand ExportConfigCommand { get; }
    public ICommand ImportConfigCommand { get; }
    public ICommand ApplyDnsCommand => _applyDnsCommand;
    public ICommand FlushDnsCommand => _flushDnsCommand;

    public System.Collections.ObjectModel.ObservableCollection<Models.DnsProvider> DnsProviders { get; }

    private Models.DnsProvider? _selectedDnsProvider;
    public Models.DnsProvider? SelectedDnsProvider
    {
        get => _selectedDnsProvider;
        set
        {
            if (SetProperty(ref _selectedDnsProvider, value))
            {
                _applyDnsCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private string _currentDnsInfo = "Loading...";
    public string CurrentDnsInfo
    {
        get => _currentDnsInfo;
        set => SetProperty(ref _currentDnsInfo, value);
    }
    
    private async Task LoadDnsInfoAsync()
    {
        try
        {
            var config = await _dnsService.GetCurrentDnsAsync();
            if (config != null)
            {
                CurrentDnsInfo = config.IsDhcp 
                    ? $"Automatic (DHCP) - {config.AdapterName}"
                    : $"{config.PrimaryDns}, {config.SecondaryDns} - {config.AdapterName}";
                
                var matched = _dnsService.DetectCurrentProvider(config);
                if (matched != null)
                {
                    SelectedDnsProvider = matched;
                }
            }
            else
            {
                CurrentDnsInfo = "Unknown or No Connection";
            }
        }
        catch
        {
            CurrentDnsInfo = "Error detecting DNS";
        }
    }

    private async Task ApplyDnsAsync()
    {
        if (SelectedDnsProvider == null) return;
        
        IsSaving = true;
        StatusMessage = $"Applying DNS ({SelectedDnsProvider.Name})...";
        
        try
        {
            var success = await _dnsService.SetDnsAsync(SelectedDnsProvider);
            if (success)
            {
                StatusMessage = "DNS settings applied successfully.";
                await LoadDnsInfoAsync();
            }
            else
            {
                StatusMessage = "Failed to apply DNS settings.";
            }
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"DNS apply error: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task FlushDnsAsync()
    {
        IsSaving = true;
        StatusMessage = "Flushing DNS cache...";
        
        try
        {
            var success = await _dnsService.FlushDnsCacheAsync();
            StatusMessage = success ? "DNS cache flushed." : "Failed to flush DNS cache.";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Flush error: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task CheckUpdatesAsync()
    {
        try
        {
            StatusMessage = "Checking for updates...";
            var update = await _autoUpdateService.CheckForUpdateAsync();
            
            if (update != null && update.IsUpdateAvailable)
            {
                StatusMessage = $"Update available: {update.ReleaseName}";
                if (System.Windows.MessageBox.Show($"New version {update.LatestVersion} is available.\n\n{update.ReleaseNotes}\n\nDownload now?", 
                    "Update Available", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information) == System.Windows.MessageBoxResult.Yes)
                {
                    StatusMessage = "Downloading update...";
                    await _autoUpdateService.DownloadAndInstallAsync(update);
                }
            }
            else
            {
                StatusMessage = "You are up to date.";
            }
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Update check failed: {ex.Message}";
        }
    }

    private async Task ExportConfigAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON Config (*.json)|*.json",
            FileName = $"WindowsOptimizer_Config_{System.DateTime.Now:yyyyMMdd}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            StatusMessage = "Exporting configuration...";
            var success = await _configService.ExportAsync(dialog.FileName, new ExportOptions());
            StatusMessage = success ? "Configuration exported successfully." : "Export failed.";
        }
    }

    private async Task ImportConfigAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON Config (*.json)|*.json"
        };

        if (dialog.ShowDialog() == true)
        {
            StatusMessage = "Importing configuration...";
            var result = await _configService.ImportAsync(dialog.FileName);
            StatusMessage = result.Success ? $"Import successful: {result.Message}" : $"Import failed: {result.Message}";
            
            if (result.Success)
            {
                await LoadSettingsAsync();
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        // Dispose services if they implement IDisposable
        (_autoUpdateService as System.IDisposable)?.Dispose();
        (_configService as System.IDisposable)?.Dispose();
        (_dnsService as System.IDisposable)?.Dispose();
    }
}
