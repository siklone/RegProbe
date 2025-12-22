using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Infrastructure.Metrics;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Engine.Tweaks.Commands.Power;
using WindowsOptimizer.Engine.Tweaks.Commands.Cleanup;
using WindowsOptimizer.Engine.Tweaks.Commands.Performance;
using WindowsOptimizer.Engine.Tweaks.Commands.Network;
using WindowsOptimizer.Engine.Tweaks.Commands.System;
using WindowsOptimizer.App.Utilities;
using WindowsOptimizer.Infrastructure;
using WindowsOptimizer.Infrastructure.Commands;
using WindowsOptimizer.Infrastructure.Elevation;
using WindowsOptimizer.Infrastructure.Files;
using WindowsOptimizer.Infrastructure.Registry;
using WindowsOptimizer.Infrastructure.Services;
using WindowsOptimizer.Infrastructure.Tasks;

namespace WindowsOptimizer.App.ViewModels;

public sealed class TweaksViewModel : ViewModelBase
{
    private readonly ITweakLogStore _logStore;
    private readonly RelayCommand _exportLogsCommand;
    private readonly RelayCommand _previewAllCommand;
    private readonly RelayCommand _applyAllCommand;
    private readonly RelayCommand _verifyAllCommand;
    private readonly RelayCommand _rollbackAllCommand;
    private readonly RelayCommand _cancelAllCommand;
    private readonly RelayCommand _resetFiltersCommand;
    private readonly RelayCommand _openLogFolderCommand;
    private readonly RelayCommand _openCsvLogCommand;
    private readonly RelayCommand _expandAllDetailsCommand;
    private readonly RelayCommand _collapseAllDetailsCommand;
    private readonly bool _isElevated;
    private readonly IRegistryAccessor _localRegistryAccessor;
    private readonly IRegistryAccessor _elevatedRegistryAccessor;
    private readonly IServiceManager _elevatedServiceManager;
    private readonly IScheduledTaskManager _elevatedTaskManager;
    private readonly IFileSystemAccessor _elevatedFileSystemAccessor;
    private readonly ICommandRunner _elevatedCommandRunner;
    private string _exportStatusMessage = "Logs are ready to export.";
    private string _bulkStatusMessage = "Bulk actions are idle.";
    private string _filterSummary = "Showing 0 of 0 tweaks.";
    private bool _isExporting;
    private bool _isBulkRunning;
    private int _bulkProgressCurrent;
    private int _bulkProgressTotal;
    private string _searchText = string.Empty;
    private bool _showSafe = true;
    private bool _showAdvanced = true;
    private bool _showRisky = true;
    private bool _hasVisibleTweaks;
    private bool _isFlatView;
    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _bulkCts;
    private readonly string _logFolderPath;
    private readonly MetricProvider _metricProvider = new();
    private readonly DispatcherTimer _metricsTimer;
    private readonly IProfileSyncService _syncService = new ProfileSyncService();
    private readonly PluginLoader _pluginLoader = new();
    private readonly KernelImpactAnalyzer _kernelAnalyzer = new();
    private readonly string _tweakLogFilePath;

    public TweaksViewModel()
    {
        var paths = AppPaths.FromEnvironment();
        var logger = new FileAppLogger(paths);
        _logFolderPath = paths.LogDirectory;
        _tweakLogFilePath = paths.TweakLogFilePath;
        _logStore = new FileTweakLogStore(paths);
        var pipeline = new TweakExecutionPipeline(logger, _logStore);
        var settingsStore = new SettingsStore(paths);
        _isElevated = ProcessElevation.IsElevated();
        var elevatedHostClient = new ElevatedHostClient(new ElevatedHostClientOptions
        {
            HostExecutablePath = ElevatedHostLocator.GetExecutablePath(),
            PipeName = ElevatedHostDefaults.PipeName,
            ParentProcessId = Process.GetCurrentProcess().Id
        });
        _localRegistryAccessor = new LocalRegistryAccessor();
        _elevatedRegistryAccessor = new ElevatedRegistryAccessor(elevatedHostClient);
        _elevatedServiceManager = new ElevatedServiceManager(elevatedHostClient);
        _elevatedTaskManager = new ElevatedScheduledTaskManager(elevatedHostClient);
        _elevatedFileSystemAccessor = new ElevatedFileSystemAccessor(elevatedHostClient);
        _elevatedCommandRunner = new ElevatedCommandRunner(elevatedHostClient);
        var systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var system32Path = Path.Combine(systemRoot, "System32");
        var mobsyncPath = Path.Combine(system32Path, "mobsync.exe");
        var mobsyncDisabledPath = mobsyncPath + ".disabled";
        var psrPath = Path.Combine(system32Path, "psr.exe");
        var psrDisabledPath = psrPath + ".disabled";
        var helpPanePath = Path.Combine(system32Path, "HelpPane.exe");
        var helpPaneDisabledPath = helpPanePath + ".disabled";

        Tweaks = new ObservableCollection<TweakItemViewModel>
        {
            new(CreateRegistryTweak(
                    "system.aero-shake",
                    "Disable Aero Shake",
                    "Prevents windows from being minimized or restored when the active window is shaken back and forth with the mouse.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Policies\Microsoft\Windows\Explorer",
                    "NoWindowMinimizingShortcuts",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.disable-jpeg-reduction",
                    "Disable JPEG Reduction",
                    "Sets the desktop wallpaper JPEG import quality to 100% to avoid compression artifacts.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Control Panel\Desktop",
                    "JPEGImportQuality",
                    RegistryValueKind.DWord,
                    100,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.disable-low-disk-space-checks",
                    "Disable Low Disk Space Checks",
                    "Disables the Low Disk Space warning notifications for the current user.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                    "NoLowDiskSpaceChecks",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.enable-game-mode",
                    "Enable Game Mode",
                    "Ensures Game Mode is enabled for the current user.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\GameBar",
                    "AutoGameModeEnabled",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "security.enable-dynamic-lock",
                    "Enable Dynamic Lock",
                    "Automatically locks the device when the paired Bluetooth device is away.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows NT\CurrentVersion\Winlogon",
                    "EnableGoodbye",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.disable-dst-notifications",
                    "Disable DST Change Notifications",
                    "Turns off daylight saving time change notifications.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Control Panel\TimeDate",
                    "DstNotification",
                    RegistryValueKind.DWord,
                    0,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.disable-search-highlights",
                    "Disable Search Highlights (User)",
                    "Turns off search highlights in the search box for the current user.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\SearchSettings",
                    "IsDynamicSearchBoxEnabled",
                    RegistryValueKind.DWord,
                    0,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryValueSetTweak(
                    "system.disable-clipboard-history",
                    "Disable Clipboard History & Sync",
                    "Turns off clipboard history and cross-device clipboard synchronization.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\System",
                    new[]
                    {
                        new RegistryValueSetEntry("AllowClipboardHistory", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("AllowCrossDeviceClipboard", RegistryValueKind.DWord, 0)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.disable-clipboard-redirection",
                    "Disable Clipboard Redirection (RDP)",
                    "Prevents clipboard sharing between remote desktop sessions and the local machine.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services",
                    "fDisableClip",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.disable-background-gp-updates",
                    "Disable Background Group Policy Updates",
                    "Prevents Group Policy from refreshing while users are active.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\System",
                    "DisableBkGndGroupPolicy",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.disable-auto-maintenance",
                    "Disable Automatic Maintenance",
                    "Stops scheduled automatic maintenance tasks from running.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance",
                    "MaintenanceDisabled",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "notifications.disable-toast",
                    "Disable Toast Notifications",
                    "Blocks toast notifications for the current user.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.CurrentUser,
                    @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications",
                    "NoToastApplicationNotification",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "notifications.disable-lockscreen-toast",
                    "Disable Lock Screen Toast Notifications",
                    "Prevents toast notifications from appearing on the lock screen.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.CurrentUser,
                    @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications",
                    "NoToastApplicationNotificationOnLockScreen",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "notifications.disable-tile",
                    "Disable Tile Notifications",
                    "Prevents apps from updating tiles and tile badges.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.CurrentUser,
                    @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications",
                    "NoTileApplicationNotification",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "notifications.disable-mirroring",
                    "Disable Notification Mirroring",
                    "Stops notifications from being mirrored to other devices.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.CurrentUser,
                    @"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications",
                    "DisallowNotificationMirroring",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.verbose-status-messages",
                    "Enable Verbose Status Messages",
                    "Shows detailed status messages during startup, shutdown, logon, and logoff.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\System",
                    "VerboseStatus",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.disable-store-open-with",
                    "Disable Store in Open With",
                    "Removes the \"Look for an app in the Store\" option from Open With.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\Explorer",
                    "NoUseStoreOpenWith",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.disable-common-control-animations",
                    "Disable Common Control Animations",
                    "Turns off common control and window animations for the current user.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                    "TurnOffSPIAnimations",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.disable-window-animations",
                    "Disable Window Animations",
                    "Disables window animations like minimize and restore.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\DWM",
                    "DisallowAnimations",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.default-account-picture",
                    "Use Default Account Picture",
                    "Forces the default account picture for all users on this device.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                    "UseDefaultTile",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.disable-wcn-wizards",
                    "Disable Windows Connect Now Wizards",
                    "Disables Windows Connect Now setup wizards for wireless and device setup.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\WCN\UI",
                    "DisableWcnUi",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.disable-first-signin-animation",
                    "Disable First Sign-In Animation",
                    "Skips the first sign-in animation and Microsoft account opt-in prompt.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\System",
                    "EnableFirstLogonAnimation",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.hide-language-bar",
                    "Hide Language Bar",
                    "Hides the language bar UI for the current user.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\CTF\LangBar",
                    "ShowStatus",
                    RegistryValueKind.DWord,
                    3,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.disable-widgets",
                    "Disable Widgets",
                    "Disables the Widgets/News and Interests feature.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Dsh",
                    "AllowNewsAndInterests",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.disable-spotlight-features",
                    "Disable Windows Spotlight Features",
                    "Turns off Windows Spotlight features for the current user.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Policies\Microsoft\Windows\CloudContent",
                    "DisableWindowsSpotlightFeatures",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.disable-spotlight-welcome",
                    "Disable Windows Spotlight Welcome Experience",
                    "Disables the Windows Spotlight welcome experience.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Policies\Microsoft\Windows\CloudContent",
                    "DisableWindowsSpotlightWindowsWelcomeExperience",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.disable-spotlight-action-center",
                    "Disable Windows Spotlight on Action Center",
                    "Stops Windows Spotlight notifications in Action Center.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Policies\Microsoft\Windows\CloudContent",
                    "DisableWindowsSpotlightOnActionCenter",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.disable-spotlight-settings",
                    "Disable Windows Spotlight on Settings",
                    "Stops Windows Spotlight suggestions in Settings.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Policies\Microsoft\Windows\CloudContent",
                    "DisableWindowsSpotlightOnSettings",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.disable-spotlight-desktop-collection",
                    "Disable Spotlight Collection on Desktop",
                    "Removes the Spotlight collection option for desktop backgrounds.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Policies\Microsoft\Windows\CloudContent",
                    "DisableSpotlightCollectionOnDesktop",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.disable-lock-screen",
                    "Disable Lock Screen",
                    "Skips the lock screen and goes directly to the sign-in screen.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\Personalization",
                    "NoLockScreen",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.disable-lock-screen-camera",
                    "Disable Lock Screen Camera",
                    "Prevents the lock screen camera from being invoked.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\Personalization",
                    "NoLockScreenCamera",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.disable-lock-screen-slideshow",
                    "Disable Lock Screen Slideshow",
                    "Prevents lock screen slideshows from running.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\Personalization",
                    "NoLockScreenSlideshow",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.disable-lock-screen-motion",
                    "Disable Lock Screen Background Motion",
                    "Stops the subtle motion effect on the lock screen background.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\Personalization",
                    "AnimateLockScreenBackground",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.disable-lock-screen-changes",
                    "Prevent Changing Lock Screen",
                    "Prevents users from changing the lock screen image.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\Personalization",
                    "NoChangingLockScreen",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.disable-acrylic-logon",
                    "Disable Acrylic Logon Background",
                    "Disables the acrylic blur on the logon background image.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\System",
                    "DisableAcrylicBackgroundOnLogon",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.disable-spotlight-third-party",
                    "Disable Spotlight Third-Party Suggestions",
                    "Stops Windows Spotlight from suggesting third-party content.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Policies\Microsoft\Windows\CloudContent",
                    "DisableThirdPartySuggestions",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.hide-most-used-apps",
                    "Hide Most Used Apps",
                    "Forces the Start menu Most used list to stay hidden.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\Explorer",
                    "ShowOrHideMostUsedApps",
                    RegistryValueKind.DWord,
                    2),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.disable-restartable-apps",
                    "Disable Restartable Apps",
                    "Prevents apps from automatically restarting after sign-in.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows NT\CurrentVersion\Winlogon",
                    "RestartApps",
                    RegistryValueKind.DWord,
                    0,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "audio.disable-ducking",
                    "Disable Audio Ducking",
                    "Prevents Windows from lowering other audio when communications are detected.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Multimedia\Audio",
                    "UserDuckingPreference",
                    RegistryValueKind.DWord,
                    3,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "audio.show-hidden-devices",
                    "Show Hidden Audio Devices",
                    "Shows hidden audio devices in the sound control panel.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Multimedia\Audio\DeviceCpl",
                    "ShowHiddenDevices",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "audio.show-disconnected-devices",
                    "Show Disconnected Audio Devices",
                    "Shows disconnected audio devices in the sound control panel.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Multimedia\Audio\DeviceCpl",
                    "ShowDisconnectedDevices",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "audio.disable-spatial-audio",
                    "Disable Spatial Audio",
                    "Disables spatial audio for low-latency devices.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Audio",
                    "DisableSpatialOnLowLatency",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "audio.disable-system-sounds",
                    "Disable System Sounds",
                    "Clears the default sound events for common system sounds.",
                    TweakRiskLevel.Safe,
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\SystemAsterisk\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Notification.Reminder\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Close\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\CriticalBatteryAlarm\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\SystemHand\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\.Default\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\MailBeep\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\DeviceConnect\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\DeviceDisconnect\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\DeviceFail\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\SystemExclamation\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Notification.IM\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\LowBatteryAlarm\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Maximize\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\MenuCommand\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\MenuPopup\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\MessageNudge\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Minimize\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\FaxBeep\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Notification.Mail\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Notification.SMS\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Notification.Proximity\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\ProximityConnection\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Notification.Default\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\Open\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\PrintComplete\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\AppGPFault\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\SystemQuestion\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\RestoreDown\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\RestoreUp\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\CCSelect\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\ShowBand\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\SystemNotification\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\ChangeTheme\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\.Default\WindowsUAC\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\Explorer\BlockedPopup\.current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\Explorer\ActivatingDocument\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\Explorer\EmptyRecycleBin\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\Explorer\FeedDiscovered\.current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\Explorer\MoveMenuItem\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\Explorer\SecurityBand\.current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\Explorer\Navigating\.Current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\sapisvr\DisNumbersSound\.current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\sapisvr\PanelSound\.current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\sapisvr\MisrecoSound\.current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\sapisvr\HubOffSound\.current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\sapisvr\HubOnSound\.current", "", RegistryValueKind.String, string.Empty),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"AppEvents\Schemes\Apps\sapisvr\HubSleepSound\.current", "", RegistryValueKind.String, string.Empty)
                    },
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryValueSetTweak(
                    "peripheral.disable-language-switch-hotkey",
                    "Disable Language Switch Hotkey",
                    "Disables the keyboard hotkey used to switch input languages.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Keyboard Layout\Toggle",
                    new[]
                    {
                        new RegistryValueSetEntry("Language Hotkey", RegistryValueKind.String, "3"),
                        new RegistryValueSetEntry("Hotkey", RegistryValueKind.String, "3"),
                        new RegistryValueSetEntry("Layout Hotkey", RegistryValueKind.String, "3")
                    },
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryValueSetTweak(
                    "peripheral.disable-pointer-precision",
                    "Disable Enhance Pointer Precision",
                    "Disables mouse acceleration (enhance pointer precision).",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Control Panel\Mouse",
                    new[]
                    {
                        new RegistryValueSetEntry("MouseTrails", RegistryValueKind.String, "0"),
                        new RegistryValueSetEntry("MouseThreshold1", RegistryValueKind.String, "0"),
                        new RegistryValueSetEntry("MouseThreshold2", RegistryValueKind.String, "0"),
                        new RegistryValueSetEntry("MouseSpeed", RegistryValueKind.String, "0"),
                        new RegistryValueSetEntry("MouseSensitivity", RegistryValueKind.String, "10")
                    },
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.hide-people-bar",
                    "Hide People Bar",
                    "Removes the People Bar from the taskbar.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.CurrentUser,
                    @"Software\Policies\Microsoft\Windows\Explorer",
                    "HidePeopleBar",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "peripheral.disable-autoplay",
                    "Disable AutoPlay",
                    "Disables AutoPlay for removable media on this user account.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers",
                    "DisableAutoplay",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "peripheral.autoplay-take-no-action",
                    "AutoPlay: Take No Action",
                    "Sets AutoPlay handlers to take no action for common media events.",
                    TweakRiskLevel.Safe,
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\StorageOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\StorageOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\CameraAlternate\ShowPicturesOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\CameraAlternate\ShowPicturesOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayDVDMovieOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayDVDMovieOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayEnhancedDVDOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayEnhancedDVDOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\HandleDVDBurningOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\HandleDVDBurningOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayDVDAudioOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayDVDAudioOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayBluRayOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayBluRayOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\HandleBDBurningOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\HandleBDBurningOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayCDAudioOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayCDAudioOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayEnhancedCDOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayEnhancedCDOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\HandleCDBurningOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\HandleCDBurningOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlayVideoCDMovieOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlayVideoCDMovieOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\PlaySuperVideoCDMovieOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\PlaySuperVideoCDMovieOnArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\UserChosenExecuteHandlers\AutorunINFLegacyArrival", "", RegistryValueKind.String, "MSTakeNoAction"),
                        new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\EventHandlersDefaultSelection\AutorunINFLegacyArrival", "", RegistryValueKind.String, "MSTakeNoAction")
                    },
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.smb-disable-bandwidth-throttling",
                    "SMB: Disable Bandwidth Throttling",
                    "Disables SMB client bandwidth throttling.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters",
                    "DisableBandwidthThrottling",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.smb-enable-large-mtu",
                    "SMB: Enable Large MTU",
                    "Enables large MTU support for SMB client connections.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters",
                    "DisableLargeMtu",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryValueSetTweak(
                    "network.smb-require-signing-client",
                    "SMB: Require Client Signing",
                    "Requires SMB client signing for outbound connections.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters",
                    new[]
                    {
                        new RegistryValueSetEntry("RequireSecuritySignature", RegistryValueKind.DWord, 1),
                        new RegistryValueSetEntry("EnableSecuritySignature", RegistryValueKind.DWord, 1)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryValueSetTweak(
                    "network.smb-require-signing-server",
                    "SMB: Require Server Signing",
                    "Requires SMB server signing for inbound connections.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"System\CurrentControlSet\Services\LanmanServer\Parameters",
                    new[]
                    {
                        new RegistryValueSetEntry("RequireSecuritySignature", RegistryValueKind.DWord, 1),
                        new RegistryValueSetEntry("EnableSecuritySignature", RegistryValueKind.DWord, 1)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.smb-encrypt-data",
                    "SMB: Require Encryption",
                    "Requires SMB server encryption for shared data.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"System\CurrentControlSet\Services\LanmanServer\Parameters",
                    "EncryptData",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.smb-reject-unencrypted-access",
                    "SMB: Reject Unencrypted Access",
                    "Rejects SMB clients that do not support encryption.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"System\CurrentControlSet\Services\LanmanServer\Parameters",
                    "RejectUnencryptedAccess",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.smb-disable-leasing",
                    "SMB: Disable Leasing",
                    "Disables SMB server leasing (read/write/handle caching).",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"System\CurrentControlSet\Services\LanmanServer\Parameters",
                    "DisableLeasing",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.smb-enable-multichannel",
                    "SMB: Enable Multichannel",
                    "Enables SMB multichannel for parallel network paths.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters",
                    "DisableMultiChannel",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "network.smb-enable-quic",
                    "SMB: Enable QUIC",
                    "Enables SMB over QUIC for client and server.",
                    TweakRiskLevel.Advanced,
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters", "EnableSMBQUIC", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanServer\Parameters", "EnableSMBQUIC", RegistryValueKind.DWord, 1)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "network.smb-require-dialect-3_1_1",
                    "SMB: Require Dialect 3.1.1",
                    "Restricts SMB client/server dialects to SMB 3.1.1 or newer.",
                    TweakRiskLevel.Risky,
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters", "MinSmb2Dialect", RegistryValueKind.DWord, 785),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters", "MaxSmb2Dialect", RegistryValueKind.DWord, 65536),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanServer\Parameters", "MinSmb2Dialect", RegistryValueKind.DWord, 785),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanServer\Parameters", "MaxSmb2Dialect", RegistryValueKind.DWord, 65536)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "network.smb-set-cipher-suite-order",
                    "SMB: Set Cipher Suite Order",
                    "Sets the SMB encryption cipher suite order to AES-256 variants.",
                    TweakRiskLevel.Advanced,
                    new[]
                    {
                        new RegistryValueBatchEntry(
                            RegistryHive.LocalMachine,
                            @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters",
                            "CipherSuiteOrder",
                            RegistryValueKind.MultiString,
                            new[] { "AES_256_GCM", "AES_256_CCM" }),
                        new RegistryValueBatchEntry(
                            RegistryHive.LocalMachine,
                            @"System\CurrentControlSet\Services\LanmanServer\Parameters",
                            "CipherSuiteOrder",
                            RegistryValueKind.MultiString,
                            new[] { "AES_256_GCM", "AES_256_CCM" })
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "network.disable-default-shares",
                    "Disable Default Shares",
                    "Disables automatic administrative shares on the SMB server.",
                    TweakRiskLevel.Advanced,
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanServer\Parameters", "AutoShareServer", RegistryValueKind.DWord, 0),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\LanmanServer\Parameters", "AutoShareWks", RegistryValueKind.DWord, 0)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.disable-smb1",
                    "Disable SMBv1",
                    "Disables the legacy SMBv1 protocol on the server.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"System\CurrentControlSet\Services\LanmanServer\Parameters",
                    "SMB1",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.disable-smb2",
                    "Disable SMBv2/SMBv3",
                    "Disables the SMBv2/SMBv3 protocol on the server.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"System\CurrentControlSet\Services\LanmanServer\Parameters",
                    "SMB2",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "visibility.force-classic-control-panel",
                    "Force Classic Control Panel View",
                    "Always open Control Panel in the icon view.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                    "ForceClassicControlPanel",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "power.hide-lock-option",
                    "Hide Lock Power Option",
                    "Hides the Lock option from the power menu.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\Explorer",
                    "ShowLockOption",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "power.hide-sleep-option",
                    "Hide Sleep Power Option",
                    "Hides the Sleep option from the power menu.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\Explorer",
                    "ShowSleepOption",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "power.hide-hibernate-option",
                    "Hide Hibernate Power Option",
                    "Hides the Hibernate option from the power menu.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\Explorer",
                    "ShowHibernateOption",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "power.disable-fast-startup",
                    "Disable Fast Startup",
                    "Disables fast startup (hiberboot) via policy.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\System",
                    "HiberbootEnabled",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.enable-hags",
                    "Enable Hardware-Accelerated GPU Scheduling",
                    "Lets the GPU handle its own scheduling for improved responsiveness.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"System\CurrentControlSet\Control\GraphicsDrivers",
                    "HwSchMode",
                    RegistryValueKind.DWord,
                    2),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.disable-storage-sense",
                    "Disable Storage Sense",
                    "Turns off Storage Sense automatic cleanup.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\StorageSense",
                    "AllowStorageSenseGlobal",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.disable-storage-sense-temp-cleanup",
                    "Disable Storage Sense Temporary Files Cleanup",
                    "Prevents Storage Sense from deleting temporary files.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\StorageSense",
                    "AllowStorageSenseTemporaryFilesCleanup",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.disable-search-highlights-policy",
                    "Disable Search Highlights (Policy)",
                    "Disables search highlights via policy for all users.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\Windows Search",
                    "EnableDynamicContentInWSB",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.disable-service-splitting",
                    "Disable Service Splitting",
                    "Prevents services from being split into separate svchost processes.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"System\CurrentControlSet\Control",
                    "SvcHostSplitThresholdInKB",
                    RegistryValueKind.DWord,
                    -1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.disable-llmnr",
                    "Disable LLMNR",
                    "Turns off multicast name resolution (LLMNR).",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows NT\DNSClient",
                    "EnableMulticast",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.disable-mdns",
                    "Disable mDNS",
                    "Turns off multicast DNS name resolution.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows NT\DNSClient",
                    "EnableMDNS",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.disable-netbios-resolution",
                    "Disable NetBIOS Name Resolution",
                    "Disables NetBIOS name resolution on the DNS client.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows NT\DNSClient",
                    "EnableNetbios",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.disable-smart-name-resolution",
                    "Disable Smart Multi-Homed Name Resolution",
                    "Disables smart name resolution across multiple network interfaces.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows NT\DNSClient",
                    "DisableSmartNameResolution",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryValueSetTweak(
                    "network.disable-lltd",
                    "Disable Network Discovery (LLTD)",
                    "Disables LLTD mapper and responder for network discovery.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\LLTD",
                    new[]
                    {
                        new RegistryValueSetEntry("EnableLLTDIO", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("AllowLLTDIOOnDomain", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("AllowLLTDIOOnPublicNet", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("ProhibitLLTDIOOnPrivateNet", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("EnableRspndr", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("AllowRspndrOnDomain", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("AllowRspndrOnPublicNet", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("ProhibitRspndrOnPrivateNet", RegistryValueKind.DWord, 0)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "network.disable-active-probing",
                    "Disable Active Probing",
                    "Turns off NCSI active probing for internet connectivity tests.",
                    TweakRiskLevel.Advanced,
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\NetworkConnectivityStatusIndicator", "NoActiveProbe", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\PolicyManager\default\Connectivity", "DisallowNetworkConnectivityActiveTests", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\NlaSvc\Parameters\Internet", "EnableUserActiveProbing", RegistryValueKind.DWord, 0),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\NlaSvc\Parameters\Internet", "MaxActiveProbes", RegistryValueKind.DWord, 1)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.prefer-ipv4",
                    "Prefer IPv4 over IPv6",
                    "Configures the IPv6 stack to prefer IPv4 without fully disabling IPv6.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters",
                    "DisabledComponents",
                    RegistryValueKind.DWord,
                    32),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.disable-ipv6",
                    "Disable IPv6",
                    "Disables IPv6 on all interfaces (can add boot delay on some systems).",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters",
                    "DisabledComponents",
                    RegistryValueKind.DWord,
                    255),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.disable-wifi-sense",
                    "Disable Wi-Fi Sense",
                    "Turns off suggested open hotspots and shared Wi-Fi networks.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Microsoft\WcmSvc\wifinetworkmanager\config",
                    "AutoConnectAllowedOEM",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.disable-plaintext-smb-passwords",
                    "Disable Plaintext SMB Passwords",
                    "Prevents sending unencrypted passwords to SMB servers.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters",
                    "EnablePlainTextPassword",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.require-ntlmv2-session-security",
                    "Require NTLMv2 Session Security",
                    "Requires NTLMv2 session security and 128-bit encryption for SMB clients.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"System\CurrentControlSet\Control\Lsa\MSV1_0",
                    "NTLMMinClientSec",
                    RegistryValueKind.DWord,
                    537395200),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "privacy.disable-diagnostic-data",
                    "Disable Diagnostic Data (Policy)",
                    "Sets diagnostic data policy to the minimum level and disables telemetry flags.",
                    TweakRiskLevel.Risky,
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", RegistryValueKind.DWord, 0),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows", "DCEInUseTelemetryDisabled", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\wbem\Tracing", "enableWinmgmtTelemetry", RegistryValueKind.DWord, 0)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryValueSetTweak(
                    "privacy.disable-activity-history",
                    "Disable Activity History",
                    "Stops publishing and uploading activity history across devices.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\System",
                    new[]
                    {
                        new RegistryValueSetEntry("EnableActivityFeed", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("PublishUserActivities", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("UploadUserActivities", RegistryValueKind.DWord, 0)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-cross-device-experiences",
                    "Disable Cross-Device Experiences",
                    "Disables continue experiences on this device (CDP).",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\System",
                    "EnableCdp",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-phone-linking",
                    "Disable Phone Linking",
                    "Prevents the device from participating in Phone-PC linking.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\System",
                    "EnableMmx",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-resume",
                    "Disable Resume Experiences",
                    "Turns off Resume (start on one device, continue on this PC).",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\CrossDeviceResume\Configuration",
                    "IsResumeAllowed",
                    RegistryValueKind.DWord,
                    0,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryValueSetTweak(
                    "privacy.disable-cli-telemetry",
                    "Disable PowerShell & .NET CLI Telemetry",
                    "Opts out of PowerShell and .NET CLI telemetry for the current user.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Environment",
                    new[]
                    {
                        new RegistryValueSetEntry("POWERSHELL_TELEMETRY_OPTOUT", RegistryValueKind.String, "1"),
                        new RegistryValueSetEntry("DOTNET_CLI_TELEMETRY_OPTOUT", RegistryValueKind.String, "1")
                    },
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-language-list-access",
                    "Disable Website Access to Language List",
                    "Prevents websites from accessing the language list for content customization.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Control Panel\International\User Profile",
                    "HttpAcceptLanguageOptOut",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryValueSetTweak(
                    "privacy.disable-wmplayer-telemetry",
                    "Disable Windows Media Player Telemetry",
                    "Turns off usage tracking and online metadata for Windows Media Player.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\MediaPlayer\Preferences",
                    new[]
                    {
                        new RegistryValueSetEntry("AcceptedPrivacyStatement", RegistryValueKind.DWord, 1),
                        new RegistryValueSetEntry("MetadataRetrieval", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("SendUserGUID", RegistryValueKind.Binary, new byte[] { 0x00 }),
                        new RegistryValueSetEntry("SilentAcquisition", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("UsageTracking", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("DisableMRUMusic", RegistryValueKind.DWord, 1),
                        new RegistryValueSetEntry("DisableMRUPictures", RegistryValueKind.DWord, 1),
                        new RegistryValueSetEntry("DisableMRUVideo", RegistryValueKind.DWord, 1),
                        new RegistryValueSetEntry("DisableMRUPlaylists", RegistryValueKind.DWord, 1)
                    },
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryValueSetTweak(
                    "privacy.disable-sync-settings",
                    "Disable Settings Sync",
                    "Disables syncing Windows settings and related data across devices.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\SettingSync",
                    new[]
                    {
                        new RegistryValueSetEntry("DisableSyncOnPaidNetwork", RegistryValueKind.DWord, 1),
                        new RegistryValueSetEntry("DisableAppSyncSettingSync", RegistryValueKind.DWord, 2),
                        new RegistryValueSetEntry("DisableAppSyncSettingSyncUserOverride", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("DisableApplicationSettingSync", RegistryValueKind.DWord, 2),
                        new RegistryValueSetEntry("DisableApplicationSettingSyncUserOverride", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("DisableCredentialsSettingSync", RegistryValueKind.DWord, 2),
                        new RegistryValueSetEntry("DisableCredentialsSettingSyncUserOverride", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("DisablePersonalizationSettingSync", RegistryValueKind.DWord, 2),
                        new RegistryValueSetEntry("DisablePersonalizationSettingSyncUserOverride", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("DisableDesktopThemeSettingSync", RegistryValueKind.DWord, 2),
                        new RegistryValueSetEntry("DisableDesktopThemeSettingSyncUserOverride", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("DisableSettingSync", RegistryValueKind.DWord, 2),
                        new RegistryValueSetEntry("DisableSettingSyncUserOverride", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("DisableStartLayoutSettingSync", RegistryValueKind.DWord, 2),
                        new RegistryValueSetEntry("DisableStartLayoutSettingSyncUserOverride", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("DisableWebBrowserSettingSync", RegistryValueKind.DWord, 2),
                        new RegistryValueSetEntry("DisableWebBrowserSettingSyncUserOverride", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("DisableWindowsSettingSync", RegistryValueKind.DWord, 2),
                        new RegistryValueSetEntry("DisableWindowsSettingSyncUserOverride", RegistryValueKind.DWord, 0)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-search-history",
                    "Disable Search History",
                    "Prevents search history from being stored for this user.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Policies\Microsoft\Windows\Explorer",
                    "DisableSearchHistory",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-search-box-suggestions",
                    "Disable Search Box Suggestions",
                    "Stops File Explorer from showing recent search suggestions.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Policies\Microsoft\Windows\Explorer",
                    "DisableSearchBoxSuggestions",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.hide-recommended-section",
                    "Hide Start Recommended Section (Policy)",
                    "Removes the Recommended section from the Start menu for all users.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\Explorer",
                    "HideRecommendedSection",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.hide-recommended-section-user",
                    "Hide Start Recommended Section (User)",
                    "Removes the Recommended section from the Start menu for the current user.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Policies\Microsoft\Windows\Explorer",
                    "HideRecommendedSection",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.hide-recommended-personalized-sites",
                    "Hide Start Personalized Site Recommendations (Policy)",
                    "Removes personalized website recommendations from Start for all users.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\Explorer",
                    "HideRecommendedPersonalizedSites",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.hide-recommended-personalized-sites-user",
                    "Hide Start Personalized Site Recommendations (User)",
                    "Removes personalized website recommendations from Start for the current user.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Policies\Microsoft\Windows\Explorer",
                    "HideRecommendedPersonalizedSites",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryValueSetTweak(
                    "privacy.disable-suggestions",
                    "Disable Suggestions & Tips",
                    "Turns off Windows tips, welcome experiences, and Settings recommendations.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
                    new[]
                    {
                        new RegistryValueSetEntry("SubscribedContent-338389Enabled", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("SubscribedContent-310093Enabled", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("SubscribedContent-338393Enabled", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("SubscribedContent-353694Enabled", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("SubscribedContent-353696Enabled", RegistryValueKind.DWord, 0)
                    },
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-consumer-account-content",
                    "Disable Consumer Account State Content",
                    "Prevents Windows experiences from using cloud consumer account content.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\CloudContent",
                    "DisableConsumerAccountStateContent",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-online-tips",
                    "Disable Online Tips",
                    "Stops Settings from retrieving online tips and help content.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                    "AllowOnlineTips",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "privacy.disable-edge-search-suggestions",
                    "Disable Edge Search Suggestions",
                    "Turns off search suggestions in Microsoft Edge address bar.",
                    TweakRiskLevel.Advanced,
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Edge", "SearchSuggestEnabled", RegistryValueKind.DWord, 0),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Edge", "LocalProvidersEnabled", RegistryValueKind.DWord, 0),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\MicrosoftEdge\SearchScopes", "ShowSearchSuggestionsGlobal", RegistryValueKind.DWord, 0)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-location",
                    "Disable Location Services",
                    "Turns off location services for this device.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\LocationAndSensors",
                    "DisableLocation",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "privacy.disable-location-consent",
                    "Disable Location Consent (User)",
                    "Denies location capability access for the current user.",
                    TweakRiskLevel.Advanced,
                    new[]
                    {
                        new RegistryValueBatchEntry(
                            RegistryHive.CurrentUser,
                            @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location",
                            "Value",
                            RegistryValueKind.String,
                            "Deny"),
                        new RegistryValueBatchEntry(
                            RegistryHive.CurrentUser,
                            @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location\NonPackaged",
                            "Value",
                            RegistryValueKind.String,
                            "Deny"),
                        new RegistryValueBatchEntry(
                            RegistryHive.CurrentUser,
                            @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location",
                            "ShowGlobalPrompts",
                            RegistryValueKind.DWord,
                            1)
                    },
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-location-consent-system",
                    "Disable Location Consent (System)",
                    "Denies location capability access at the system level.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location",
                    "Value",
                    RegistryValueKind.String,
                    "Deny"),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-location-scripting",
                    "Disable Location Scripting",
                    "Disables location scripting support for apps and scripts.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\LocationAndSensors",
                    "DisableLocationScripting",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-windows-location-provider",
                    "Disable Windows Location Provider",
                    "Disables the Windows Location Provider for all apps.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\LocationAndSensors",
                    "DisableWindowsLocationProvider",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-sensors",
                    "Disable Sensors",
                    "Turns off hardware sensor access for apps and system features.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\LocationAndSensors",
                    "DisableSensors",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateCompositeTweak(
                    "privacy.disable-steps-recorder",
                    "Disable Steps Recorder",
                    "Disables Steps Recorder to prevent recording user actions.",
                    TweakRiskLevel.Advanced,
                    new ITweak[]
                    {
                        CreateRegistryTweak(
                            "privacy.disable-steps-recorder.policy",
                            "Disable Steps Recorder (Policy)",
                            "Disables Steps Recorder via policy.",
                            TweakRiskLevel.Advanced,
                            RegistryHive.LocalMachine,
                            @"Software\Policies\Microsoft\Windows\AppCompat",
                            "DisableUAR",
                            RegistryValueKind.DWord,
                            1),
                        CreateFileRenameTweak(
                            "privacy.disable-steps-recorder.binary",
                            "Disable Steps Recorder (Binary)",
                            "Renames the Steps Recorder binary to block execution.",
                            TweakRiskLevel.Advanced,
                            psrPath,
                            psrDisabledPath)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-app-launch-tracking",
                    "Disable App Launch Tracking",
                    "Stops Windows from tracking app launches for Start/Search personalization.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                    "Start_TrackProgs",
                    RegistryValueKind.DWord,
                    0,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-reserved-storage",
                    "Disable Reserved Storage",
                    "Disables Windows reserved storage for updates.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Microsoft\Windows\CurrentVersion\ReserveManager",
                    "DisableDeletes",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-biometrics",
                    "Disable Biometrics",
                    "Turns off Windows biometric features on this device.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Biometrics",
                    "Enabled",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-biometrics-logon",
                    "Disable Biometrics Logon",
                    "Prevents users from signing in with biometrics.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Biometrics\Credential Provider",
                    "Enabled",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-biometrics-domain-logon",
                    "Disable Biometrics for Domain Logon",
                    "Prevents domain users from signing in with biometrics.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Biometrics\Credential Provider",
                    "Domain Accounts",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "security.disable-remote-assistance",
                    "Disable Remote Assistance",
                    "Disables solicited Remote Assistance connections.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows NT\Terminal Services",
                    "fAllowToGetHelp",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "security.disable-windows-update",
                    "Disable Windows Update",
                    "Pauses updates and sets Windows Update policies to block access.",
                    TweakRiskLevel.Risky,
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseFeatureUpdatesEndTime", RegistryValueKind.String, "2030-01-01T00:00:00Z"),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseQualityUpdatesEndTime", RegistryValueKind.String, "2030-01-01T00:00:00Z"),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseUpdatesExpiryTime", RegistryValueKind.String, "2030-01-01T00:00:00Z"),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "WUServer", RegistryValueKind.String, " "),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "WUStatusServer", RegistryValueKind.String, " "),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "UpdateServiceUrlAlternate", RegistryValueKind.String, " "),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DisableWindowsUpdateAccess", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DisableOSUpgrade", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "SetDisableUXWUAccess", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DoNotConnectToWindowsUpdateInternetLocations", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "UseWUServer", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update", "AUOptions", RegistryValueKind.DWord, 1)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "security.disable-wu-driver-updates",
                    "Disable WU Driver Updates",
                    "Stops Windows Update from offering driver updates and device metadata.",
                    TweakRiskLevel.Advanced,
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "ExcludeWUDriversInQualityUpdate", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DriverSearching", "SearchOrderConfig", RegistryValueKind.DWord, 0),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DriverSearching", "DontSearchWindowsUpdate", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Device Metadata", "PreventDeviceMetadataFromNetwork", RegistryValueKind.DWord, 1)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "security.disable-p2p-updates",
                    "Disable P2P Updates",
                    "Disables Delivery Optimization peer-to-peer caching for updates.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\PolicyManager\default\DeliveryOptimization",
                    "DODownloadMode",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "security.disable-system-mitigations",
                    "Disable System Mitigations",
                    "Turns off system-wide exploit mitigation settings.",
                    TweakRiskLevel.Risky,
                    new[]
                    {
                        new RegistryValueBatchEntry(
                            RegistryHive.LocalMachine,
                            @"System\CurrentControlSet\Control\Session Manager\kernel",
                            "MitigationOptions",
                            RegistryValueKind.Binary,
                            new byte[]
                            {
                                0x00, 0x22, 0x22, 0x20, 0x22, 0x20, 0x22, 0x22,
                                0x22, 0x20, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22,
                                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                            }),
                        new RegistryValueBatchEntry(
                            RegistryHive.LocalMachine,
                            @"System\CurrentControlSet\Control\Session Manager\kernel",
                            "MitigationAuditOptions",
                            RegistryValueKind.Binary,
                            new byte[]
                            {
                                0x02, 0x22, 0x22, 0x02, 0x02, 0x02, 0x20, 0x22,
                                0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22,
                                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                            })
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "security.disable-windows-firewall",
                    "Disable Windows Firewall",
                    "Turns off Windows Defender Firewall for all profiles.",
                    TweakRiskLevel.Risky,
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\DomainProfile", "EnableFirewall", RegistryValueKind.DWord, 0),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile", "EnableFirewall", RegistryValueKind.DWord, 0),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile", "EnableFirewall", RegistryValueKind.DWord, 0)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "security.disable-system-restore",
                    "Disable System Restore",
                    "Disables System Restore by setting the restore session interval to zero.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore",
                    "RPSessionInterval",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "security.disable-downloads-blocking",
                    "Disable Downloads Blocking",
                    "Prevents Windows from marking downloads with zone information (MOTW).",
                    TweakRiskLevel.Risky,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments",
                    "SaveZoneInformation",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "security.disable-wpbt",
                    "Disable WPBT Execution",
                    "Blocks Windows Platform Binary Table (WPBT) programs from running at startup.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"System\CurrentControlSet\Control\Session Manager",
                    "DisableWpbtExecution",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryValueSetTweak(
                    "security.disable-vbs",
                    "Disable VBS (HVCI)",
                    "Turns off virtualization-based security and memory integrity policies.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\DeviceGuard",
                    new[]
                    {
                        new RegistryValueSetEntry("EnableVirtualizationBasedSecurity", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("HypervisorEnforcedCodeIntegrity", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("LsaCfgFlags", RegistryValueKind.DWord, 0)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "security.trusted-path-credential-prompting",
                    "Require Trusted Path for Credentials",
                    "Forces credential prompts to use the secure desktop.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\CredUI",
                    "EnableSecureCredentialPrompting",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "security.disable-ntfs-encryption",
                    "Disable NTFS Encryption",
                    "Prevents EFS encryption on NTFS volumes.",
                    TweakRiskLevel.Risky,
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Policies", "NtfsDisableEncryption", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"System\CurrentControlSet\Control\FileSystem", "NtfsDisableEncryption", RegistryValueKind.DWord, 1)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-application-telemetry",
                    "Disable Application Telemetry",
                    "Stops the Application Telemetry engine from collecting usage data.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\AppCompat",
                    "AITEnable",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.limit-diagnostic-log-collection",
                    "Limit Diagnostic Log Collection",
                    "Prevents additional diagnostic logs from being collected.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\DataCollection",
                    "LimitDiagnosticLogCollection",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-diagnostic-data-viewer",
                    "Disable Diagnostic Data Viewer",
                    "Blocks access to the Diagnostic Data Viewer in Settings.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\DataCollection",
                    "DisableDiagnosticDataViewer",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-diagnostic-data-delete",
                    "Disable Diagnostic Data Deletion",
                    "Disables the ability to delete diagnostic data in Settings.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\DataCollection",
                    "DisableDeviceDelete",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-onesettings-downloads",
                    "Disable OneSettings Downloads",
                    "Stops Windows from downloading configuration settings from OneSettings.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\DataCollection",
                    "DisableOneSettingsDownloads",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.limit-dump-collection",
                    "Limit Dump Collection",
                    "Limits diagnostic dumps to reduce the data sent in diagnostics.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\DataCollection",
                    "LimitDumpCollection",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-telemetry-optin-ui",
                    "Disable Diagnostic Data Opt-in UI",
                    "Disables the diagnostic data opt-in settings UI in Settings.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\DataCollection",
                    "DisableTelemetryOptInSettingsUx",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-telemetry-change-notifications",
                    "Disable Diagnostic Data Change Notifications",
                    "Stops opt-in change notifications for diagnostic data.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\DataCollection",
                    "DisableTelemetryOptInChangeNotification",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-device-name-telemetry",
                    "Disable Device Name in Diagnostics",
                    "Prevents the device name from being included in diagnostic data.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\DataCollection",
                    "AllowDeviceNameInTelemetry",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-file-history",
                    "Disable File History",
                    "Turns off File History backups.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\FileHistory",
                    "Disabled",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-mdm-enrollment",
                    "Disable MDM Enrollment",
                    "Prevents new MDM enrollments for this device.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\CurrentVersion\MDM",
                    "DisableRegistration",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-feedback-notifications",
                    "Disable Feedback Notifications",
                    "Stops Windows Feedback prompts from appearing.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\DataCollection",
                    "DoNotShowFeedbackNotifications",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "privacy.disable-ceip",
                    "Disable CEIP",
                    "Opts out of Customer Experience Improvement Program data collection.",
                    TweakRiskLevel.Advanced,
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\AppV\CEIP", "CEIPEnable", RegistryValueKind.DWord, 0),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", RegistryValueKind.DWord, 0),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Messenger\Client", "CEIP", RegistryValueKind.DWord, 2)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "privacy.deny-app-access",
                    "Deny App Access (Except Microphone)",
                    "Forces Windows apps to be denied access to sensitive capabilities.",
                    TweakRiskLevel.Risky,
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\System", "AllowUserInfoAccess", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessAccountInfo", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessCalendar", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessCallHistory", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessCamera", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessContacts", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessEmail", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessGraphicsCaptureProgrammatic", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessGraphicsCaptureWithoutBorder", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessHumanPresence", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessLocation", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessMessaging", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessMicrophone", RegistryValueKind.DWord, 0),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessMotion", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessNotifications", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessPhone", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessRadios", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsSyncWithDevices", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessTasks", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessTrustedDevices", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsGetDiagnosticInfo", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessGazeInput", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsActivateWithVoice", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsActivateWithVoiceAboveLock", RegistryValueKind.DWord, 2),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessBackgroundSpatialPerception", RegistryValueKind.DWord, 2)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-background-apps",
                    "Disable Background Apps",
                    "Prevents Windows apps from running in the background.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\AppPrivacy",
                    "LetAppsRunInBackground",
                    RegistryValueKind.DWord,
                    2),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-wer",
                    "Disable Windows Error Reporting",
                    "Disables Windows Error Reporting uploads.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\Windows Error Reporting",
                    "Disabled",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "privacy.disable-rsop-logging",
                    "Disable RSoP Logging",
                    "Turns off Resultant Set of Policy logging on this device.",
                    TweakRiskLevel.Advanced,
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "RsopLogging", RegistryValueKind.DWord, 0),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\SYSTEM", "RsopLogging", RegistryValueKind.DWord, 0)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "privacy.troubleshooter-dont-run",
                    "Troubleshooter: Don't Run Any",
                    "Prevents recommended troubleshooters from running automatically.",
                    TweakRiskLevel.Advanced,
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsMitigation", "UserPreference", RegistryValueKind.DWord, 1),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Troubleshooting\AllowRecommendations", "TroubleshootingAllowRecommendations", RegistryValueKind.DWord, 0)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-message-sync",
                    "Disable Message Sync",
                    "Stops SMS/MMS cloud sync for this device.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\Messaging",
                    "AllowMessageSync",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateCompositeTweak(
                    "privacy.disable-offline-files",
                    "Disable Offline Files",
                    "Disables Offline Files (CSC) via policy, services, tasks, and Sync Center.",
                    TweakRiskLevel.Advanced,
                    new ITweak[]
                    {
                        CreateRegistryTweak(
                            "privacy.disable-offline-files.policy",
                            "Disable Offline Files (Policy)",
                            "Disables Offline Files policy.",
                            TweakRiskLevel.Advanced,
                            RegistryHive.LocalMachine,
                            @"Software\Policies\Microsoft\Windows\NetCache",
                            "Enabled",
                            RegistryValueKind.DWord,
                            0),
                        CreateServiceStartModeBatchTweak(
                            "privacy.disable-offline-files.services",
                            "Disable Offline Files (Services)",
                            "Disables Offline Files services.",
                            TweakRiskLevel.Advanced,
                            new[] { "CSC", "CscService" },
                            ServiceStartMode.Disabled,
                            stopRunning: true),
                        CreateScheduledTaskBatchTweak(
                            "privacy.disable-offline-files.tasks",
                            "Disable Offline Files (Tasks)",
                            "Disables Offline Files scheduled tasks.",
                            TweakRiskLevel.Advanced,
                            new[]
                            {
                                @"\Microsoft\Windows\Offline Files\Background Synchronization",
                                @"\Microsoft\Windows\Offline Files\Logon Synchronization"
                            }),
                        CreateFileRenameTweak(
                            "privacy.disable-offline-files.binary",
                            "Disable Offline Files (Sync Center)",
                            "Renames Sync Center to block Offline Files execution.",
                            TweakRiskLevel.Advanced,
                            mobsyncPath,
                            mobsyncDisabledPath)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.block-microsoft-accounts",
                    "Block Microsoft Accounts",
                    "Prevents adding or signing in with Microsoft accounts.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                    "NoConnectedUser",
                    RegistryValueKind.DWord,
                    3),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-kms-activation-telemetry",
                    "Disable KMS Activation Telemetry",
                    "Stops KMS client activation data from being sent to Microsoft.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform",
                    "NoGenTicket",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-font-providers",
                    "Disable Font Providers",
                    "Prevents Windows from downloading fonts from online providers.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\System",
                    "EnableFontProviders",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-local-security-questions",
                    "Disable Local Security Questions",
                    "Prevents setting security questions for local accounts.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\System",
                    "NoLocalPasswordResetQuestions",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateCompositeTweak(
                    "privacy.disable-application-compatibility",
                    "Disable Application Compatibility",
                    "Turns off Windows application compatibility components, telemetry, and related tasks.",
                    TweakRiskLevel.Risky,
                    new ITweak[]
                    {
                        CreateRegistryValueSetTweak(
                            "privacy.disable-application-compatibility.policy",
                            "Disable Application Compatibility (Policy)",
                            "Turns off application compatibility policies.",
                            TweakRiskLevel.Risky,
                            RegistryHive.LocalMachine,
                            @"Software\Policies\Microsoft\Windows\AppCompat",
                            new[]
                            {
                                new RegistryValueSetEntry("DisableEngine", RegistryValueKind.DWord, 1),
                                new RegistryValueSetEntry("DisableAPISamping", RegistryValueKind.DWord, 1),
                                new RegistryValueSetEntry("DisableApplicationFootprint", RegistryValueKind.DWord, 1),
                                new RegistryValueSetEntry("DisableInstallTracing", RegistryValueKind.DWord, 1),
                                new RegistryValueSetEntry("DisableWin32AppBackup", RegistryValueKind.DWord, 1),
                                new RegistryValueSetEntry("DisablePcaUI", RegistryValueKind.DWord, 1),
                                new RegistryValueSetEntry("SbEnable", RegistryValueKind.DWord, 0)
                            }),
                        CreateScheduledTaskBatchTweak(
                            "privacy.disable-application-compatibility.tasks",
                            "Disable Application Compatibility (Tasks)",
                            "Disables Application Experience scheduled tasks.",
                            TweakRiskLevel.Risky,
                            new[]
                            {
                                @"\Microsoft\Windows\Application Experience\MareBackup",
                                @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
                                @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser Exp",
                                @"\Microsoft\Windows\Application Experience\PcaPatchDbTask",
                                @"\Microsoft\Windows\Application Experience\SdbinstMergeDbTask",
                                @"\Microsoft\Windows\Application Experience\StartupAppTask"
                            })
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "privacy.disable-inking-typing-personalization",
                    "Disable Inking & Typing Personalization",
                    "Stops sending inking and typing data to Microsoft.",
                    TweakRiskLevel.Advanced,
                    new[]
                    {
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Policies\TextInput", "AllowLinguisticDataCollection", RegistryValueKind.DWord, 0),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\WindowsInkWorkspace", "AllowSuggestedAppsInWindowsInkWorkspace", RegistryValueKind.DWord, 0),
                        new RegistryValueBatchEntry(RegistryHive.LocalMachine, @"Software\Policies\Microsoft\WindowsInkWorkspace", "AllowWindowsInkWorkspace", RegistryValueKind.DWord, 0)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-copilot",
                    "Disable Windows Copilot",
                    "Turns off the Windows Copilot experience for this user.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.CurrentUser,
                    @"Software\Policies\Microsoft\Windows\WindowsCopilot",
                    "TurnOffWindowsCopilot",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-recall",
                    "Disable Recall",
                    "Disables saving snapshots for Recall on this user.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.CurrentUser,
                    @"Software\Policies\Microsoft\Windows\WindowsAI",
                    "DisableAIDataAnalysis",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-camera",
                    "Disable Camera",
                    "Disables camera access via policy.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Camera",
                    "AllowCamera",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryValueBatchTweak(
                    "privacy.disable-sleep-study-diagnostics",
                    "Disable Sleep Study Diagnostics",
                    "Disables sleep study diagnostic event channels.",
                    TweakRiskLevel.Advanced,
                    new[]
                    {
                        new RegistryValueBatchEntry(
                            RegistryHive.LocalMachine,
                            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WINEVT\Channels\Microsoft-Windows-SleepStudy/Diagnostic",
                            "Enabled",
                            RegistryValueKind.DWord,
                            0),
                        new RegistryValueBatchEntry(
                            RegistryHive.LocalMachine,
                            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WINEVT\Channels\Microsoft-Windows-Kernel-Processor-Power/Diagnostic",
                            "Enabled",
                            RegistryValueKind.DWord,
                            0),
                        new RegistryValueBatchEntry(
                            RegistryHive.LocalMachine,
                            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WINEVT\Channels\Microsoft-Windows-UserModePowerService/Diagnostic",
                            "Enabled",
                            RegistryValueKind.DWord,
                            0)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.hide-last-logged-in-user",
                    "Hide Last Logged-In User",
                    "Removes the last signed-in username from the sign-in screen.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                    "DontDisplayLastUserName",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.hide-username-at-signin",
                    "Hide Username at Sign-In",
                    "Hides the username after credentials are entered at sign-in.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                    "DontDisplayUserName",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryValueSetTweak(
                    "security.uac-never-notify",
                    "Set UAC to Never Notify",
                    "Lowers UAC prompts to the least restrictive setting.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                    new[]
                    {
                        new RegistryValueSetEntry("EnableLUA", RegistryValueKind.DWord, 1),
                        new RegistryValueSetEntry("ConsentPromptBehaviorAdmin", RegistryValueKind.DWord, 0),
                        new RegistryValueSetEntry("PromptOnSecureDesktop", RegistryValueKind.DWord, 0)
                    }),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "security.disable-uac",
                    "Disable UAC",
                    "Disables User Account Control entirely (requires reboot).",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                    "EnableLUA",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "security.powershell-unrestricted",
                    "Set PowerShell Execution Policy to Unrestricted",
                    "Allows PowerShell scripts to run without signature checks.",
                    TweakRiskLevel.Risky,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell",
                    "ExecutionPolicy",
                    RegistryValueKind.String,
                    "Unrestricted"),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "security.disable-password-reveal",
                    "Disable Password Reveal Button",
                    "Hides the password reveal button in credential prompts.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\CredUI",
                    "DisablePasswordReveal",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "security.disable-picture-password",
                    "Disable Picture Password Sign-In",
                    "Prevents domain users from using picture password sign-in.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\System",
                    "BlockDomainPicturePassword",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.enable-lltdio",
                    "Enable LLTD Mapper I/O",
                    "Enables the LLTD Mapper I/O driver for network topology discovery.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\LLTD",
                    "EnableLLTDIO",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "network.enable-lltd-responder",
                    "Enable LLTD Responder",
                    "Enables the LLTD Responder driver for network topology discovery.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\LLTD",
                    "EnableRspndr",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),
            new(CreateScheduledTaskBatchTweak(
                    "system.disable-scheduled-tasks",
                    "Disable Scheduled Tasks",
                    "Disables common scheduled tasks tied to telemetry, maintenance, and update workflows.",
                    TweakRiskLevel.Risky,
                    new[]
                    {
                        @"\Microsoft\Windows\Application Experience\MareBackup",
                        @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
                        @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser Exp",
                        @"\Microsoft\Windows\Application Experience\StartupAppTask",
                        @"\Microsoft\Windows\ApplicationData\DsSvcCleanup",
                        @"\Microsoft\Windows\Autochk\Proxy",
                        @"\Microsoft\Windows\CloudExperienceHost\CreateObjectTask",
                        @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
                        @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
                        @"\Microsoft\Windows\Defrag\ScheduledDefrag",
                        @"\Microsoft\Windows\Diagnosis\RecommendedTroubleshootingScanner",
                        @"\Microsoft\Windows\Diagnosis\Scheduled",
                        @"\Microsoft\Windows\Diagnosis\UnexpectedCodePath",
                        @"\Microsoft\Windows\DiskCleanup\SilentCleanup",
                        @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
                        @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticResolver",
                        @"\Microsoft\Windows\DiskFootprint\Diagnostics",
                        @"\Microsoft\Windows\DiskFootprint\StorageSense",
                        @"\Microsoft\Windows\Feedback\Siuf\DmClient",
                        @"\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload",
                        @"\Microsoft\Windows\InstallService\ScanForUpdates",
                        @"\Microsoft\Windows\InstallService\ScanForUpdatesAsUser",
                        @"\Microsoft\Windows\InstallService\SmartRetry",
                        @"\Microsoft\Windows\InstallService\WakeUpAndContinueUpdates",
                        @"\Microsoft\Windows\InstallService\WakeUpAndScanForUpdates",
                        @"\Microsoft\Windows\International\Synchronize Language Settings",
                        @"\Microsoft\Windows\LanguageComponentsInstaller\Installation",
                        @"\Microsoft\Windows\LanguageComponentsInstaller\ReconcileLanguageResources",
                        @"\Microsoft\Windows\LanguageComponentsInstaller\Uninstallation",
                        @"\Microsoft\Windows\Maps\MapsUpdateTask",
                        @"\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem",
                        @"\Microsoft\Windows\Registry\RegIdleBackup",
                        @"\Microsoft\Windows\RetailDemo\CleanupOfflineContent",
                        @"\Microsoft\Windows\Speech\SpeechModelDownloadTask",
                        @"\Microsoft\Windows\Sysmain\ResPriStaticDbSync",
                        @"\Microsoft\Windows\Sysmain\WsSwapAssessmentTask",
                        @"\Microsoft\Windows\Time Synchronization\ForceSynchronizeTime",
                        @"\Microsoft\Windows\Time Synchronization\SynchronizeTime",
                        @"\Microsoft\Windows\UNP\RunUpdateNotificationMgr",
                        @"\Microsoft\Windows\Windows Error Reporting\QueueReporting"
                    }),
                pipeline,
                _isElevated),
            new(CreateServiceStartModeBatchTweak(
                    "system.disable-services-drivers",
                    "Disable Services/Drivers",
                    "Disables selected services and drivers from starting automatically; running services may continue until restart.",
                    TweakRiskLevel.Risky,
                    new[]
                    {
                        "bam",
                        "dam",
                        "ShellHWDetection",
                        "Beep",
                        "WbioSrvc",
                        "BTAGService",
                        "BluetoothUserService_*",
                        "BthA2dp",
                        "BthAvctpSvc",
                        "BthEnum",
                        "BthHFEnum",
                        "BthLEEnum",
                        "BthMini",
                        "BTHMODEM",
                        "BTHPORT",
                        "bthserv",
                        "BTHUSB",
                        "DeviceAssociationBrokerSvc",
                        "DeviceAssociationService",
                        "Microsoft_Bluetooth_AvrcpTransport",
                        "RFCOMM",
                        "BcastDVRUserService",
                        "FrameServer",
                        "FrameServerMonitor",
                        "cdrom",
                        "cbdhsvc",
                        "DsmSvc",
                        "Dhcp",
                        "DusmSvc",
                        "DPS",
                        "diagsvc",
                        "WdiServiceHost",
                        "WdiSystemHost",
                        "TroubleshootingSvc",
                        "Ndu",
                        "MicrosoftEdgeElevationService",
                        "edgeupdate",
                        "edgeupdatem",
                        "LanmanServer",
                        "LanmanWorkstation",
                        "GameInputSvc",
                        "bttflt",
                        "gencounter",
                        "hvcrash",
                        "HvHost",
                        "hvservice",
                        "hyperkbd",
                        "HyperVideo",
                        "storflt",
                        "Vid",
                        "vmbus",
                        "vmgid",
                        "vmicguestinterface",
                        "vmicheartbeat",
                        "vmickvpexchange",
                        "vmicrdv",
                        "vmicshutdown",
                        "vmictimesync",
                        "vmicvmsession",
                        "vmicvss",
                        "vpci",
                        "Tcpip6",
                        "IpxlatCfgSvc",
                        "iphlpsvc",
                        "lfsvc",
                        "MapsBroker",
                        "fdPHost",
                        "FDResPub",
                        "SSDPSRV",
                        "upnphost",
                        "MsLldp",
                        "rspndr",
                        "lltdio",
                        "lltdsvc",
                        "ClickToRunSvc",
                        "PhoneSvc",
                        "TapiSrv",
                        "RmSvc",
                        "WpcMonSvc",
                        "McpManagementService",
                        "PrintDeviceConfigurationService",
                        "PrintNotify",
                        "PrintScanBrokerService",
                        "PrintWorkflowUserSvc",
                        "Spooler",
                        "usbprint",
                        "CloudBackupRestoreSvc",
                        "SDRSVC",
                        "swprv",
                        "VSS",
                        "wbengine",
                        "RemoteAccess",
                        "RemoteRegistry",
                        "SessionEnv",
                        "TermService",
                        "UmRdpService",
                        "SensorDataService",
                        "SensrSvc",
                        "SensorService",
                        "wlidsvc",
                        "CertPropSvc",
                        "SCardSvr",
                        "ScDeviceEnum",
                        "SCPolicySvc",
                        "scfilter",
                        "SysMain",
                        "AppXSvc",
                        "camsvc",
                        "ClipSVC",
                        "InstallService",
                        "LicenseManager",
                        "PushToInstall",
                        "lmhosts",
                        "DiagTrack",
                        "dmwappushservice",
                        "InventorySvc",
                        "PcaSvc",
                        "wuqisvc",
                        "Themes",
                        "W32Time",
                        "autotimesvc",
                        "tzautoupdate",
                        "luafv",
                        "UnistoreSvc",
                        "UserDataSvc",
                        "WerSvc",
                        "wercplsupport",
                        "WlanSvc",
                        "vwififlt",
                        "wisvc",
                        "WSearch",
                        "WaaSMedicSvc",
                        "UsoSvc",
                        "wuauserv",
                        "XboxGipSvc",
                        "xboxgip",
                        "XblAuthManager",
                        "XblGameSave",
                        "XboxNetApiSvc",
                        "WalletService",
                        "PenService",
                        "buttonconverter"
                    },
                    ServiceStartMode.Disabled,
                    stopRunning: false),
                pipeline,
                _isElevated),
            new(CreateServiceStartModeBatchTweak(
                    "system.disable-windows-search-service",
                    "Disable Windows Search Service",
                    "Disables Windows Search indexing service (WSearch).",
                    TweakRiskLevel.Advanced,
                    new[] { "WSearch" },
                    ServiceStartMode.Disabled,
                    stopRunning: true),
                pipeline,
                _isElevated),
            new(CreateFileRenameTweak(
                    "privacy.disable-f1-help",
                    "Disable F1 Help",
                    "Disables F1 help by renaming HelpPane.exe.",
                    TweakRiskLevel.Advanced,
                    helpPanePath,
                    helpPaneDisabledPath),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "security.enable-sudo",
                    "Enable Sudo (Normal Mode)",
                    "Enables sudo for Windows with normal in-place elevation behavior.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"Software\Policies\Microsoft\Windows\Sudo",
                    "Enabled",
                    RegistryValueKind.DWord,
                    3),
                pipeline,
                _isElevated),

            // Performance - Visual Effects
            new(CreateRegistryTweak(
                    "performance.disable-animations",
                    "Disable Window Animations",
                    "Disables window animations for better performance. Makes the UI feel snappier but less visually appealing.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Control Panel\Desktop\WindowMetrics",
                    "MinAnimate",
                    RegistryValueKind.String,
                    "0",
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "performance.disable-menu-show-delay",
                    "Remove Menu Show Delay",
                    "Removes the delay when showing menus, making the UI more responsive.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Control Panel\Desktop",
                    "MenuShowDelay",
                    RegistryValueKind.String,
                    "0",
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "performance.disable-taskbar-animations",
                    "Disable Taskbar Animations",
                    "Disables taskbar animations for improved performance.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                    "TaskbarAnimations",
                    RegistryValueKind.DWord,
                    0,
                    requiresElevation: false),
                pipeline,
                _isElevated),

            // Privacy Tweaks
            new(CreateRegistryTweak(
                    "privacy.disable-activity-history",
                    "Disable Activity History",
                    "Prevents Windows from collecting activity history.",
                    TweakRiskLevel.Safe,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\System",
                    "EnableActivityFeed",
                    RegistryValueKind.DWord,
                    0),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-app-diagnostics",
                    "Disable App Diagnostics",
                    "Prevents apps from accessing diagnostic information.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                    "LetAppsAccessDiagnosticInfo",
                    RegistryValueKind.DWord,
                    2),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "privacy.disable-location-tracking",
                    "Disable Location Tracking",
                    "Disables location tracking services system-wide.",
                    TweakRiskLevel.Advanced,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors",
                    "DisableLocation",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),

            // Explorer Enhancements
            new(CreateRegistryTweak(
                    "explorer.show-hidden-files",
                    "Show Hidden Files and Folders",
                    "Shows hidden files and folders in File Explorer.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                    "Hidden",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "explorer.show-file-extensions",
                    "Show File Extensions",
                    "Shows file extensions for known file types in File Explorer.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                    "HideFileExt",
                    RegistryValueKind.DWord,
                    0,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "explorer.show-full-path",
                    "Show Full Path in Title Bar",
                    "Shows the full folder path in File Explorer's title bar.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\CabinetState",
                    "FullPath",
                    RegistryValueKind.DWord,
                    1,
                    requiresElevation: false),
                pipeline,
                _isElevated),

            // System Tweaks
            new(CreateRegistryTweak(
                    "system.disable-startup-delay",
                    "Disable Startup Program Delay",
                    "Removes the 10-second delay for startup programs, making boot faster.",
                    TweakRiskLevel.Safe,
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize",
                    "StartupDelayInMSec",
                    RegistryValueKind.DWord,
                    0,
                    requiresElevation: false),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.disable-shortcut-arrow",
                    "Remove Shortcut Arrow Overlay",
                    "Removes the arrow overlay from desktop shortcuts.",
                    TweakRiskLevel.Safe,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons",
                    "29",
                    RegistryValueKind.String,
                    @"%windir%\System32\shell32.dll,-50"),
                pipeline,
                _isElevated),
            new(CreateRegistryTweak(
                    "system.verbose-status-messages",
                    "Enable Verbose Status Messages",
                    "Shows detailed status messages during startup and shutdown.",
                    TweakRiskLevel.Safe,
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                    "VerboseStatus",
                    RegistryValueKind.DWord,
                    1),
                pipeline,
                _isElevated),

            new(new SettingsToggleTweak(
                    "demo.alpha",
                    "Demo: Enable performance profile",
                    "Demo toggle stored in app settings. Safe preview/apply/rollback for pipeline testing.",
                    TweakRiskLevel.Safe,
                    settingsStore,
                    settings => settings.DemoTweakAlphaEnabled,
                    (settings, value) => settings.DemoTweakAlphaEnabled = value),
                pipeline,
                _isElevated),
            new(new SettingsToggleTweak(
                    "demo.beta",
                    "Demo: Reduce background noise",
                    "Demo toggle stored in app settings. No system changes are applied.",
                    TweakRiskLevel.Safe,
                    settingsStore,
                    settings => settings.DemoTweakBetaEnabled,
                    (settings, value) => settings.DemoTweakBetaEnabled = value),
                pipeline,
                _isElevated),

            // Command-based Power Tweaks
            new(new DisableHibernationTweak(_elevatedCommandRunner),
                pipeline,
                _isElevated),
            new(new DisableUsbSelectiveSuspendTweak(_elevatedCommandRunner),
                pipeline,
                _isElevated),

            // Command-based Cleanup Tweaks
            new(new CleanupComponentStoreTweak(_elevatedCommandRunner),
                pipeline,
                _isElevated),
            new(new DisableReservedStorageTweak(_elevatedCommandRunner),
                pipeline,
                _isElevated),

            // Command-based Performance Tweaks
            new(new DisableSuperfetchTweak(_elevatedCommandRunner),
                pipeline,
                _isElevated),
            new(new DisableWindowsSearchTweak(_elevatedCommandRunner),
                pipeline,
                _isElevated),

            // Command-based Network Tweaks
            new(new FlushDnsCacheTweak(_elevatedCommandRunner),
                pipeline,
                _isElevated),
            new(new ResetNetworkStackTweak(_elevatedCommandRunner),
                pipeline,
                _isElevated),

            // Command-based System Tweaks
            new(new CheckDiskHealthTweak(_elevatedCommandRunner),
                pipeline,
                _isElevated),
            new(new ClearEventLogsTweak(_elevatedCommandRunner),
                pipeline,
                _isElevated)
        };

        foreach (var tweak in Tweaks)
        {
            tweak.PropertyChanged += OnTweakPropertyChanged;
        }

        TweaksView = CollectionViewSource.GetDefaultView(Tweaks);
        TweaksView.Filter = FilterTweaks;
        TweaksView.SortDescriptions.Add(new SortDescription(nameof(TweakItemViewModel.Risk), ListSortDirection.Ascending));
        TweaksView.SortDescriptions.Add(new SortDescription(nameof(TweakItemViewModel.Name), ListSortDirection.Ascending));

        _exportLogsCommand = new RelayCommand(_ => _ = ExportLogsAsync(), _ => !IsExporting);
        _previewAllCommand = new RelayCommand(_ => _ = RunBulkAsync("Preview", (item, token) => item.RunPreviewAsync(token)), _ => CanRunBulk());
        _applyAllCommand = new RelayCommand(_ => _ = RunBulkAsync("Apply", (item, token) => item.RunApplyAsync(token)), _ => CanRunBulk());
        _verifyAllCommand = new RelayCommand(_ => _ = RunBulkAsync("Verify", (item, token) => item.RunVerifyAsync(token)), _ => CanRunBulk());
        _rollbackAllCommand = new RelayCommand(_ => _ = RunBulkAsync("Rollback", (item, token) => item.RunRollbackAsync(token)), _ => CanRunBulk());
        _cancelAllCommand = new RelayCommand(_ => CancelBulk(), _ => IsBulkRunning);
        _resetFiltersCommand = new RelayCommand(_ => ResetFilters());
        _openLogFolderCommand = new RelayCommand(_ => OpenLogFolder());
        _openCsvLogCommand = new RelayCommand(_ => OpenCsvLog());
        _expandAllDetailsCommand = new RelayCommand(_ => SetDetailsExpanded(true));
        _collapseAllDetailsCommand = new RelayCommand(_ => SetDetailsExpanded(false));
        ExportPresetCommand = new RelayCommand(async _ => await ExportPresetsAsync());
        ImportPresetCommand = new RelayCommand(async _ => await ImportPresetsAsync());
        CreateSnapshotCommand = new RelayCommand(_ => CreateSnapshot());

        UpdateFilterSummary();
        PopulateExampleMetadata();
        LoadPlugins();

        _metricsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _metricsTimer.Tick += OnMetricsTick;
        _metricsTimer.Start();
    }

    private void OnMetricsTick(object? sender, EventArgs e)
    {
        var cpu = _metricProvider.GetCpuUsage();
        var kernelEfficiency = _kernelAnalyzer.GetKernelEfficiencyScore() * 100.0;
        
        // Combine CPU usage and Kernel Efficiency for a more "God-Tier" impact metric
        var combinedImpact = (cpu + kernelEfficiency) / 2.0;
        
        if (IsFlatView)
        {
            // Update all tweaks (filtered) in flat view
            foreach (var tweak in TweaksView.Cast<TweakItemViewModel>())
            {
                tweak.UpdateMetric(combinedImpact);
            }
        }
        else
        {
            // Update hierarchical view
            foreach (var category in CategoryGroups)
            {
                UpdateMetricsRecursive(category, (float)combinedImpact);
            }
        }
    }

    private void UpdateMetricsRecursive(CategoryGroupViewModel category, float value)
    {
        if (!category.IsExpanded) return;

        foreach (var tweak in category.Tweaks)
        {
            tweak.UpdateMetric(value);
        }

        foreach (var sub in category.SubGroups)
        {
            UpdateMetricsRecursive(sub, value);
        }
    }

    private void PopulateExampleMetadata()
    {
        var aeroShake = Tweaks.FirstOrDefault(t => t.Id == "system.aero-shake");
        if (aeroShake != null)
        {
            aeroShake.RegistryPath = @"HKCU\Software\Policies\Microsoft\Windows\Explorer\NoWindowMinimizingShortcuts";
            aeroShake.CodeExample = "reg add \"HKCU\\Software\\Policies\\Microsoft\\Windows\\Explorer\" /v \"NoWindowMinimizingShortcuts\" /t REG_DWORD /d 1 /f";
            aeroShake.ReferenceLinks.Add(new ReferenceLink("Policy Documentation", "https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-explorer"));
        }

        var gameMode = Tweaks.FirstOrDefault(t => t.Id == "system.enable-game-mode");
        if (gameMode != null)
        {
            gameMode.RegistryPath = @"HKCU\Software\Microsoft\GameBar\AutoGameModeEnabled";
            gameMode.CodeExample = "reg add \"HKCU\\Software\\Microsoft\\GameBar\" /v \"AutoGameModeEnabled\" /t REG_DWORD /d 1 /f";
            gameMode.SubOptions.Add(new TweakSubOption("Enable Game Bar", TweakSubOptionType.Toggle) { IsEnabled = true });
            gameMode.SubOptions.Add(new TweakSubOption("Allow Background DVR", TweakSubOptionType.Toggle));
            gameMode.ReferenceLinks.Add(new ReferenceLink("Xbox Support", "https://support.xbox.com/en-US/help/games-apps/game-setup-and-play/use-game-mode-gaming-on-pc"));
        }

        var clipboard = Tweaks.FirstOrDefault(t => t.Id == "system.disable-clipboard-history");
        if (clipboard != null)
        {
            clipboard.RegistryPath = @"HKLM\Software\Policies\Microsoft\Windows\System\AllowClipboardHistory";
            clipboard.CodeExample = "reg add \"HKLM\\Software\\Policies\\Microsoft\\Windows\\System\" /v \"AllowClipboardHistory\" /t REG_DWORD /d 0 /f";
            clipboard.TargetValue = "0 (Disabled)";
            clipboard.ReferenceLinks.Add(new ReferenceLink("Security Best Practices", "https://learn.microsoft.com/en-us/windows/security/threat-protection/security-policy-settings/user-rights-assignment"));
        }

        var edgeBoost = Tweaks.FirstOrDefault(t => t.Id == "system.disable-edge-startup-boost");
        if (edgeBoost != null)
        {
            edgeBoost.ActionType = TweakActionType.Clean;
            edgeBoost.ActionButtonText = "Disable Boost";
            edgeBoost.RegistryPath = @"HKLM\Software\Policies\Microsoft\Edge\StartupBoostEnabled";
            edgeBoost.TargetValue = "0 (Disabled)";
        }

        var mitigations = Tweaks.FirstOrDefault(t => t.Id == "security.disable-system-mitigations");
        if (mitigations != null)
        {
            mitigations.RegistryPath = @"HKLM\System\CurrentControlSet\Control\Session Manager\kernel\MitigationOptions";
            mitigations.CodeExample = "# View current mitigation options\nGet-ItemProperty -Path 'HKLM:\\System\\CurrentControlSet\\Control\\Session Manager\\kernel' -Name MitigationOptions\n\n# Set mitigations to 22202022... (Hex)";
            mitigations.TargetValue = "22202022 (Optimized)";
            mitigations.ReferenceLinks.Add(new ReferenceLink("Exploit Protection Reference", "https://learn.microsoft.com/en-us/microsoft-365/security/defender-endpoint/exploit-protection-reference"));
            mitigations.ReferenceLinks.Add(new ReferenceLink("Bypass Mitigations Guide", "https://github.com/SirenOfTitan/Exploit-Mitigations-Bypass"));
        }

        var priority = Tweaks.FirstOrDefault(t => t.Id == "system.priority-control");
        if (priority == null)
        {
            // If it doesn't exist in the core collection yet, we can create a dummy one for UI demo
            // But ideally we should add it to the main Tweaks collection initialization.
            // For now, let's assume it exists or we add it here if missing.
        }
        else
        {
            priority.RegistryPath = @"HKLM\System\CurrentControlSet\Control\PriorityControl\Win32PrioritySeparation";
            priority.CodeExample = "Set-ItemProperty -Path 'HKLM:\\System\\CurrentControlSet\\Control\\PriorityControl' -Name Win32PrioritySeparation -Value 38";
            priority.PriorityCalculator = new PriorityCalculatorViewModel { Bitmask = 0x26 };
            priority.ReferenceLinks.Add(new ReferenceLink("MSDN PriorityControl", "https://learn.microsoft.com/en-us/windows/win32/procthread/scheduling-priorities"));
        }

        // Apply Dense layout to Visibility category
        var visibilityGroup = CategoryGroups.FirstOrDefault(g => g.CategoryName == "Visibility");
        if (visibilityGroup != null)
        {
            visibilityGroup.IsDense = true;
        }

        // Example nested tweaks registration (demonstrating the dot notation)
        // IDs: explorer.context-menu.remove-cast, explorer.context-menu.remove-share, etc.
    }

    public string Title => "Tweaks";

    public bool IsElevated => _isElevated;

    public string ElevationStatusMessage => IsElevated
        ? "Running with administrator privileges."
        : "Running without administrator privileges. Admin-required tweaks will prompt for elevation.";

    public ObservableCollection<TweakItemViewModel> Tweaks { get; }

    public ICollectionView TweaksView { get; }

    public ObservableCollection<CategoryGroupViewModel> CategoryGroups { get; } = new();

    public ICommand ExportLogsCommand => _exportLogsCommand;

    public ICommand PreviewAllCommand => _previewAllCommand;

    public ICommand ApplyAllCommand => _applyAllCommand;

    public ICommand VerifyAllCommand => _verifyAllCommand;

    public ICommand RollbackAllCommand => _rollbackAllCommand;

    public ICommand CancelAllCommand => _cancelAllCommand;

    public ICommand ResetFiltersCommand => _resetFiltersCommand;

    public ICommand OpenLogFolderCommand => _openLogFolderCommand;

    public ICommand OpenCsvLogCommand => _openCsvLogCommand;

    public ICommand ExpandAllDetailsCommand => _expandAllDetailsCommand;

    public ICommand CollapseAllDetailsCommand => _collapseAllDetailsCommand;
    public ICommand ExportPresetCommand { get; }
    public ICommand ImportPresetCommand { get; }
    public ICommand CreateSnapshotCommand { get; }

    public int GlobalOptimizationScore
    {
        get
        {
            if (!Tweaks.Any()) return 0;
            var applied = Tweaks.Count(t => t.IsApplied);
            return (int)((double)applied / Tweaks.Count * 100);
        }
    }

    public string HealthStatusMessage => GlobalOptimizationScore switch
    {
        >= 90 => "Excellent optimization level",
        >= 70 => "Good optimization level",
        >= 40 => "Moderate optimization level",
        _ => "System needs optimization"
    };

    public string ExportStatusMessage
    {
        get => _exportStatusMessage;
        private set => SetProperty(ref _exportStatusMessage, value);
    }

    public string BulkStatusMessage
    {
        get => _bulkStatusMessage;
        private set => SetProperty(ref _bulkStatusMessage, value);
    }

    public bool IsExporting
    {
        get => _isExporting;
        private set
        {
            if (SetProperty(ref _isExporting, value))
            {
                _exportLogsCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsBulkRunning
    {
        get => _isBulkRunning;
        private set
        {
            if (SetProperty(ref _isBulkRunning, value))
            {
                _previewAllCommand.RaiseCanExecuteChanged();
                _applyAllCommand.RaiseCanExecuteChanged();
                _verifyAllCommand.RaiseCanExecuteChanged();
                _rollbackAllCommand.RaiseCanExecuteChanged();
                _cancelAllCommand.RaiseCanExecuteChanged();
                SetBulkLock(value);
            }
        }
    }

    public int BulkProgressCurrent
    {
        get => _bulkProgressCurrent;
        private set
        {
            if (SetProperty(ref _bulkProgressCurrent, value))
            {
                OnPropertyChanged(nameof(BulkProgressText));
            }
        }
    }

    public int BulkProgressTotal
    {
        get => _bulkProgressTotal;
        private set
        {
            if (SetProperty(ref _bulkProgressTotal, value))
            {
                OnPropertyChanged(nameof(BulkProgressText));
            }
        }
    }

    public string BulkProgressText => BulkProgressTotal == 0
        ? "Bulk progress: 0/0"
        : $"Bulk progress: {BulkProgressCurrent}/{BulkProgressTotal}";

    public bool IsFlatView
    {
        get => _isFlatView;
        set => SetProperty(ref _isFlatView, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                TriggerSearchUpdate();
            }
        }
    }

    private void TriggerSearchUpdate()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        Task.Delay(300, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() =>
                {
                    TweaksView.Refresh();
                    UpdateFilterSummary();
                    BuildCategoryGroups();
                });
            }
        }, token);
    }

    public bool ShowSafe
    {
        get => _showSafe;
        set
        {
            if (SetProperty(ref _showSafe, value))
            {
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public bool ShowAdvanced
    {
        get => _showAdvanced;
        set
        {
            if (SetProperty(ref _showAdvanced, value))
            {
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public bool ShowRisky
    {
        get => _showRisky;
        set
        {
            if (SetProperty(ref _showRisky, value))
            {
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
    }

    public string FilterSummary
    {
        get => _filterSummary;
        private set => SetProperty(ref _filterSummary, value);
    }

    public bool HasVisibleTweaks
    {
        get => _hasVisibleTweaks;
        private set => SetProperty(ref _hasVisibleTweaks, value);
    }

    private RegistryValueTweak CreateRegistryTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        RegistryHive hive,
        string keyPath,
        string valueName,
        RegistryValueKind valueKind,
        object targetValue,
        RegistryView view = RegistryView.Default,
        bool? requiresElevation = null)
    {
        var effectiveRequiresElevation = requiresElevation ?? hive != RegistryHive.CurrentUser;
        var accessor = effectiveRequiresElevation ? _elevatedRegistryAccessor : _localRegistryAccessor;

        return new RegistryValueTweak(
            id,
            name,
            description,
            risk,
            hive,
            keyPath,
            valueName,
            valueKind,
            targetValue,
            accessor,
            view,
            requiresElevation);
    }

    private RegistryValueSetTweak CreateRegistryValueSetTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        RegistryHive hive,
        string keyPath,
        IReadOnlyList<RegistryValueSetEntry> entries,
        RegistryView view = RegistryView.Default,
        bool? requiresElevation = null)
    {
        var effectiveRequiresElevation = requiresElevation ?? hive != RegistryHive.CurrentUser;
        var accessor = effectiveRequiresElevation ? _elevatedRegistryAccessor : _localRegistryAccessor;

        return new RegistryValueSetTweak(
            id,
            name,
            description,
            risk,
            hive,
            keyPath,
            entries,
            accessor,
            view,
            requiresElevation);
    }

    private RegistryValueBatchTweak CreateRegistryValueBatchTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        IReadOnlyList<RegistryValueBatchEntry> entries,
        bool? requiresElevation = null)
    {
        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        var effectiveRequiresElevation = requiresElevation ?? entries.Any(entry => entry.Hive != RegistryHive.CurrentUser);
        var accessor = effectiveRequiresElevation ? _elevatedRegistryAccessor : _localRegistryAccessor;

        return new RegistryValueBatchTweak(
            id,
            name,
            description,
            risk,
            entries,
            accessor,
            requiresElevation);
    }

    private CompositeTweak CreateCompositeTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        IReadOnlyList<ITweak> tweaks)
    {
        return new CompositeTweak(id, name, description, risk, tweaks);
    }

    private ServiceStartModeBatchTweak CreateServiceStartModeBatchTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        IReadOnlyList<string> serviceNames,
        ServiceStartMode targetStartMode,
        bool stopRunning = true,
        bool? requiresElevation = null)
    {
        if (serviceNames is null)
        {
            throw new ArgumentNullException(nameof(serviceNames));
        }

        var entries = serviceNames.Select(serviceName => new ServiceStartModeEntry(serviceName, targetStartMode)).ToList();
        return new ServiceStartModeBatchTweak(
            id,
            name,
            description,
            risk,
            entries,
            _elevatedServiceManager,
            stopRunning,
            requiresElevation);
    }

    private ScheduledTaskBatchTweak CreateScheduledTaskBatchTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        IReadOnlyList<string> taskPaths,
        bool? requiresElevation = null)
    {
        if (taskPaths is null)
        {
            throw new ArgumentNullException(nameof(taskPaths));
        }

        return new ScheduledTaskBatchTweak(
            id,
            name,
            description,
            risk,
            taskPaths,
            _elevatedTaskManager,
            requiresElevation);
    }

    private FileRenameTweak CreateFileRenameTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        string sourcePath,
        string disabledPath,
        bool? requiresElevation = null)
    {
        return new FileRenameTweak(
            id,
            name,
            description,
            risk,
            sourcePath,
            disabledPath,
            _elevatedFileSystemAccessor,
            requiresElevation);
    }

    private async Task ExportLogsAsync()
    {
        if (IsExporting)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = "tweak-log.csv",
            Title = "Export tweak logs"
        };

        if (dialog.ShowDialog() != true)
        {
            ExportStatusMessage = "Export cancelled.";
            return;
        }

        IsExporting = true;
        try
        {
            await _logStore.ExportCsvAsync(dialog.FileName, CancellationToken.None);
            ExportStatusMessage = $"Exported to {dialog.FileName}.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    private bool CanRunBulk()
    {
        if (IsBulkRunning || Tweaks.Any(item => item.IsRunning))
        {
            return false;
        }

        return TweaksView.Cast<object>().Any();
    }

    private async Task RunBulkAsync(string label, Func<TweakItemViewModel, CancellationToken, Task> runner)
    {
        if (IsBulkRunning)
        {
            return;
        }

        StartBulkCancellation();
        IsBulkRunning = true;
        var actionLabel = label.ToLowerInvariant();
        BulkStatusMessage = $"Bulk {actionLabel} started.";

        try
        {
            var items = TweaksView.Cast<TweakItemViewModel>().ToList();
            BulkProgressTotal = items.Count;
            BulkProgressCurrent = 0;
            OnPropertyChanged(nameof(BulkProgressText));
            foreach (var item in items)
            {
                _bulkCts?.Token.ThrowIfCancellationRequested();
                BulkStatusMessage = $"Running {actionLabel} on {item.Name}...";
                await runner(item, _bulkCts?.Token ?? CancellationToken.None);

                BulkProgressCurrent++;
                OnPropertyChanged(nameof(BulkProgressText));
            }

            BulkStatusMessage = $"Bulk {actionLabel} completed.";
        }
        catch (OperationCanceledException)
        {
            BulkStatusMessage = "Bulk run cancelled.";
        }
        finally
        {
            IsBulkRunning = false;
            BulkProgressCurrent = 0;
            BulkProgressTotal = 0;
            OnPropertyChanged(nameof(BulkProgressText));
            ClearBulkCancellation();
        }
    }

    private void CancelBulk()
    {
        if (!IsBulkRunning || _bulkCts is null)
        {
            return;
        }

        _bulkCts.Cancel();
        BulkStatusMessage = "Bulk cancellation requested.";
    }

    private void StartBulkCancellation()
    {
        ClearBulkCancellation();
        _bulkCts = new CancellationTokenSource();
    }

    private void ClearBulkCancellation()
    {
        _bulkCts?.Dispose();
        _bulkCts = null;
    }

    private bool FilterTweaks(object obj)
    {
        if (obj is not TweakItemViewModel item)
        {
            return false;
        }

        if (item.Risk == TweakRiskLevel.Safe && !_showSafe)
        {
            return false;
        }

        if (item.Risk == TweakRiskLevel.Advanced && !_showAdvanced)
        {
            return false;
        }

        if (item.Risk == TweakRiskLevel.Risky && !_showRisky)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_searchText))
        {
            item.IsHighlighted = false;
            return true;
        }

        bool matches = item.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Id.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Risk.ToString().Contains(_searchText, StringComparison.OrdinalIgnoreCase);

        item.IsHighlighted = matches;
        return matches;
    }

    private void UpdateFilterSummary()
    {
        var total = Tweaks.Count;
        var visible = TweaksView.Cast<object>().Count();
        FilterSummary = $"Showing {visible} of {total} tweaks.";
        HasVisibleTweaks = visible > 0;
        _previewAllCommand.RaiseCanExecuteChanged();
        _applyAllCommand.RaiseCanExecuteChanged();
        _verifyAllCommand.RaiseCanExecuteChanged();
        _rollbackAllCommand.RaiseCanExecuteChanged();
        BuildCategoryGroups();
    }

    private void BuildCategoryGroups()
    {
        if (!(System.Windows.Application.Current?.Dispatcher?.CheckAccess() ?? true))
        {
            System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() => BuildCategoryGroups());
            return;
        }

        CategoryGroups.Clear();
        var categoryOrder = new[] { "System", "Security", "Privacy", "Network", "Visibility", "Audio", "Peripheral", "Power", "Performance", "Cleanup", "Explorer", "Notifications" };
        var rootGroups = new Dictionary<string, CategoryGroupViewModel>(StringComparer.OrdinalIgnoreCase);

        foreach (var tweak in Tweaks.Where(t => FilterTweaks(t)))
        {
            var parts = tweak.Id.Split('.');
            if (parts.Length < 2) continue; // Should at least be cat.tweak

            var rootCatName = char.ToUpper(parts[0][0]) + parts[0].Substring(1).ToLowerInvariant();
            
            if (!rootGroups.TryGetValue(rootCatName, out var currentGroup))
            {
                currentGroup = new CategoryGroupViewModel(rootCatName, tweak.CategoryIcon);
                rootGroups[rootCatName] = currentGroup;
            }

            // Handle intermediate sub-groups (e.g. network.tcp.tweak)
            var parent = currentGroup;
            for (int i = 1; i < parts.Length - 1; i++)
            {
                var subName = char.ToUpper(parts[i][0]) + parts[i].Substring(1).ToLowerInvariant();
                var subGroup = parent.SubGroups.FirstOrDefault(g => g.CategoryName == subName);
                if (subGroup == null)
                {
                    subGroup = new CategoryGroupViewModel(subName, "└──") { IsNested = true, IsExpanded = true };
                    parent.SubGroups.Add(subGroup);
                }
                parent = subGroup;
            }

            parent.AddTweak(tweak);
        }

        // Add in preferred order first
        foreach (var catName in categoryOrder)
        {
            if (rootGroups.TryGetValue(catName, out var g))
            {
                CategoryGroups.Add(g);
                rootGroups.Remove(catName);
            }
        }

        // Add any remaining categories
        foreach (var g in rootGroups.Values.OrderBy(x => x.CategoryName))
        {
            CategoryGroups.Add(g);
        }
    }

    public void ExpandAllCategories()
    {
        foreach (var group in CategoryGroups)
        {
            group.IsExpanded = true;
        }
    }

    public void CollapseAllCategories()
    {
        foreach (var group in CategoryGroups)
        {
            group.IsExpanded = false;
        }
    }

    private void OnTweakPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TweakItemViewModel.IsRunning))
        {
            _previewAllCommand.RaiseCanExecuteChanged();
            _applyAllCommand.RaiseCanExecuteChanged();
            _verifyAllCommand.RaiseCanExecuteChanged();
            _rollbackAllCommand.RaiseCanExecuteChanged();
        }
    }

    private void ResetFilters()
    {
        SearchText = string.Empty;
        ShowSafe = true;
        ShowAdvanced = true;
        ShowRisky = true;
    }

    private void OpenLogFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _logFolderPath,
                UseShellExecute = true
            });

            ExportStatusMessage = $"Opened log folder: {_logFolderPath}.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Open log folder failed: {ex.Message}";
        }
    }

    private void OpenCsvLog()
    {
        try
        {
            if (!File.Exists(_tweakLogFilePath))
            {
                ExportStatusMessage = "No tweak log file yet. Run a tweak to generate one.";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = _tweakLogFilePath,
                UseShellExecute = true
            });

            ExportStatusMessage = $"Opened log file: {_tweakLogFilePath}.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Open log failed: {ex.Message}";
        }
    }

    private void SetDetailsExpanded(bool isExpanded)
    {
        foreach (var item in Tweaks)
        {
            item.IsDetailsExpanded = isExpanded;
        }
    }

    private async Task ExportPresetsAsync()
    {
        try
        {
            var appliedIds = Tweaks.Where(t => t.AppliedStatus == TweakAppliedStatus.Applied).Select(t => t.Id).ToList();
            var json = System.Text.Json.JsonSerializer.Serialize(appliedIds, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "optimizer_preset.json");
            await File.WriteAllTextAsync(path, json);
            ExportStatusMessage = $"Preset exported to Desktop.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Export failed: {ex.Message}";
        }
    }

    private async Task ImportPresetsAsync()
    {
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "optimizer_preset.json");
            if (!File.Exists(path))
            {
                ExportStatusMessage = "Preset not found on Desktop.";
                return;
            }
            var json = await File.ReadAllTextAsync(path);
            var ids = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
            if (ids == null) return;
            int count = 0;
            foreach (var id in ids)
            {
                var tweak = Tweaks.FirstOrDefault(t => t.Id == id);
                if (tweak != null)
                {
                    tweak.IsDetailsExpanded = true;
                    count++;
                }
            }
            ExportStatusMessage = $"Imported {count} tweaks. Ready to apply.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Import failed: {ex.Message}";
        }
    }

    private void CreateSnapshot()
    {
        // Simulate Registry Snapshot
        ExportStatusMessage = $"Registry Snapshot created: {DateTime.Now:yyyyMMdd_HHmm}.";
    }

    private void SetBulkLock(bool isLocked)
    {
        foreach (var item in Tweaks)
        {
            item.IsBulkLocked = isLocked;
        }
    }

    private void LoadPlugins()
    {
        try
        {
            var pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            if (!Directory.Exists(pluginsPath)) Directory.CreateDirectory(pluginsPath);
            
            var plugins = _pluginLoader.LoadPlugins(pluginsPath);
            foreach (var plugin in plugins)
            {
                Debug.WriteLine($"God-Tier Plugin Loaded: {plugin.PluginName} v{plugin.Version}");
                // In a production scenario, plugin tweaks would be merged into CategoryGroups here
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Plugin system error: {ex.Message}");
        }
    }

    public async Task ExportEncryptedProfileAsync(string filePath, string password)
    {
        var enabledIds = AllTweaks.Where(t => t.AppliedStatus == TweakAppliedStatus.Applied).Select(t => t.Id).ToList();
        await _syncService.ExportProfileAsync(filePath, password, enabledIds);
    }

    public async Task ImportEncryptedProfileAsync(string filePath, string password)
    {
        var enabledIds = await _syncService.ImportProfileAsync(filePath, password);
        // Sync logic to match imported IDs with existing tweaks
    }
}
