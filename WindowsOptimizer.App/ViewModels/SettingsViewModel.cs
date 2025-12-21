using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WindowsOptimizer.Infrastructure;
using WindowsOptimizer.Infrastructure.Discord;

namespace WindowsOptimizer.App.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsStore _settingsStore;
    private readonly DiscordWebhookClient _discordClient;
    private readonly RelayCommand _saveCommand;
    private readonly RelayCommand _sendTestCommand;
    private AppSettings _settings = new();
    private bool _isBusy;
    private string _statusMessage = "Settings are ready.";
    private string _discordWebhookUrl = string.Empty;
    private bool _discordAutoPatchEnabled;

    public SettingsViewModel()
    {
        var paths = AppPaths.FromEnvironment();
        _settingsStore = new SettingsStore(paths);
        _discordClient = new DiscordWebhookClient();

        _saveCommand = new RelayCommand(_ => _ = SaveAsync(), _ => !IsBusy);
        _sendTestCommand = new RelayCommand(_ => _ = SendTestAsync(), _ => CanSendTest());

        _ = LoadAsync();
    }

    public string Title => "Settings";

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string DiscordWebhookUrl
    {
        get => _discordWebhookUrl;
        set
        {
            if (SetProperty(ref _discordWebhookUrl, value))
            {
                _sendTestCommand.RaiseCanExecuteChanged();
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool DiscordAutoPatchEnabled
    {
        get => _discordAutoPatchEnabled;
        set
        {
            if (SetProperty(ref _discordAutoPatchEnabled, value))
            {
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand SaveCommand => _saveCommand;

    public ICommand SendTestCommand => _sendTestCommand;

    private bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                _saveCommand.RaiseCanExecuteChanged();
                _sendTestCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            _settings = await _settingsStore.LoadAsync(CancellationToken.None);
            DiscordWebhookUrl = _settings.DiscordWebhookUrl ?? string.Empty;
            DiscordAutoPatchEnabled = _settings.DiscordAutoPatchEnabled;
            StatusMessage = "Settings loaded.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load settings: {ex.Message}";
        }
    }

    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Saving settings...";

        try
        {
            _settings.DiscordWebhookUrl = DiscordWebhookUrl.Trim();
            _settings.DiscordAutoPatchEnabled = DiscordAutoPatchEnabled;

            await _settingsStore.SaveAsync(_settings, CancellationToken.None);
            StatusMessage = "Settings saved.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to save settings: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SendTestAsync()
    {
        if (IsBusy || !CanSendTest())
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Sending test message...";

        try
        {
            var result = await _discordClient.SendMessageAsync(
                DiscordWebhookUrl.Trim(),
                "Windows Optimizer: test ping from Settings.",
                CancellationToken.None);

            StatusMessage = result.Success
                ? "Discord test message sent."
                : $"Discord test failed (HTTP {result.StatusCode}).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Discord test failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSendTest()
    {
        return !IsBusy && Uri.TryCreate(DiscordWebhookUrl.Trim(), UriKind.Absolute, out _);
    }
}
