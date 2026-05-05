using System;
using System.Collections.Generic;
using System.IO;
using RegProbe.Core.Commands;

namespace RegProbe.Infrastructure.Commands;

public sealed class CommandAllowlist
{
    private readonly Dictionary<string, List<string[]>> _allowed;
    private readonly string _systemDirectory;
    private readonly bool _allowUnsafeDeveloperCommands;

    public CommandAllowlist(Dictionary<string, List<string[]>> allowed, string systemDirectory, bool allowUnsafeDeveloperCommands = false)
    {
        _allowed = allowed ?? throw new ArgumentNullException(nameof(allowed));
        _systemDirectory = string.IsNullOrWhiteSpace(systemDirectory)
            ? throw new ArgumentException("System directory is required.", nameof(systemDirectory))
            : Path.GetFullPath(systemDirectory);
        _allowUnsafeDeveloperCommands = allowUnsafeDeveloperCommands;
    }

    public static CommandAllowlist CreateDefault(bool allowUnsafeDeveloperCommands = false)
    {
        var systemDirectory = Environment.SystemDirectory;
        var powercfg = Path.Combine(systemDirectory, "powercfg.exe");
        var dism = Path.Combine(systemDirectory, "dism.exe");
        var bcdedit = Path.Combine(systemDirectory, "bcdedit.exe");
        var sc = Path.Combine(systemDirectory, "sc.exe");
        var ipconfig = Path.Combine(systemDirectory, "ipconfig.exe");
        var netsh = Path.Combine(systemDirectory, "netsh.exe");
        var reg = Path.Combine(systemDirectory, "reg.exe");
        var chkdsk = Path.Combine(systemDirectory, "chkdsk.exe");
        var wevtutil = Path.Combine(systemDirectory, "wevtutil.exe");
        var vssadmin = Path.Combine(systemDirectory, "vssadmin.exe");
        var cleanmgr = Path.Combine(systemDirectory, "cleanmgr.exe");
        var cscript = Path.Combine(systemDirectory, "cscript.exe");
        var powershell = Path.Combine(systemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
        var regAllowlist = new List<string[]>
        {
            new[] { "query", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "/v", "EnableLUA" },
            new[] { "add", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "/v", "EnableLUA", "/t", "REG_DWORD", "/d", "0", "/f" },
            new[] { "add", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "/v", "EnableLUA", "/t", "REG_DWORD", "/d", "1", "/f" },
            new[] { "delete", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "/v", "EnableLUA", "/f" },
            new[] { "query", @"HKLM\SOFTWARE\Policies\Microsoft\FindMyDevice", "/v", "AllowFindMyDevice" },
            new[] { "add", @"HKLM\SOFTWARE\Policies\Microsoft\FindMyDevice", "/v", "AllowFindMyDevice", "/t", "REG_DWORD", "/d", "0", "/f" },
            new[] { "add", @"HKLM\SOFTWARE\Policies\Microsoft\FindMyDevice", "/v", "AllowFindMyDevice", "/t", "REG_DWORD", "/d", "1", "/f" },
            new[] { "delete", @"HKLM\SOFTWARE\Policies\Microsoft\FindMyDevice", "/v", "AllowFindMyDevice", "/f" }
        };

        static void AddRegDwordRule(List<string[]> allowlist, string keyPath, string valueName, params string[] values)
        {
            foreach (var value in values)
            {
                allowlist.Add(new[] { "add", keyPath, "/v", valueName, "/t", "REG_DWORD", "/d", value, "/f" });
            }

            allowlist.Add(new[] { "delete", keyPath, "/v", valueName, "/f" });
        }

        static void AddRegSzRule(List<string[]> allowlist, string keyPath, string valueName, params string[] values)
        {
            foreach (var value in values)
            {
                allowlist.Add(new[] { "add", keyPath, "/v", valueName, "/t", "REG_SZ", "/d", value, "/f" });
            }

            allowlist.Add(new[] { "delete", keyPath, "/v", valueName, "/f" });
        }

        AddRegDwordRule(regAllowlist, @"HKCU\Software\Policies\Microsoft\Windows\Explorer", "DisableSearchHistory", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKCU\Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKCU\Software\Policies\Microsoft\Windows\Explorer", "HideRecommendedSection", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKCU\Software\Policies\Microsoft\Windows\Explorer", "HideRecommendedPersonalizedSites", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "TurnOffSPIAnimations", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "NoConnectedUser", "0", "1", "2", "3");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\Explorer", "HideRecommendedSection", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\Explorer", "HideRecommendedPersonalizedSites", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AdvertisingInfo", "DisabledByGroupPolicy", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessDiagnosticInfo", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsGetDiagnosticInfo", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\AppV\CEIP", "CEIPEnable", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Messenger\Client", "CEIP", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\CloudContent", "DisableConsumerAccountStateContent", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\CloudContent", "DisableSoftLanding", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\DataCollection", "AllowDeviceNameInTelemetry", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\DataCollection", "DisableDeviceDelete", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\DataCollection", "DisableDiagnosticDataViewer", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\DataCollection", "DoNotShowFeedbackNotifications", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\DataCollection", "DisableOneSettingsDownloads", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\DataCollection", "LimitDiagnosticLogCollection", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\DataCollection", "LimitDumpCollection", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", "0", "1", "3");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet", "SubmitSamplesConsent", "0", "1", "2", "3");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "VerboseStatus", "0", "1", "2");
        AddRegSzRule(regAllowlist, @"HKLM\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location", "Value", "Allow", "Deny");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock", "AllowDevelopmentWithoutDevLicense", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\System", "AllowClipboardHistory", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\System", "AllowCrossDeviceClipboard", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\System", "EnableFontProviders", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\System", "EnableMmx", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\System", "NoLocalPasswordResetQuestions", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\NetCache", "Enabled", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\DWM", "DisallowAnimations", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\Appx", "AllowAutomaticAppArchiving", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppCompat", "AITEnable", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppCompat", "DisablePCA", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppCompat", "DisableEngine", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppCompat", "DisablePcaUI", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppCompat", "SbEnable", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppCompat", "DisableUAR", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppCompat", "DisableAPISamping", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppCompat", "DisableApplicationFootprint", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppCompat", "DisableInstallTracing", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppCompat", "DisableWin32AppBackup", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\StorageSense", "AllowStorageSenseGlobal", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\StorageSense", "AllowStorageSenseTemporaryFilesCleanup", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\CredUI", "DisablePasswordReveal", "0", "1", "2");
        foreach (var valueName in new[]
                 {
                     "DisableSettingSync",
                     "DisableAppSyncSettingSync",
                     "DisableApplicationSettingSync",
                     "DisableCredentialsSettingSync",
                     "DisablePersonalizationSettingSync",
                     "DisableDesktopThemeSettingSync",
                     "DisableStartLayoutSettingSync",
                     "DisableWebBrowserSettingSync",
                     "DisableWindowsSettingSync",
                     "DisableSettingSyncUserOverride",
                     "DisableAppSyncSettingSyncUserOverride",
                     "DisableApplicationSettingSyncUserOverride",
                     "DisableCredentialsSettingSyncUserOverride",
                     "DisablePersonalizationSettingSyncUserOverride",
                     "DisableDesktopThemeSettingSyncUserOverride",
                     "DisableStartLayoutSettingSyncUserOverride",
                     "DisableWebBrowserSettingSyncUserOverride",
                     "DisableWindowsSettingSyncUserOverride",
                     "DisableSyncOnPaidNetwork"
                 })
        {
            AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\SettingSync", valueName, "0", "1", "2");
        }
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessAccountInfo", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessBackgroundSpatialPerception", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessCalendar", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessCallHistory", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessCamera", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessContacts", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessEmail", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessGazeInput", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessGraphicsCaptureProgrammatic", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessGraphicsCaptureWithoutBorder", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessHumanPresence", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessLocation", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessMessaging", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessMicrophone", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessMotion", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessNotifications", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessPhone", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessRadios", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessTasks", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessTrustedDevices", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsActivateWithVoice", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsActivateWithVoiceAboveLock", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\AppPrivacy", "LetAppsSyncWithDevices", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\FileHistory", "Disabled", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocationScripting", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\LocationAndSensors", "DisableSensors", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\LocationAndSensors", "DisableWindowsLocationProvider", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\Messaging", "AllowMessageSync", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\CurrentVersion\MDM", "DisableRegistration", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\NetworkConnectivityStatusIndicator", "NoActiveProbe", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows NT\DNSClient", "EnableMDNS", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\LLTD", "EnableLLTDIO", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\LLTD", "EnableRspndr", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\System", "DisableAcrylicBackgroundOnLogon", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\System", "RSoPLogging", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\System", "EnableFirstLogonAnimation", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\Personalization", "NoLockScreenCamera", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\Personalization", "NoChangingLockScreen", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\Personalization", "AnimateLockScreenBackground", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\Personalization", "NoLockScreen", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\Personalization", "NoLockScreenSlideshow", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\Troubleshooting\AllowRecommendations", "TroubleshootingAllowRecommendations", "0", "1", "2", "3", "4", "5");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\WCN\UI", "DisableWcnUi", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance", "MaintenanceDisabled", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", "10", "20", "30", "40", "50", "60", "70", "80", "90", "100");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Affinity", "0");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority", "8");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority", "2", "8");
        AddRegSzRule(regAllowlist, @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Scheduling Category", "High", "Medium", "Normal");
        AddRegSzRule(regAllowlist, @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "SFIO Priority", "High", "Normal");
        AddRegDwordRule(regAllowlist, @"HKLM\System\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\CrashControl", "AutoReboot", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\CrashControl", "DisplayParameters", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "DisableTaskOffload", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", "DisabledComponents", "0", "32", "255");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows NT\Terminal Services", "fAllowToGetHelp", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableBkGndGroupPolicy", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DoNotUseWebResults", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "ConnectedSearchUseWeb", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowIndexingEncryptedStoresOrItems", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "EnableDynamicContentInWSB", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "PreventRemoteQueries", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Edge", "SearchSuggestEnabled", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Edge", "LocalProvidersEnabled", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\MicrosoftEdge\SearchScopes", "ShowSearchSuggestionsGlobal", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoLowDiskSpaceChecks", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "UseDefaultTile", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "AllowOnlineTips", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\System", "DontDisplayLastUserName", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\System", "DontDisplayUserName", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\Explorer", "NoUseStoreOpenWith", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\Explorer", "ShowHibernateOption", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\Explorer", "ShowLockOption", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\Explorer", "ShowSleepOption", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\Explorer", "ShowOrHideMostUsedApps", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\LanmanWorkstation", "EnableSMBQUIC", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\LanmanServer", "EnableSMBQUIC", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\LanmanWorkstation", "MinSmb2Dialect", "514", "528", "768", "770", "785");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\LanmanWorkstation", "MaxSmb2Dialect", "514", "528", "768", "770", "785");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\LanmanServer", "MinSmb2Dialect", "514", "528", "768", "770", "785");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\LanmanServer", "MaxSmb2Dialect", "514", "528", "768", "770", "785");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\System", "BlockDomainPicturePassword", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows\System", "EnableCdp", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\Software\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform", "NoGenTicket", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Services\Beep", "Start", "1", "4");
        AddRegSzRule(regAllowlist, @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons", "29", @"%windir%\System32\shell32.dll,-50");
        AddRegSzRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "NODE_OPTIONS", "--max-old-space-size=8192");
        AddRegSzRule(regAllowlist, @"HKLM\SOFTWARE\Policies\Microsoft\Windows\PowerShell", "ExecutionPolicy", "RemoteSigned", "AllSigned", "Unrestricted");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "AutoShareServer", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "AutoShareWks", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "SMB1", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "SMB2", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\System\CurrentControlSet\Services\LanmanWorkstation\Parameters", "EnableSecuritySignature", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\System\CurrentControlSet\Services\LanmanWorkstation\Parameters", "EnablePlainTextPassword", "0", "1");
        AddRegDwordRule(regAllowlist, @"HKLM\System\CurrentControlSet\Services\LanmanWorkstation\Parameters", "RequireSecuritySignature", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters", "DirectoryCacheEntriesMax", "16", "128", "4096");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters", "FileInfoCacheEntriesMax", "64", "128", "32768");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters", "FileNotFoundCacheEntriesMax", "128", "32768");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters", "MaxCmds", "15", "64", "32768");
        AddRegDwordRule(regAllowlist, @"HKLM\System\CurrentControlSet\Services\LanmanServer\Parameters", "EnableSecuritySignature", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\System\CurrentControlSet\Services\LanmanServer\Parameters", "RequireSecuritySignature", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "TdrDdiDelay", "5");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "TdrDelay", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "TdrLevel", "0", "1", "2", "3");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "TdrLimitCount", "5");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "TdrLimitTime", "60");
        AddRegDwordRule(regAllowlist, @"HKLM\SOFTWARE\Microsoft\Windows\Dwm", "OverlayMinFPS", "0");
        AddRegDwordRule(regAllowlist, @"HKLM\System\CurrentControlSet\Control", "RegistrySizeLimit", "0");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsDisable8dot3NameCreation", "0", "1", "2", "3");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsDisableLastAccessUpdate", "0", "1", "2", "3", "-2147483646", "2147483650");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsMftZoneReservation", "0", "1", "2", "3", "4");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsMemoryUsage", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "ClearPageFileAtShutdown", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", "0", "1", "2");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "NonPagedPoolSize", "0");
        AddRegDwordRule(regAllowlist, @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "PagedPoolSize", "0");

        var allowed = new Dictionary<string, List<string[]>>(StringComparer.OrdinalIgnoreCase)
        {
            [powercfg] = new List<string[]>
            {
                // Hibernation control
                new[] { "/hibernate", "off" },
                new[] { "/hibernate", "on" },

                // Query commands (safe read-only operations)
                new[] { "/query" },
                new[] { "/list" },
                new[] { "/availablesleepstates" },

                // USB selective suspend (AC power)
                new[] { "/setacvalueindex", "SCHEME_CURRENT", "SUB_USB", "USBSELECTIVESUSPEND", "0" },
                new[] { "/setacvalueindex", "SCHEME_CURRENT", "SUB_USB", "USBSELECTIVESUSPEND", "1" },

                // USB selective suspend (DC/battery power)
                new[] { "/setdcvalueindex", "SCHEME_CURRENT", "SUB_USB", "USBSELECTIVESUSPEND", "0" },
                new[] { "/setdcvalueindex", "SCHEME_CURRENT", "SUB_USB", "USBSELECTIVESUSPEND", "1" },

                // Hidden processor core parking settings
                new[] { "/qh", "SCHEME_CURRENT", "SUB_PROCESSOR", "CPMINCORES" },
                new[] { "/qh", "SCHEME_CURRENT", "SUB_PROCESSOR", "CPMAXCORES" },
                new[] { "/setacvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", "CPMINCORES", "100" },
                new[] { "/setdcvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", "CPMINCORES", "100" },
                new[] { "/setacvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", "CPMAXCORES", "100" },
                new[] { "/setdcvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", "CPMAXCORES", "100" },

                // Apply power scheme changes
                new[] { "/setactive", "SCHEME_CURRENT" },

                // Power scheme management
                new[] { "/getactivescheme" },
                new[] { "/setactive", "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c" }, // High performance
                new[] { "/setactive", "381b4222-f694-41f0-9685-ff5bb260df2e" }, // Balanced
                new[] { "/setactive", "a1841308-3541-4fab-bc81-f71556f20b4a" }  // Power saver
            },
            [dism] = new List<string[]>
            {
                // Reserved storage control
                new[] { "/online", "/Set-ReservedStorageState", "/State:Disabled", "/NoRestart" },
                new[] { "/online", "/Set-ReservedStorageState", "/State:Enabled", "/NoRestart" },
                new[] { "/online", "/Get-ReservedStorageState" },

                // Component store cleanup
                new[] { "/online", "/Cleanup-Image", "/StartComponentCleanup" },
                new[] { "/online", "/Cleanup-Image", "/StartComponentCleanup", "/ResetBase" },
                new[] { "/online", "/Cleanup-Image", "/AnalyzeComponentStore" },

                // Image health check (safe read-only)
                new[] { "/online", "/Cleanup-Image", "/CheckHealth" },
                new[] { "/online", "/Cleanup-Image", "/ScanHealth" }
            },
            [bcdedit] = new List<string[]>
            {
                // Query only (safe read-only operation)
                new[] { "/enum" },
                new[] { "/v" },

                // DEP (Data Execution Prevention) settings
                new[] { "/set", "{current}", "nx", "AlwaysOn" },
                new[] { "/set", "{current}", "nx", "AlwaysOff" },
                new[] { "/set", "{current}", "nx", "OptIn" },
                new[] { "/set", "{current}", "nx", "OptOut" },

                // Boot menu timeout
                new[] { "/timeout", "3" },
                new[] { "/timeout", "5" },
                new[] { "/timeout", "10" },
                new[] { "/timeout", "30" }
            },
            [sc] = new List<string[]>
            {
                // Service control - query (safe read-only)
                new[] { "query", "SysMain" },
                new[] { "query", "WSearch" },
                new[] { "query", "DoSvc" },
                new[] { "query", "FontCache" },

                // Service control - stop/start
                new[] { "stop", "SysMain" },
                new[] { "start", "SysMain" },
                new[] { "stop", "WSearch" },
                new[] { "start", "WSearch" },
                new[] { "stop", "DoSvc" },
                new[] { "start", "DoSvc" },
                new[] { "stop", "FontCache" },
                new[] { "start", "FontCache" }
            },
            [ipconfig] = new List<string[]>
            {
                // Display DNS cache (read-only)
                new[] { "/displaydns" },

                // Flush DNS cache
                new[] { "/flushdns" },

                // Release/renew IP
                new[] { "/release" },
                new[] { "/renew" },

                // Display all network info (read-only)
                new[] { "/all" }
            },
            [netsh] = new List<string[]>
            {
                // Winsock reset
                new[] { "winsock", "reset" },
                new[] { "winsock", "show", "catalog" },

                // IP reset
                new[] { "int", "ip", "reset" },

                // Interface management (read-only)
                new[] { "interface", "show", "interface" },
                new[] { "interface", "ip", "show", "config" }
            },
            [reg] = regAllowlist,
            [chkdsk] = new List<string[]>
            {
                // Check disk health (read-only)
                new[] { "C:" },
                new[] { "D:" },
                new[] { "E:" }
            },
            [wevtutil] = new List<string[]>
            {
                // Get log info (read-only)
                new[] { "gli", "Application" },
                new[] { "gli", "System" },
                new[] { "gli", "Security" },

                // Clear logs
                new[] { "cl", "Application" },
                new[] { "cl", "System" },
                new[] { "cl", "Security" },

                // Enum all logs (read-only)
                new[] { "el" }
            },
            [vssadmin] = new List<string[]>
            {
                // List shadow copies (read-only)
                new[] { "list", "shadows" },
                new[] { "list", "shadows", "/for=C:" },
                new[] { "list", "shadows", "/for=D:" },
                new[] { "list", "shadows", "/for=E:" },

                // Delete all shadow copies (RISKY - requires confirmation)
                new[] { "delete", "shadows", "/all", "/quiet" },
                new[] { "delete", "shadows", "/for=C:", "/all", "/quiet" },
                new[] { "delete", "shadows", "/for=D:", "/all", "/quiet" },
                new[] { "delete", "shadows", "/for=E:", "/all", "/quiet" }
            },
            [powershell] = new List<string[]>
            {
                // Clear Recycle Bin (safe, requires confirmation in UI)
                new[] { "-NoProfile", "-NonInteractive", "-Command", "Clear-RecycleBin", "-Force", "-ErrorAction", "SilentlyContinue" },
                new[] { "-NoProfile", "-NonInteractive", "-Command", "Clear-RecycleBin", "-DriveLetter", "C", "-Force", "-ErrorAction", "SilentlyContinue" },
                new[] { "-NoProfile", "-NonInteractive", "-Command", "Clear-RecycleBin", "-DriveLetter", "D", "-Force", "-ErrorAction", "SilentlyContinue" },

                // Clear clipboard (safe)
                new[] { "-NoProfile", "-NonInteractive", "-Command", "Set-Clipboard", "-Value", "$null" },

                // SMB server leasing
                new[] { "-NoProfile", "-NonInteractive", "-Command", "$value = (Get-SmbServerConfiguration).EnableLeasing; if ($value) { Write-Output 'True' } else { Write-Output 'False' }" },
                new[] { "-NoProfile", "-NonInteractive", "-Command", "Set-SmbServerConfiguration -EnableLeasing $false -Force | Out-Null; Write-Output 'EnableLeasing=False'" },
                new[] { "-NoProfile", "-NonInteractive", "-Command", "Set-SmbServerConfiguration -EnableLeasing $true -Force | Out-Null; Write-Output 'EnableLeasing=True'" },

                // SMB multichannel
                new[] { "-NoProfile", "-NonInteractive", "-Command", "$client = (Get-SmbClientConfiguration).EnableMultiChannel; $server = (Get-SmbServerConfiguration).EnableMultiChannel; [pscustomobject]@{ ClientEnableMultiChannel = [bool]$client; ServerEnableMultiChannel = [bool]$server } | ConvertTo-Json -Compress" },
                new[] { "-NoProfile", "-NonInteractive", "-Command", "Set-SmbClientConfiguration -EnableMultiChannel $true -Force | Out-Null; Set-SmbServerConfiguration -EnableMultiChannel $true -Force | Out-Null; Write-Output '{\"ClientEnableMultiChannel\":true,\"ServerEnableMultiChannel\":true}'" },
                new[] { "-NoProfile", "-NonInteractive", "-Command", "Set-SmbClientConfiguration -EnableMultiChannel $false -Force | Out-Null; Set-SmbServerConfiguration -EnableMultiChannel $false -Force | Out-Null; Write-Output '{\"ClientEnableMultiChannel\":false,\"ServerEnableMultiChannel\":false}'" }
            },
            [cscript] = new List<string[]>
            {
                // Product key removal (slmgr.vbs)
                new[] { "//NoLogo", Path.Combine(systemDirectory, "slmgr.vbs"), "/cpky" },
                new[] { "//NoLogo", Path.Combine(systemDirectory, "slmgr.vbs"), "/dli" }
            }
        };

        return new CommandAllowlist(allowed, systemDirectory, allowUnsafeDeveloperCommands);
    }

    public bool IsAllowed(CommandRequest request, out string? reason)
    {
        if (request is null)
        {
            reason = "Request is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Executable))
        {
            reason = "Executable path is required.";
            return false;
        }

        var normalizedExecutable = NormalizeExecutable(request.Executable);
        if (normalizedExecutable is null)
        {
            reason = "Executable must be a full path under System32.";
            return false;
        }

        if (!_allowed.TryGetValue(normalizedExecutable, out var allowedArguments))
        {
            reason = "Executable is not allowlisted.";
            return false;
        }

        if (Path.GetFileName(normalizedExecutable).Equals("reg.exe", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var args in allowedArguments)
            {
                if (ArgumentsMatch(args, request.Arguments))
                {
                    reason = null;
                    return true;
                }
            }

            if (IsGeneralRegistryQueryAllowed(request.Arguments, out reason))
            {
                return true;
            }

            if (_allowUnsafeDeveloperCommands && IsGeneralRegistryMutationAllowed(request.Arguments, out reason))
            {
                return true;
            }

            reason = _allowUnsafeDeveloperCommands
                ? "Registry arguments are not allowlisted."
                : "Registry mutations require explicit allowlisting.";
            return false;
        }

        if (Path.GetFileName(normalizedExecutable).Equals("powershell.exe", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var args in allowedArguments)
            {
                if (ArgumentsMatch(args, request.Arguments))
                {
                    reason = null;
                    return true;
                }
            }

            if (IsKnownPowerShellCommandAllowed(request.Arguments, out reason))
            {
                return true;
            }

            reason ??= "Arguments are not allowlisted.";
            return false;
        }

        if (Path.GetFileName(normalizedExecutable).Equals("powercfg.exe", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var args in allowedArguments)
            {
                if (ArgumentsMatch(args, request.Arguments))
                {
                    reason = null;
                    return true;
                }
            }

            if (IsKnownPowerCfgCommandAllowed(request.Arguments, out reason))
            {
                return true;
            }

            reason ??= "Arguments are not allowlisted.";
            return false;
        }

        foreach (var args in allowedArguments)
        {
            if (ArgumentsMatch(args, request.Arguments))
            {
                reason = null;
                return true;
            }
        }

        reason = "Arguments are not allowlisted.";
        return false;
    }

    private static bool IsGeneralRegistryQueryAllowed(IReadOnlyList<string> arguments, out string? reason)
    {
        if (arguments.Count < 2)
        {
            reason = "Registry command arguments are incomplete.";
            return false;
        }

        var operation = arguments[0];
        if (!operation.Equals("query", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Only reg query is generally allowed.";
            return false;
        }

        if (!IsRegistryHivePath(arguments[1]))
        {
            reason = "Registry path must target a supported hive.";
            return false;
        }

        reason = null;
        return true;
    }

    private static bool IsKnownPowerShellCommandAllowed(IReadOnlyList<string> arguments, out string? reason)
    {
        if (arguments.Count != 4
            || !arguments[0].Equals("-NoProfile", StringComparison.OrdinalIgnoreCase)
            || !arguments[1].Equals("-NonInteractive", StringComparison.OrdinalIgnoreCase)
            || !arguments[2].Equals("-Command", StringComparison.OrdinalIgnoreCase))
        {
            reason = "PowerShell arguments are not allowlisted.";
            return false;
        }

        var script = arguments[3];
        if (string.IsNullOrWhiteSpace(script))
        {
            reason = "PowerShell command text is required.";
            return false;
        }

        if (script.Equals(
                "$items = @(Get-CimInstance Win32_NetworkAdapterConfiguration | Where-Object { $_.IPEnabled -eq $true } | Sort-Object Index | ForEach-Object {   [pscustomobject]@{     Index = [int]$_.Index;     SettingID = [string]$_.SettingID;     Description = [string]$_.Description;     TcpipNetbiosOptions = if ($null -eq $_.TcpipNetbiosOptions) { -1 } else { [int]$_.TcpipNetbiosOptions }   } }); $items | ConvertTo-Json -Compress -Depth 3",
                StringComparison.Ordinal)
            || script.Equals(
                "$configs = @(Get-CimInstance Win32_NetworkAdapterConfiguration | Where-Object { $_.IPEnabled -eq $true }); foreach ($cfg in $configs) {   $result = Invoke-CimMethod -InputObject $cfg -MethodName SetTcpipNetbios -Arguments @{ TcpipNetbiosOptions = [uint32]2 };   if ($result.ReturnValue -ne 0 -and $result.ReturnValue -ne 1) { throw \"SetTcpipNetbios failed for adapter $($cfg.Index) with return code $($result.ReturnValue).\" } } Write-Output (\"Updated NetBIOS over TCP/IP on {0} adapters.\" -f $configs.Count)",
                StringComparison.Ordinal))
        {
            reason = null;
            return true;
        }

        if (script.StartsWith("$json = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('", StringComparison.Ordinal)
            && script.Contains("$items = @($json | ConvertFrom-Json);", StringComparison.Ordinal)
            && script.Contains("Get-CimInstance Win32_NetworkAdapterConfiguration | Where-Object { $_.SettingID -eq $item.SettingID };", StringComparison.Ordinal)
            && script.Contains("Invoke-CimMethod -InputObject $cfg -MethodName SetTcpipNetbios -Arguments @{ TcpipNetbiosOptions = [uint32]$item.TcpipNetbiosOptions };", StringComparison.Ordinal)
            && script.Contains("Write-Output (\"Restored NetBIOS over TCP/IP state on {0} adapters.\" -f $items.Count)", StringComparison.Ordinal))
        {
            reason = null;
            return true;
        }

        if (script.StartsWith("$currentExportPath = ", StringComparison.Ordinal)
            && script.Contains("Get-ProcessMitigation -RegistryConfigFilePath $currentExportPath | Out-Null;", StringComparison.Ordinal)
            && script.Contains("[pscustomobject]@{ BackupPath = $currentExportPath; MatchesDesired = $matches } | ConvertTo-Json -Compress", StringComparison.Ordinal))
        {
            reason = null;
            return true;
        }

        if (script.StartsWith("$policyPath = ", StringComparison.Ordinal)
            && script.Contains("Set-ProcessMitigation -PolicyFilePath $policyPath | Out-Null;", StringComparison.Ordinal)
            && script.Contains("Write-Output 'Imported exploit protection XML.'", StringComparison.Ordinal))
        {
            reason = null;
            return true;
        }

        if (script.StartsWith("$backupPath = ", StringComparison.Ordinal)
            && script.Contains("$policyPath = ", StringComparison.Ordinal)
            && script.Contains("Set-ProcessMitigation -PolicyFilePath $backupPath | Out-Null;", StringComparison.Ordinal)
            && script.Contains("Write-Output 'Restored exploit protection XML.'", StringComparison.Ordinal))
        {
            reason = null;
            return true;
        }

        reason = "PowerShell arguments are not allowlisted.";
        return false;
    }

    private static bool IsKnownPowerCfgCommandAllowed(IReadOnlyList<string> arguments, out string? reason)
    {
        if (arguments.Count == 4
            && arguments[0].Equals("/qh", StringComparison.OrdinalIgnoreCase)
            && arguments[1].Equals("SCHEME_CURRENT", StringComparison.OrdinalIgnoreCase)
            && arguments[2].Equals("SUB_PROCESSOR", StringComparison.OrdinalIgnoreCase)
            && arguments[3].Equals("PERFBOOSTMODE", StringComparison.OrdinalIgnoreCase))
        {
            reason = null;
            return true;
        }

        if (arguments.Count == 12
            && arguments[0].Equals("/setacvalueindex", StringComparison.OrdinalIgnoreCase)
            && arguments[1].Equals("SCHEME_CURRENT", StringComparison.OrdinalIgnoreCase)
            && arguments[2].Equals("SUB_PROCESSOR", StringComparison.OrdinalIgnoreCase)
            && arguments[3].Equals("PERFBOOSTMODE", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(arguments[4], out _)
            && arguments[5].Equals("/setdcvalueindex", StringComparison.OrdinalIgnoreCase)
            && arguments[6].Equals("SCHEME_CURRENT", StringComparison.OrdinalIgnoreCase)
            && arguments[7].Equals("SUB_PROCESSOR", StringComparison.OrdinalIgnoreCase)
            && arguments[8].Equals("PERFBOOSTMODE", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(arguments[9], out _)
            && arguments[10].Equals("/setactive", StringComparison.OrdinalIgnoreCase)
            && arguments[11].Equals("SCHEME_CURRENT", StringComparison.OrdinalIgnoreCase))
        {
            reason = null;
            return true;
        }

        if (arguments.Count == 5
            && (arguments[0].Equals("/setacvalueindex", StringComparison.OrdinalIgnoreCase)
                || arguments[0].Equals("/setdcvalueindex", StringComparison.OrdinalIgnoreCase))
            && arguments[1].Equals("SCHEME_CURRENT", StringComparison.OrdinalIgnoreCase)
            && arguments[2].Equals("SUB_PROCESSOR", StringComparison.OrdinalIgnoreCase)
            && arguments[3].Equals("PERFBOOSTMODE", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(arguments[4], out _))
        {
            reason = null;
            return true;
        }

        if (arguments.Count == 2
            && arguments[0].Equals("/setactive", StringComparison.OrdinalIgnoreCase)
            && arguments[1].Equals("SCHEME_CURRENT", StringComparison.OrdinalIgnoreCase))
        {
            reason = null;
            return true;
        }

        reason = "powercfg arguments are not allowlisted.";
        return false;
    }

    private static bool IsGeneralRegistryMutationAllowed(IReadOnlyList<string> arguments, out string? reason)
    {
        if (arguments.Count < 2)
        {
            reason = "Registry command arguments are incomplete.";
            return false;
        }

        var operation = arguments[0];
        if (!operation.Equals("add", StringComparison.OrdinalIgnoreCase)
            && !operation.Equals("delete", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Only reg add/delete mutations are allowed in developer mode.";
            return false;
        }

        if (!IsRegistryHivePath(arguments[1]))
        {
            reason = "Registry path must target a supported hive.";
            return false;
        }

        reason = null;
        return true;
    }

    private static bool IsRegistryHivePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return path.StartsWith(@"HKLM\", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(@"HKCU\", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(@"HKCR\", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(@"HKU\", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(@"HKCC\", StringComparison.OrdinalIgnoreCase)
            || path.Equals("HKLM", StringComparison.OrdinalIgnoreCase)
            || path.Equals("HKCU", StringComparison.OrdinalIgnoreCase)
            || path.Equals("HKCR", StringComparison.OrdinalIgnoreCase)
            || path.Equals("HKU", StringComparison.OrdinalIgnoreCase)
            || path.Equals("HKCC", StringComparison.OrdinalIgnoreCase);
    }

    private string? NormalizeExecutable(string executable)
    {
        if (!Path.IsPathFullyQualified(executable))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(executable);
        var directory = Path.GetDirectoryName(fullPath);
        if (directory is null)
        {
            return null;
        }

        var relativeDirectory = Path.GetRelativePath(_systemDirectory, directory);
        if (relativeDirectory.StartsWith("..", StringComparison.OrdinalIgnoreCase)
            || Path.IsPathRooted(relativeDirectory))
        {
            return null;
        }

        return fullPath;
    }

    private static bool ArgumentsMatch(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        if (expected.Count != actual.Count)
        {
            return false;
        }

        for (var i = 0; i < expected.Count; i++)
        {
            if (!string.Equals(expected[i], actual[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
