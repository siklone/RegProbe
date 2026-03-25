using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using OpenTraceProject.App.Models;
using OpenTraceProject.App.Services;
using OpenTraceProject.App.Utilities;

namespace OpenTraceProject.App.ViewModels;

/// <summary>
/// ViewModel for DNS configuration panel in Tweaks > Network.
/// </summary>
public sealed class DnsConfigurationViewModel : ViewModelBase
{
    private readonly DnsService _dnsService = new();
    private DnsProvider? _selectedDnsProvider;
    private string _currentDnsInfo = "Loading...";
    private string _statusMessage = string.Empty;
    private bool _isBusy;

    public DnsConfigurationViewModel()
    {
        DnsProviders = new ObservableCollection<DnsProvider>(DnsService.GetProviders());
        
        ApplyDnsCommand = new RelayCommand(async _ => await ApplyDnsAsync(), _ => SelectedDnsProvider != null && !IsBusy);
        FlushDnsCommand = new RelayCommand(async _ => await FlushDnsAsync(), _ => !IsBusy);
        
        _ = LoadDnsInfoAsync();
    }

    public ObservableCollection<DnsProvider> DnsProviders { get; }

    public DnsProvider? SelectedDnsProvider
    {
        get => _selectedDnsProvider;
        set
        {
            if (SetProperty(ref _selectedDnsProvider, value))
            {
                ((RelayCommand)ApplyDnsCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string CurrentDnsInfo
    {
        get => _currentDnsInfo;
        set => SetProperty(ref _currentDnsInfo, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ((RelayCommand)ApplyDnsCommand).RaiseCanExecuteChanged();
                ((RelayCommand)FlushDnsCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand ApplyDnsCommand { get; }
    public ICommand FlushDnsCommand { get; }

    public async Task LoadDnsInfoAsync()
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

        IsBusy = true;
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
                StatusMessage = "Failed to apply DNS settings. Run as Administrator.";
            }
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"DNS apply error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task FlushDnsAsync()
    {
        IsBusy = true;
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
            IsBusy = false;
        }
    }
}
