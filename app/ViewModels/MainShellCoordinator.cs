using System;
using System.Windows.Input;

namespace RegProbe.App.ViewModels;

public sealed class MainShellCoordinator : ViewModelBase
{
    private readonly ConfigurationShellViewModel _configurationViewModel;
    private readonly RepairsShellViewModel _repairsViewModel;
    private readonly AboutViewModel _aboutViewModel;
    private readonly Action<string> _log;
    private ViewModelBase? _currentViewModel;

    public MainShellCoordinator(
        ConfigurationShellViewModel configurationViewModel,
        RepairsShellViewModel repairsViewModel,
        AboutViewModel aboutViewModel,
        Action<string> log)
    {
        _configurationViewModel = configurationViewModel ?? throw new ArgumentNullException(nameof(configurationViewModel));
        _repairsViewModel = repairsViewModel ?? throw new ArgumentNullException(nameof(repairsViewModel));
        _aboutViewModel = aboutViewModel ?? throw new ArgumentNullException(nameof(aboutViewModel));
        _log = log ?? throw new ArgumentNullException(nameof(log));

        ShowConfigurationCommand = new RelayCommand(_ => ShowConfiguration());
        ShowRepairsCommand = new RelayCommand(_ => ShowRepairs());
        ShowAboutCommand = new RelayCommand(_ => ShowAbout());
        FocusSearchCommand = new RelayCommand(_ => FocusSearch());
        ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
    }

    public RelayCommand ShowConfigurationCommand { get; }

    public RelayCommand ShowRepairsCommand { get; }

    public RelayCommand ShowAboutCommand { get; }

    public RelayCommand FocusSearchCommand { get; }

    public RelayCommand ClearFiltersCommand { get; }

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            try
            {
                _log($"CurrentViewModel setter: Setting to {value?.GetType().Name}");
                if (SetProperty(ref _currentViewModel, value))
                {
                    RaiseShellViewStateChanged();
                }

                _log("CurrentViewModel setter: Set complete");
            }
            catch (Exception ex)
            {
                _log($"CRASH in CurrentViewModel setter: {ex.Message}");
                _log($"Stack: {ex.StackTrace}");
                throw;
            }
        }
    }

    public bool IsConfigurationViewActive => ReferenceEquals(CurrentViewModel, _configurationViewModel);

    public bool IsRepairsViewActive => ReferenceEquals(CurrentViewModel, _repairsViewModel);

    public bool IsAboutViewActive => ReferenceEquals(CurrentViewModel, _aboutViewModel);

    public void Initialize()
    {
        ShowConfiguration();
    }

    public void ShowConfiguration()
    {
        _configurationViewModel.ShowConfigurationWorkspace();
        CurrentViewModel = _configurationViewModel;
    }

    public void ShowRepairs()
    {
        _repairsViewModel.ShowRepairsWorkspace();
        CurrentViewModel = _repairsViewModel;
    }

    public void ShowAbout()
    {
        CurrentViewModel = _aboutViewModel;
    }

    public void FocusSearch()
    {
        ShowConfiguration();
    }

    public void ClearFilters()
    {
        _configurationViewModel.ClearFilters();
        CurrentViewModel = _configurationViewModel;
    }

    private void RaiseShellViewStateChanged()
    {
        OnPropertyChanged(nameof(IsConfigurationViewActive));
        OnPropertyChanged(nameof(IsRepairsViewActive));
        OnPropertyChanged(nameof(IsAboutViewActive));
    }
}
