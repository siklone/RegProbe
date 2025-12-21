using System.Windows.Input;
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

    public SettingsViewModel()
    {
        var paths = AppPaths.FromEnvironment();
        _settingsStore = new SettingsStore(paths);

        _saveCommand = new RelayCommand(_ => SaveSettings(), _ => !IsSaving);
        _testWebhookCommand = new RelayCommand(_ => _ = TestWebhookAsync(), _ => !IsTesting && !string.IsNullOrWhiteSpace(DiscordWebhookUrl));

        LoadSettings();
    }

    public string Title => "Settings";

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

    private void LoadSettings()
    {
        var settings = _settingsStore.Load();
        DiscordWebhookUrl = settings.DiscordWebhookUrl ?? string.Empty;
        DiscordNotificationsEnabled = settings.DiscordNotificationsEnabled;
        DiscordAutoPatchEnabled = settings.DiscordAutoPatchEnabled;
        StatusMessage = "Settings loaded successfully.";
    }

    private void SaveSettings()
    {
        IsSaving = true;
        StatusMessage = "Saving settings...";

        try
        {
            _settingsStore.Update(settings =>
            {
                settings.DiscordWebhookUrl = string.IsNullOrWhiteSpace(DiscordWebhookUrl) ? null : DiscordWebhookUrl;
                settings.DiscordNotificationsEnabled = DiscordNotificationsEnabled;
                settings.DiscordAutoPatchEnabled = DiscordAutoPatchEnabled;
            });

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

    private async System.Threading.Tasks.Task TestWebhookAsync()
    {
        IsTesting = true;
        StatusMessage = "Testing Discord webhook...";

        try
        {
            using var client = new Infrastructure.Discord.DiscordWebhookClient();
            var result = await client.SendMessageAsync(
                DiscordWebhookUrl,
                "🧪 Test message from Windows Optimizer Suite! Webhook is working correctly.",
                System.Threading.CancellationToken.None);

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
