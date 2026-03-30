using System;
using System.Threading.Tasks;
using RegProbe.App.Services;
using RegProbe.App.Services.TweakProviders;
using RegProbe.Engine.Services;
using RegProbe.Infrastructure;

namespace RegProbe.App.ViewModels;

public sealed class MainCompositionCoordinator : IDisposable
{
    public MainCompositionCoordinator(Action<string> logToFile)
    {
        ArgumentNullException.ThrowIfNull(logToFile);

        BusyService = new BusyService();

        var paths = AppPaths.FromEnvironment();
        var rollbackStore = new RollbackStateStore(paths);
        var providers = new ITweakProvider[]
        {
            new SystemTweakProvider(),
            new SystemRegistryTweakProvider(),
            new PrivacyTweakProvider(),
            new SecurityTweakProvider(),
            new NetworkTweakProvider(),
            new PowerTweakProvider(),
            new PeripheralTweakProvider(),
            new VisibilityTweakProvider(),
            new PerformanceTweakProvider(),
            new AudioTweakProvider(),
            new MiscTweakProvider()
        };

        WorkspaceViewModel = new TweaksViewModel(providers, BusyService);
        SettingsViewModel = new SettingsViewModel();
        var configurationViewModel = new ConfigurationShellViewModel(WorkspaceViewModel);
        var repairsViewModel = new RepairsShellViewModel(WorkspaceViewModel);
        var aboutViewModel = new AboutViewModel();

        RecoveryCoordinator = new MainRecoveryCoordinator(rollbackStore, WorkspaceViewModel, logToFile);
        ShellCoordinator = new MainShellCoordinator(
            configurationViewModel,
            repairsViewModel,
            SettingsViewModel,
            aboutViewModel,
            logToFile);
    }

    public IBusyService BusyService { get; }

    public TweaksViewModel WorkspaceViewModel { get; }

    public SettingsViewModel SettingsViewModel { get; }

    public MainRecoveryCoordinator RecoveryCoordinator { get; }

    public MainShellCoordinator ShellCoordinator { get; }

    public void Initialize()
    {
        ShellCoordinator.Initialize();
        _ = Task.Run(async () => await RecoveryCoordinator.InitializeAsync());
    }

    public void Dispose()
    {
        WorkspaceViewModel.Dispose();
        SettingsViewModel.Dispose();
    }
}
