using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.App.Utilities;
using WindowsOptimizer.Infrastructure;

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
    private CancellationTokenSource? _bulkCts;
    private readonly string _logFolderPath;
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

        Tweaks = new ObservableCollection<TweakItemViewModel>
        {
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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
            new(new RegistryValueTweak(
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

        UpdateFilterSummary();
    }

    public string Title => "Tweaks";

    public bool IsElevated => _isElevated;

    public string ElevationStatusMessage => IsElevated
        ? "Running with administrator privileges."
        : "Running without administrator privileges. Admin-required tweaks are locked.";

    public ObservableCollection<TweakItemViewModel> Tweaks { get; }

    public ICollectionView TweaksView { get; }

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

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                TweaksView.Refresh();
                UpdateFilterSummary();
            }
        }
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
            return true;
        }

        return item.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Id.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Risk.ToString().Contains(_searchText, StringComparison.OrdinalIgnoreCase);
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

    private void SetBulkLock(bool isLocked)
    {
        foreach (var item in Tweaks)
        {
            item.IsBulkLocked = isLocked;
        }
    }
}
