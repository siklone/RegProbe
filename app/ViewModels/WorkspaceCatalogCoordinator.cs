using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using RegProbe.App.Services;
using RegProbe.Core;
using RegProbe.Core.Commands;
using RegProbe.Core.Files;
using RegProbe.Core.Models;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Core.Tasks;
using RegProbe.Engine;
using RegProbe.Engine.Services;
using RegProbe.Engine.Tweaks;
using RegProbe.Infrastructure;
using RegProbe.Infrastructure.Services;

namespace RegProbe.App.ViewModels;

public sealed class WorkspaceCatalogCoordinator
{
    private readonly IEnumerable<ITweakProvider>? _providerList;
    private readonly IRegistryAccessor _localRegistryAccessor;
    private readonly IRegistryAccessor _scanAwareElevatedRegistryAccessor;
    private readonly IServiceManager _elevatedServiceManager;
    private readonly IScheduledTaskManager _elevatedTaskManager;
    private readonly IFileSystemAccessor _elevatedFileSystemAccessor;
    private readonly ICommandRunner _elevatedCommandRunner;
    private readonly TweakExecutionPipeline _pipeline;
    private readonly bool _isElevated;
    private readonly IAppLogger _appLogger;
    private readonly PluginLoader _pluginLoader = new();

    public WorkspaceCatalogCoordinator(
        IEnumerable<ITweakProvider>? providerList,
        IRegistryAccessor localRegistryAccessor,
        IRegistryAccessor scanAwareElevatedRegistryAccessor,
        IServiceManager elevatedServiceManager,
        IScheduledTaskManager elevatedTaskManager,
        IFileSystemAccessor elevatedFileSystemAccessor,
        ICommandRunner elevatedCommandRunner,
        TweakExecutionPipeline pipeline,
        bool isElevated,
        IAppLogger appLogger)
    {
        _providerList = providerList;
        _localRegistryAccessor = localRegistryAccessor ?? throw new ArgumentNullException(nameof(localRegistryAccessor));
        _scanAwareElevatedRegistryAccessor = scanAwareElevatedRegistryAccessor ?? throw new ArgumentNullException(nameof(scanAwareElevatedRegistryAccessor));
        _elevatedServiceManager = elevatedServiceManager ?? throw new ArgumentNullException(nameof(elevatedServiceManager));
        _elevatedTaskManager = elevatedTaskManager ?? throw new ArgumentNullException(nameof(elevatedTaskManager));
        _elevatedFileSystemAccessor = elevatedFileSystemAccessor ?? throw new ArgumentNullException(nameof(elevatedFileSystemAccessor));
        _elevatedCommandRunner = elevatedCommandRunner ?? throw new ArgumentNullException(nameof(elevatedCommandRunner));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _isElevated = isElevated;
        _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
    }

    public void LoadInitialTweaks(ObservableCollection<TweakItemViewModel> tweaks)
    {
        ArgumentNullException.ThrowIfNull(tweaks);
        LoadProviderTweaks(tweaks);
        LoadPlugins(tweaks);
        ApplyTweakMetadata(tweaks);
    }

    public IDictionary<string, int> BuildWinConfigCategoryCoverageMap(IEnumerable<TweakItemViewModel> tweaks)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var tweak in tweaks ?? Enumerable.Empty<TweakItemViewModel>())
        {
            var categoryId = MapLocalCategoryToWinConfigId(tweak.Category);
            if (string.IsNullOrWhiteSpace(categoryId))
            {
                continue;
            }

            counts.TryGetValue(categoryId, out var current);
            counts[categoryId] = current + 1;
        }

        return counts;
    }

    private void LoadProviderTweaks(ObservableCollection<TweakItemViewModel> tweaks)
    {
        if (_providerList is null)
        {
            return;
        }

        var existingIds = BuildExistingIdSet(tweaks);
        var tweakContext = new TweakContext(
            _localRegistryAccessor,
            _scanAwareElevatedRegistryAccessor,
            _elevatedServiceManager,
            _elevatedTaskManager,
            _elevatedFileSystemAccessor,
            _elevatedCommandRunner);

        foreach (var provider in _providerList)
        {
            var providerTweaks = provider.CreateTweaks(_pipeline, tweakContext, _isElevated);
            foreach (var tweak in providerTweaks)
            {
                if (string.IsNullOrWhiteSpace(tweak.Id) || !existingIds.Add(tweak.Id))
                {
                    continue;
                }

                tweaks.Add(new TweakItemViewModel(tweak, _pipeline, _isElevated));
            }
        }
    }

    private void LoadPlugins(ObservableCollection<TweakItemViewModel> tweaks)
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _appLogger.Log(LogLevel.Info, $"Plugin discovery: baseDir='{baseDir}'");

            var existingIds = BuildExistingIdSet(tweaks);
            var pluginsPath = Path.Combine(baseDir, "Plugins");
            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
            }

            _appLogger.Log(LogLevel.Info, $"Plugin discovery: pluginsPath='{pluginsPath}'");

            var plugins = _pluginLoader.LoadPlugins(pluginsPath).ToList();
            _appLogger.Log(LogLevel.Info, $"Plugin discovery: loadedPlugins={plugins.Count}");

            foreach (var plugin in plugins)
            {
                _appLogger.Log(LogLevel.Info, $"Plugin loaded: name='{plugin.PluginName}' version='{plugin.Version}'");
                var pluginTweaks = plugin.GetTweaks()?.ToList() ?? new List<ITweak>();
                _appLogger.Log(LogLevel.Info, $"Plugin tweaks: plugin='{plugin.PluginName}' count={pluginTweaks.Count}");

                foreach (var tweak in pluginTweaks)
                {
                    if (string.IsNullOrWhiteSpace(tweak.Id) || !existingIds.Add(tweak.Id))
                    {
                        continue;
                    }

                    tweaks.Add(new TweakItemViewModel(tweak, _pipeline, _isElevated));
                }
            }
        }
        catch (Exception ex)
        {
            _appLogger.Log(LogLevel.Error, "Plugin system error", ex);
        }
    }

    private static void ApplyTweakMetadata(IEnumerable<TweakItemViewModel> tweaks)
    {
        var tweakList = (tweaks ?? Enumerable.Empty<TweakItemViewModel>()).ToList();

        var aeroShake = tweakList.FirstOrDefault(t => t.Id == "system.aero-shake");
        if (aeroShake != null)
        {
            aeroShake.RegistryPath = @"HKCU\Software\Policies\Microsoft\Windows\Explorer\NoWindowMinimizingShortcuts";
            aeroShake.CodeExample = "reg add \"HKCU\\Software\\Policies\\Microsoft\\Windows\\Explorer\" /v \"NoWindowMinimizingShortcuts\" /t REG_DWORD /d 1 /f";
            aeroShake.ReferenceLinks.Add(new ReferenceLink("Policy Documentation", "https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-explorer"));
        }

        var gameMode = tweakList.FirstOrDefault(t => t.Id == "system.enable-game-mode");
        if (gameMode != null)
        {
            gameMode.RegistryPath = @"HKCU\Software\Microsoft\GameBar\AutoGameModeEnabled";
            gameMode.CodeExample = "reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v \"AutoGameModeEnabled\" /t REG_DWORD /d 1 /f";
            gameMode.SubOptions.Add(new TweakSubOption("Enable Game Bar", TweakSubOptionType.Toggle) { IsEnabled = true });
            gameMode.SubOptions.Add(new TweakSubOption("Allow Background DVR", TweakSubOptionType.Toggle));
            gameMode.ReferenceLinks.Add(new ReferenceLink("Xbox Support", "https://support.xbox.com/en-US/help/games-apps/game-setup-and-play/use-game-mode-gaming-on-pc"));
        }

        var clipboard = tweakList.FirstOrDefault(t => t.Id == "system.disable-clipboard-history");
        if (clipboard != null)
        {
            clipboard.RegistryPath = @"HKLM\Software\Policies\Microsoft\Windows\System\AllowClipboardHistory";
            clipboard.CodeExample = "reg add \"HKLM\\Software\\Policies\\Microsoft\\Windows\\System\" /v \"AllowClipboardHistory\" /t REG_DWORD /d 0 /f";
            clipboard.TargetValue = "0 (Disabled)";
            clipboard.ReferenceLinks.Add(new ReferenceLink("Security Best Practices", "https://learn.microsoft.com/en-us/windows/security/threat-protection/security-policy-settings/user-rights-assignment"));
        }

        var edgeBoost = tweakList.FirstOrDefault(t => t.Id == "system.disable-edge-startup-boost");
        if (edgeBoost != null)
        {
            edgeBoost.ActionType = TweakActionType.Clean;
            edgeBoost.ActionButtonText = "Disable Boost";
            edgeBoost.RegistryPath = @"HKLM\Software\Policies\Microsoft\Edge\StartupBoostEnabled";
            edgeBoost.TargetValue = "0 (Disabled)";
        }

        var mitigations = tweakList.FirstOrDefault(t => t.Id == "security.disable-system-mitigations");
        if (mitigations != null)
        {
            mitigations.RegistryPath = @"HKLM\System\CurrentControlSet\Control\Session Manager\kernel\MitigationOptions";
            mitigations.CodeExample = "# View current mitigation options\nGet-ItemProperty -Path 'HKLM:\\System\\CurrentControlSet\\Control\\Session Manager\\kernel' -Name MitigationOptions\n\n# Set mitigations to 22202022... (Hex)";
            mitigations.TargetValue = "22202022 (Optimized)";
            mitigations.ReferenceLinks.Add(new ReferenceLink("Exploit Protection Reference", "https://learn.microsoft.com/en-us/microsoft-365/security/defender-endpoint/exploit-protection-reference"));
            mitigations.ReferenceLinks.Add(new ReferenceLink("Bypass Mitigations Guide", "https://github.com/SirenOfTitan/Exploit-Mitigations-Bypass"));
        }

        var priority = tweakList.FirstOrDefault(t => t.Id == "system.priority-control");
        if (priority != null)
        {
            priority.RegistryPath = @"HKLM\System\CurrentControlSet\Control\PriorityControl\Win32PrioritySeparation";
            priority.CodeExample = "Set-ItemProperty -Path 'HKLM:\\System\\CurrentControlSet\\Control\\PriorityControl' -Name Win32PrioritySeparation -Value 38";
            priority.PriorityCalculator = new PriorityCalculatorViewModel { Bitmask = 0x26 };
            priority.ReferenceLinks.Add(new ReferenceLink("MSDN PriorityControl", "https://learn.microsoft.com/en-us/windows/win32/procthread/scheduling-priorities"));
        }

        var vscode = tweakList.FirstOrDefault(t => t.Id == "misc.disable-vscode-telemetry");
        if (vscode != null)
        {
            vscode.CodeExample =
                "\"telemetry.telemetryLevel\": \"off\"\n" +
                "\"workbench.enableExperiments\": false\n" +
                "\"update.mode\": \"manual\"\n" +
                "\"extensions.autoUpdate\": false";
            vscode.ReferenceLinks.Add(new ReferenceLink("VS Code telemetry docs", "https://code.visualstudio.com/docs/getstarted/telemetry", kind: ReferenceLinkKind.Docs));
            vscode.ReferenceLinks.Add(new ReferenceLink("VS Code update behavior", "https://code.visualstudio.com/docs/setup/setup-overview#_updates", kind: ReferenceLinkKind.Docs));
        }
    }

    private static HashSet<string> BuildExistingIdSet(IEnumerable<TweakItemViewModel> tweaks)
    {
        return new HashSet<string>(
            (tweaks ?? Enumerable.Empty<TweakItemViewModel>())
                .Select(t => t.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id)),
            StringComparer.OrdinalIgnoreCase);
    }

    private static string? MapLocalCategoryToWinConfigId(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return null;
        }

        var normalized = category.Trim().ToLowerInvariant();

        if (normalized.Contains("network"))
            return "network";
        if (normalized.Contains("power"))
            return "power";
        if (normalized.Contains("privacy"))
            return "privacy";
        if (normalized.Contains("security"))
            return "security";
        if (normalized.Contains("system"))
            return "system";
        if (normalized.Contains("visibility") || normalized.Contains("display") || normalized.Contains("explorer"))
            return "visibility";
        if (normalized.Contains("peripheral") || normalized.Contains("input") || normalized.Contains("usb") || normalized.Contains("audio"))
            return "peripheral";
        if (normalized.Contains("nvidia") || normalized.Contains("graphics") || normalized.Contains("gpu"))
            return "nvidia";
        if (normalized.Contains("cleanup"))
            return "cleanup";
        if (normalized.Contains("policy"))
            return "policies";
        if (normalized.Contains("performance") || normalized.Contains("affinity"))
            return "affinities";
        if (normalized.Contains("misc"))
            return "misc";

        return null;
    }
}
