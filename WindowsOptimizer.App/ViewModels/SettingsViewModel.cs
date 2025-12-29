using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.Utilities;
using WindowsOptimizer.Infrastructure;

namespace WindowsOptimizer.App.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsStore _settingsStore;
    private readonly RelayCommand _saveCommand;
    private readonly RelayCommand _testWebhookCommand;
    private string _discordWebhookUrl = string.Empty;
    private bool _discordNotificationsEnabled;
    private bool _discordAutoPatchEnabled;
    private string _statusMessage = "Settings loaded.";
    private bool _isSaving;
    private bool _isTesting;
    private readonly RelayCommand _openUrlCommand;
    private bool _isDarkTheme = true;

    public SettingsViewModel()
    {
        var paths = AppPaths.FromEnvironment();
        _settingsStore = new SettingsStore(paths);

        _saveCommand = new RelayCommand(_ => _ = SaveSettingsAsync(), _ => !IsSaving);
        _testWebhookCommand = new RelayCommand(_ => _ = TestWebhookAsync(), _ => !IsTesting && !string.IsNullOrWhiteSpace(DiscordWebhookUrl));
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

        _ = LoadSettingsAsync();
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

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                ThemeManager.SetTheme(value ? AppTheme.Dark : AppTheme.Light);
                _ = SaveSettingsAsync();
            }
        }
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

    public ICommand OpenUrlCommand => _openUrlCommand;

    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _settingsStore.LoadAsync(CancellationToken.None);
            DiscordWebhookUrl = settings.DiscordWebhookUrl ?? string.Empty;
            DiscordNotificationsEnabled = settings.DiscordNotificationsEnabled;
            DiscordAutoPatchEnabled = settings.DiscordAutoPatchEnabled;
            _isDarkTheme = settings.Theme != "Light";
            OnPropertyChanged(nameof(IsDarkTheme));
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
            var settings = new AppSettings
            {
                DiscordWebhookUrl = string.IsNullOrWhiteSpace(DiscordWebhookUrl) ? null : DiscordWebhookUrl,
                DiscordNotificationsEnabled = DiscordNotificationsEnabled,
                DiscordAutoPatchEnabled = DiscordAutoPatchEnabled,
                Theme = IsDarkTheme ? "Dark" : "Light"
            };

            await _settingsStore.SaveAsync(settings, CancellationToken.None);
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
}
