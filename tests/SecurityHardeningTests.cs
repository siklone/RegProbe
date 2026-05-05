using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using RegProbe.Core.Commands;
using RegProbe.Infrastructure.Commands;
using RegProbe.Infrastructure.Elevation;
using RegProbe.Infrastructure.Registry;
using RegProbe.Core.Registry;
using Xunit;
using CorePluginLoader = RegProbe.Core.Plugins.PluginLoader;
using InfrastructurePluginLoader = RegProbe.Infrastructure.Services.PluginLoader;

public sealed class PipeMessageSerializerTests
{
    [Fact]
    public async Task WriteAsync_RejectsPayloadsOverOneMegabyte()
    {
        var stream = new MemoryStream();
        var payload = new OversizedPipeMessage(new string('a', PipeMessageSerializer.MaxMessageBytes + 1));

        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => PipeMessageSerializer.WriteAsync(stream, payload, CancellationToken.None));

        Assert.Contains("maximum size", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadAsync_RejectsDeclaredPayloadsOverOneMegabyte()
    {
        var stream = new MemoryStream();
        var lengthBytes = BitConverter.GetBytes(PipeMessageSerializer.MaxMessageBytes + 1);
        await stream.WriteAsync(lengthBytes);
        stream.Position = 0;

        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => PipeMessageSerializer.ReadAsync<OversizedPipeMessage>(stream, CancellationToken.None));

        Assert.Contains("maximum size", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record OversizedPipeMessage(string Data);
}

public sealed class CommandAllowlistSecurityTests
{
    [Fact]
    public void RegQuery_IsAllowedForSupportedHives()
    {
        var allowlist = CommandAllowlist.CreateDefault();
        var request = CreateRegRequest("query", @"HKLM\SOFTWARE\RegProbe");

        var allowed = allowlist.IsAllowed(request, out var reason);

        Assert.True(allowed);
        Assert.Null(reason);
    }

    [Fact]
    public void RegMutation_IsRejectedWhenNotExplicitlyAllowlisted()
    {
        var allowlist = CommandAllowlist.CreateDefault();
        var request = CreateRegRequest("add", @"HKLM\SOFTWARE\RegProbe", "/v", "TestValue", "/t", "REG_DWORD", "/d", "1", "/f");

        var allowed = allowlist.IsAllowed(request, out var reason);

        Assert.False(allowed);
        Assert.Contains("explicit allowlisting", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegMutation_IsAllowedInDeveloperMode()
    {
        var allowlist = CommandAllowlist.CreateDefault(allowUnsafeDeveloperCommands: true);
        var request = CreateRegRequest("add", @"HKLM\SOFTWARE\RegProbe", "/v", "TestValue", "/t", "REG_DWORD", "/d", "1", "/f");

        var allowed = allowlist.IsAllowed(request, out var reason);

        Assert.True(allowed);
        Assert.Null(reason);
    }

    [Fact]
    public void ExplicitRegAllowlistStillPermitsKnownSafeMutation()
    {
        var allowlist = CommandAllowlist.CreateDefault();
        var request = CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "/v", "DoNotUseWebResults", "/t", "REG_DWORD", "/d", "1", "/f");

        var allowed = allowlist.IsAllowed(request, out var reason);

        Assert.True(allowed);
        Assert.Null(reason);
    }

    [Fact]
    public void ClipboardPolicyMutations_AreAllowlisted()
    {
        var allowlist = CommandAllowlist.CreateDefault();
        var historyRequest = CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "/v", "AllowClipboardHistory", "/t", "REG_DWORD", "/d", "0", "/f");
        var crossDeviceRequest = CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "/v", "AllowCrossDeviceClipboard", "/t", "REG_DWORD", "/d", "0", "/f");

        var historyAllowed = allowlist.IsAllowed(historyRequest, out var historyReason);
        var crossDeviceAllowed = allowlist.IsAllowed(crossDeviceRequest, out var crossDeviceReason);

        Assert.True(historyAllowed);
        Assert.Null(historyReason);
        Assert.True(crossDeviceAllowed);
        Assert.Null(crossDeviceReason);
    }

    [Fact]
    public void OfflineFilesPolicyMutations_AreAllowlisted()
    {
        var allowlist = CommandAllowlist.CreateDefault();
        var request = CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\NetCache", "/v", "Enabled", "/t", "REG_DWORD", "/d", "0", "/f");

        var allowed = allowlist.IsAllowed(request, out var reason);

        Assert.True(allowed);
        Assert.Null(reason);
    }

    [Fact]
    public void AdditionalPolicyAndSmbRegistryMutations_AreAllowlisted()
    {
        var allowlist = CommandAllowlist.CreateDefault();
        var requests = new[]
        {
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsGetDiagnosticInfo", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "/v", "NoConnectedUser", "/t", "REG_DWORD", "/d", "3", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\FileSystem", "/v", "LongPathsEnabled", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo", "/v", "DisabledByGroupPolicy", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "/v", "AllowDeviceNameInTelemetry", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "/v", "DisableDeviceDelete", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "/v", "DisableDiagnosticDataViewer", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "/v", "DoNotShowFeedbackNotifications", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "/v", "DisableOneSettingsDownloads", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "/v", "LimitDiagnosticLogCollection", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "/v", "LimitDumpCollection", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "/v", "AllowTelemetry", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent", "/v", "DisableSoftLanding", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "/v", "VerboseStatus", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location", "/v", "Value", "/t", "REG_SZ", "/d", "Deny", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock", "/v", "AllowDevelopmentWithoutDevLicense", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "/v", "EnableFontProviders", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "/v", "EnableMmx", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "/v", "NoLocalPasswordResetQuestions", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "/v", "DisableSettingSync", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "/v", "DisableSettingSyncUserOverride", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\SettingSync", "/v", "DisableSyncOnPaidNetwork", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Dsh", "/v", "AllowNewsAndInterests", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DWM", "/v", "DisallowAnimations", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Appx", "/v", "AllowAutomaticAppArchiving", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppCompat", "/v", "AITEnable", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppCompat", "/v", "DisableEngine", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppCompat", "/v", "DisablePcaUI", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppCompat", "/v", "SbEnable", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppCompat", "/v", "DisableUAR", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppCompat", "/v", "DisableAPISamping", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppCompat", "/v", "DisableApplicationFootprint", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppCompat", "/v", "DisableInstallTracing", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppCompat", "/v", "DisableWin32AppBackup", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\StorageSense", "/v", "AllowStorageSenseGlobal", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\StorageSense", "/v", "AllowStorageSenseTemporaryFilesCleanup", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\CredUI", "/v", "DisablePasswordReveal", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessAccountInfo", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessBackgroundSpatialPerception", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessCalendar", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessCallHistory", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessCamera", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessContacts", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessEmail", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessGazeInput", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessGraphicsCaptureProgrammatic", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessGraphicsCaptureWithoutBorder", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessHumanPresence", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessLocation", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessMessaging", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessMicrophone", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessMotion", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessNotifications", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessPhone", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessRadios", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessTasks", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsAccessTrustedDevices", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsActivateWithVoice", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsActivateWithVoiceAboveLock", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsRunInBackground", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "/v", "LetAppsSyncWithDevices", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\FileHistory", "/v", "Disabled", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppCompat", "/v", "DisablePCA", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "/v", "DisableLocation", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "/v", "DisableLocationScripting", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "/v", "DisableSensors", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "/v", "DisableWindowsLocationProvider", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Messaging", "/v", "AllowMessageSync", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\MDM", "/v", "DisableRegistration", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\NetworkConnectivityStatusIndicator", "/v", "NoActiveProbe", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DNSClient", "/v", "EnableMDNS", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LLTD", "/v", "EnableLLTDIO", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LLTD", "/v", "EnableRspndr", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "/v", "DisableAcrylicBackgroundOnLogon", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "/v", "RSoPLogging", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "/v", "EnableFirstLogonAnimation", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Personalization", "/v", "NoLockScreenCamera", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Personalization", "/v", "NoChangingLockScreen", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Personalization", "/v", "AnimateLockScreenBackground", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Personalization", "/v", "NoLockScreen", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Personalization", "/v", "NoLockScreenSlideshow", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Troubleshooting\AllowRecommendations", "/v", "TroubleshootingAllowRecommendations", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WCN\UI", "/v", "DisableWcnUi", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance", "/v", "MaintenanceDisabled", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "/v", "SystemResponsiveness", "/t", "REG_DWORD", "/d", "10", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "/v", "Affinity", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "/v", "GPU Priority", "/t", "REG_DWORD", "/d", "8", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "/v", "Priority", "/t", "REG_DWORD", "/d", "8", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "/v", "Scheduling Category", "/t", "REG_SZ", "/d", "High", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "/v", "SFIO Priority", "/t", "REG_SZ", "/d", "High", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "/v", "PowerThrottlingOff", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\CrashControl", "/v", "AutoReboot", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\CrashControl", "/v", "DisplayParameters", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power", "/v", "HiberbootEnabled", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "/v", "DisableTaskOffload", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services", "/v", "fAllowToGetHelp", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "/v", "DisableBkGndGroupPolicy", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "/v", "ConnectedSearchUseWeb", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "/v", "AllowIndexingEncryptedStoresOrItems", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "/v", "EnableDynamicContentInWSB", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "/v", "PreventRemoteQueries", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "/v", "Disabled", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "/v", "NoLowDiskSpaceChecks", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "/v", "UseDefaultTile", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "/v", "AllowOnlineTips", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "/v", "DontDisplayLastUserName", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "/v", "DontDisplayUserName", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Explorer", "/v", "NoUseStoreOpenWith", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Explorer", "/v", "ShowHibernateOption", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Explorer", "/v", "ShowLockOption", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Explorer", "/v", "ShowSleepOption", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Explorer", "/v", "ShowOrHideMostUsedApps", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR", "/v", "AllowGameDVR", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LanmanWorkstation", "/v", "EnableSMBQUIC", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LanmanServer", "/v", "EnableSMBQUIC", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LanmanWorkstation", "/v", "MinSmb2Dialect", "/t", "REG_DWORD", "/d", "785", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LanmanWorkstation", "/v", "MaxSmb2Dialect", "/t", "REG_DWORD", "/d", "785", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LanmanServer", "/v", "MinSmb2Dialect", "/t", "REG_DWORD", "/d", "785", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LanmanServer", "/v", "MaxSmb2Dialect", "/t", "REG_DWORD", "/d", "785", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "/v", "EnableCdp", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "/v", "BlockDomainPicturePassword", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform", "/v", "NoGenTicket", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Services\Beep", "/v", "Start", "/t", "REG_DWORD", "/d", "4", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons", "/v", "29", "/t", "REG_SZ", "/d", @"%windir%\System32\shell32.dll,-50", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "/v", "NODE_OPTIONS", "/t", "REG_SZ", "/d", "--max-old-space-size=8192", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\PowerShell", "/v", "ExecutionPolicy", "/t", "REG_SZ", "/d", "RemoteSigned", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "/v", "AutoShareServer", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "/v", "AutoShareWks", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "/v", "SMB1", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "/v", "SMB2", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters", "/v", "EnablePlainTextPassword", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters", "/v", "EnableSecuritySignature", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters", "/v", "RequireSecuritySignature", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters", "/v", "DirectoryCacheEntriesMax", "/t", "REG_DWORD", "/d", "4096", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters", "/v", "FileInfoCacheEntriesMax", "/t", "REG_DWORD", "/d", "32768", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters", "/v", "FileNotFoundCacheEntriesMax", "/t", "REG_DWORD", "/d", "32768", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters", "/v", "MaxCmds", "/t", "REG_DWORD", "/d", "32768", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "/v", "EnableSecuritySignature", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "/v", "RequireSecuritySignature", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "/v", "HwSchMode", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "/v", "TdrDdiDelay", "/t", "REG_DWORD", "/d", "5", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "/v", "TdrDelay", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "/v", "TdrLevel", "/t", "REG_DWORD", "/d", "3", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "/v", "TdrLimitCount", "/t", "REG_DWORD", "/d", "5", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "/v", "TdrLimitTime", "/t", "REG_DWORD", "/d", "60", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "/v", "ClearPageFileAtShutdown", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SOFTWARE\Microsoft\Windows\Dwm", "/v", "OverlayMinFPS", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control", "/v", "RegistrySizeLimit", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\FileSystem", "/v", "NtfsDisable8dot3NameCreation", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\FileSystem", "/v", "NtfsDisableLastAccessUpdate", "/t", "REG_DWORD", "/d", "-2147483646", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\FileSystem", "/v", "NtfsMftZoneReservation", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\FileSystem", "/v", "NtfsMemoryUsage", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "/v", "DisablePagingExecutive", "/t", "REG_DWORD", "/d", "1", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "/v", "LargeSystemCache", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "/v", "NonPagedPoolSize", "/t", "REG_DWORD", "/d", "0", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "/v", "PagedPoolSize", "/t", "REG_DWORD", "/d", "0", "/f"),
        };

        foreach (var request in requests)
        {
            var allowed = allowlist.IsAllowed(request, out var reason);

            Assert.True(allowed);
            Assert.Null(reason);
        }
    }

    [Fact]
    public void PowerShellUnderSystem32Subdirectory_IsAllowedWhenArgumentsMatch()
    {
        var allowlist = CommandAllowlist.CreateDefault();
        var request = new CommandRequest(
            Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe"),
            new[]
            {
                "-NoProfile",
                "-NonInteractive",
                "-Command",
                "Set-Clipboard",
                "-Value",
                "$null"
            });

        var allowed = allowlist.IsAllowed(request, out var reason);

        Assert.True(allowed);
        Assert.Null(reason);
    }

    [Fact]
    public void DefenderSampleSubmissionAndIpv6OverrideMutations_AreAllowlisted()
    {
        var allowlist = CommandAllowlist.CreateDefault();
        var requests = new[]
        {
            CreateRegRequest("add", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet", "/v", "SubmitSamplesConsent", "/t", "REG_DWORD", "/d", "2", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", "/v", "DisabledComponents", "/t", "REG_DWORD", "/d", "255", "/f"),
            CreateRegRequest("add", @"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", "/v", "DisabledComponents", "/t", "REG_DWORD", "/d", "32", "/f")
        };

        foreach (var request in requests)
        {
            var allowed = allowlist.IsAllowed(request, out var reason);

            Assert.True(allowed);
            Assert.Null(reason);
        }
    }

    [Fact]
    public void DnsClientEnableMulticastMutation_IsAllowlisted()
    {
        var allowlist = CommandAllowlist.CreateDefault();
        var request = CreateRegRequest(
            "add",
            @"HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DNSClient",
            "/v",
            "EnableMulticast",
            "/t",
            "REG_DWORD",
            "/d",
            "0",
            "/f");

        var allowed = allowlist.IsAllowed(request, out var reason);

        Assert.True(allowed);
        Assert.Null(reason);
    }

    [Fact]
    public void PerfBoostPowerCfgCommands_AreAllowlisted()
    {
        var allowlist = CommandAllowlist.CreateDefault();
        var executable = Path.Combine(Environment.SystemDirectory, "powercfg.exe");
        var detectRequest = new CommandRequest(
            executable,
            new[]
            {
                "/qh",
                "SCHEME_CURRENT",
                "SUB_PROCESSOR",
                "PERFBOOSTMODE"
            });
        var setAcRequest = new CommandRequest(
            executable,
            new[]
            {
                "/setacvalueindex",
                "SCHEME_CURRENT",
                "SUB_PROCESSOR",
                "PERFBOOSTMODE",
                "2"
            });
        var setDcRequest = new CommandRequest(
            executable,
            new[]
            {
                "/setdcvalueindex",
                "SCHEME_CURRENT",
                "SUB_PROCESSOR",
                "PERFBOOSTMODE",
                "4"
            });
        var setActiveRequest = new CommandRequest(
            executable,
            new[]
            {
                "/setactive",
                "SCHEME_CURRENT"
            });
        var combinedRequest = new CommandRequest(
            executable,
            new[]
            {
                "/setacvalueindex",
                "SCHEME_CURRENT",
                "SUB_PROCESSOR",
                "PERFBOOSTMODE",
                "0",
                "/setdcvalueindex",
                "SCHEME_CURRENT"
            });

        var detectAllowed = allowlist.IsAllowed(detectRequest, out var detectReason);
        var setAcAllowed = allowlist.IsAllowed(setAcRequest, out var setAcReason);
        var setDcAllowed = allowlist.IsAllowed(setDcRequest, out var setDcReason);
        var setActiveAllowed = allowlist.IsAllowed(setActiveRequest, out var setActiveReason);
        var combinedAllowed = allowlist.IsAllowed(combinedRequest, out var combinedReason);

        Assert.True(detectAllowed);
        Assert.Null(detectReason);
        Assert.True(setAcAllowed);
        Assert.Null(setAcReason);
        Assert.True(setDcAllowed);
        Assert.Null(setDcReason);
        Assert.True(setActiveAllowed);
        Assert.Null(setActiveReason);
        Assert.False(combinedAllowed);
        Assert.Contains("not allowlisted", combinedReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SmbLeasingPowerShellCommands_AreAllowlisted()
    {
        var allowlist = CommandAllowlist.CreateDefault();
        var executable = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
        var detectRequest = new CommandRequest(
            executable,
            new[]
            {
                "-NoProfile",
                "-NonInteractive",
                "-Command",
                "$value = (Get-SmbServerConfiguration).EnableLeasing; if ($value) { Write-Output 'True' } else { Write-Output 'False' }"
            });
        var applyRequest = new CommandRequest(
            executable,
            new[]
            {
                "-NoProfile",
                "-NonInteractive",
                "-Command",
                "Set-SmbServerConfiguration -EnableLeasing $false -Force | Out-Null; Write-Output 'EnableLeasing=False'"
            });

        var detectAllowed = allowlist.IsAllowed(detectRequest, out var detectReason);
        var applyAllowed = allowlist.IsAllowed(applyRequest, out var applyReason);

        Assert.True(detectAllowed);
        Assert.Null(detectReason);
        Assert.True(applyAllowed);
        Assert.Null(applyReason);
    }

    [Fact]
    public void NetbiosRollbackPowerShellCommand_IsAllowlisted()
    {
        var allowlist = CommandAllowlist.CreateDefault();
        var executable = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
        var request = new CommandRequest(
            executable,
            new[]
            {
                "-NoProfile",
                "-NonInteractive",
                "-Command",
                "$json = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('W10=')); $items = @($json | ConvertFrom-Json); foreach ($item in $items) {   $cfg = Get-CimInstance Win32_NetworkAdapterConfiguration | Where-Object { $_.SettingID -eq $item.SettingID };   if ($null -eq $cfg) { continue }   $result = Invoke-CimMethod -InputObject $cfg -MethodName SetTcpipNetbios -Arguments @{ TcpipNetbiosOptions = [uint32]$item.TcpipNetbiosOptions };   if ($result.ReturnValue -ne 0 -and $result.ReturnValue -ne 1) { throw \"SetTcpipNetbios restore failed for adapter $($item.Index) with return code $($result.ReturnValue).\" } } Write-Output (\"Restored NetBIOS over TCP/IP state on {0} adapters.\" -f $items.Count)"
            });

        var allowed = allowlist.IsAllowed(request, out var reason);

        Assert.True(allowed);
        Assert.Null(reason);
    }

    private static CommandRequest CreateRegRequest(params string[] arguments)
    {
        return new CommandRequest(Path.Combine(Environment.SystemDirectory, "reg.exe"), arguments);
    }
}

public sealed class PluginLoaderSecurityTests : IDisposable
{
    private readonly string _pluginDirectory;

    public PluginLoaderSecurityTests()
    {
        _pluginDirectory = Path.Combine(Path.GetTempPath(), $"RegProbePluginSecurity_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_pluginDirectory);
        File.WriteAllText(Path.Combine(_pluginDirectory, "Example.dll"), "not a real dll");
    }

    [Fact]
    public async Task CorePluginLoader_DoesNotDiscoverPluginsWhileDynamicLoadingIsDisabled()
    {
        var loader = new CorePluginLoader(_pluginDirectory);

        var plugins = await loader.DiscoverPluginsAsync(CancellationToken.None);

        Assert.Empty(plugins);
    }

    [Fact]
    public void InfrastructurePluginLoader_DoesNotLoadPluginsWhileDynamicLoadingIsDisabled()
    {
        var loader = new InfrastructurePluginLoader();

        var plugins = loader.LoadPlugins(_pluginDirectory);

        Assert.Empty(plugins);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_pluginDirectory))
            {
                Directory.Delete(_pluginDirectory, true);
            }
        }
        catch
        {
        }
    }
}

public sealed class ElevatedHostSessionSecurityTests
{
    [Fact]
    public void SessionTokenAndPipeNameIncludeNonce()
    {
        var token = ElevatedHostDefaults.CreateSessionToken();
        var pipeName = ElevatedHostDefaults.GetPipeNameForProcess(4242, token);

        Assert.Matches("^[A-F0-9]{32}$", token);
        Assert.Contains(".4242.", pipeName, StringComparison.Ordinal);
        Assert.EndsWith(ElevatedHostSessionSecurity.BuildPipeNonceSuffix(token), pipeName, StringComparison.Ordinal);
    }

    [Fact]
    public void SessionTokenValidationRequiresExactMatch()
    {
        var token = ElevatedHostDefaults.CreateSessionToken();
        var last = token[^1];
        var altered = token[..^1] + (last == '0' ? "1" : "0");

        Assert.True(ElevatedHostSessionSecurity.IsSessionTokenAccepted(token, token));
        Assert.False(ElevatedHostSessionSecurity.IsSessionTokenAccepted(token, altered));
    }

    [Fact]
    public void RedactSensitiveText_MasksSessionTokens()
    {
        var redacted = ElevatedHostSessionSecurity.RedactSensitiveText("--pipe \"regprobe.pipe.1234.ABCDEF\" --session-token \"ABCDEF123456\" token=secret");

        Assert.DoesNotContain("regprobe.pipe.1234.ABCDEF", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("ABCDEF123456", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", redacted, StringComparison.Ordinal);
        Assert.Contains("<redacted>", redacted, StringComparison.Ordinal);
    }

    [Fact]
    public void RedactSensitiveText_MasksUnquotedAndRepeatedTokens()
    {
        var redacted = ElevatedHostSessionSecurity.RedactSensitiveText(
            "--session-token ABCDEF123456 --session-token 'SECOND' SessionToken = \"THIRD\" token = fourth");

        Assert.DoesNotContain("ABCDEF123456", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("SECOND", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("THIRD", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("fourth", redacted, StringComparison.Ordinal);
        Assert.Contains("--session-token <redacted>", redacted, StringComparison.Ordinal);
        Assert.Contains("--session-token '<redacted>'", redacted, StringComparison.Ordinal);
        Assert.Contains("SessionToken = \"<redacted>\"", redacted, StringComparison.Ordinal);
        Assert.Contains("token = <redacted>", redacted, StringComparison.Ordinal);
    }

    [Fact]
    public void RedactSensitiveText_MasksPipeBootstrapValues()
    {
        var redacted = ElevatedHostSessionSecurity.RedactSensitiveText(
            "--pipe RegProbe.ElevatedHost.4242.ABCDEF pipeName = \"RegProbe.ElevatedHost.4242.SECOND\"");

        Assert.DoesNotContain("RegProbe.ElevatedHost.4242.ABCDEF", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("RegProbe.ElevatedHost.4242.SECOND", redacted, StringComparison.Ordinal);
        Assert.Contains("--pipe <redacted>", redacted, StringComparison.Ordinal);
        Assert.Contains("pipeName = \"<redacted>\"", redacted, StringComparison.Ordinal);
    }

    [Fact]
    public void RedactSensitiveText_DoesNotMaskUnrelatedTokenWords()
    {
        var original = "--tokenizer enabled --api-token off --pipeline value --session-tokenized false";

        var redacted = ElevatedHostSessionSecurity.RedactSensitiveText(original);

        Assert.Equal(original, redacted);
    }

    [Fact]
    public void ClientProcessValidationRejectsUnexpectedPid()
    {
        Assert.True(ElevatedHostSessionSecurity.IsClientProcessAccepted(1234, 1234));
        Assert.False(ElevatedHostSessionSecurity.IsClientProcessAccepted(1234, 9876));
    }
}

public sealed class RegistryOwnershipMutationGuardTests
{
    [Fact]
    public void Execute_RollsBackWhenGrantAccessFails()
    {
        var rollbackCalled = false;

        Assert.Throws<InvalidOperationException>(() =>
            RegistryOwnershipMutationGuard.Execute(
                applyOwnership: () => { },
                grantAccess: () => throw new InvalidOperationException("boom"),
                rollback: () => rollbackCalled = true));

        Assert.True(rollbackCalled);
    }
}

public sealed class ElevatedRegistryAccessorSecurityTests
{
    [Fact]
    public async Task SetValueAsync_FallsBackToRegExe_OnAccessDeniedHResult()
    {
        var client = new RecordingClient
        {
            RegistryResponseFactory = request => new ElevatedRegistryResponse(
                request.RequestId,
                false,
                "Access denied",
                null,
                unchecked((int)0x80070005)),
            CommandResponseFactory = request => new ElevatedCommandResponse(
                request.RequestId,
                true,
                null,
                new CommandResult(0, string.Empty, string.Empty, false, TimeSpan.Zero))
        };
        var accessor = new ElevatedRegistryAccessor(client);
        var reference = new RegistryValueReference(
            RegistryHive.CurrentUser,
            RegistryView.Default,
            @"Software\RegProbe",
            "TestValue");

        await accessor.SetValueAsync(
            reference,
            new RegistryValueData(RegistryValueKind.DWord, NumericValue: 1),
            CancellationToken.None);

        Assert.Equal(2, client.Requests.Count);
        Assert.Equal(ElevatedHostRequestType.Command, client.Requests[1].RequestType);
    }

    private sealed class RecordingClient : IElevatedHostClient
    {
        public List<ElevatedHostRequest> Requests { get; } = new();
        public Func<ElevatedRegistryRequest, ElevatedRegistryResponse>? RegistryResponseFactory { get; set; }
        public Func<ElevatedCommandRequest, ElevatedCommandResponse>? CommandResponseFactory { get; set; }

        public Task<ElevatedHostResponse> SendAsync(ElevatedHostRequest request, CancellationToken ct)
        {
            Requests.Add(request);

            if (request.RequestType == ElevatedHostRequestType.Registry)
            {
                var registryRequest = request.RegistryRequest!;
                var response = RegistryResponseFactory!(registryRequest);
                return Task.FromResult(new ElevatedHostResponse(request.RequestId, request.RequestType, RegistryResponse: response));
            }

            if (request.RequestType == ElevatedHostRequestType.Command)
            {
                var commandRequest = request.CommandRequest!;
                var response = CommandResponseFactory!(commandRequest);
                return Task.FromResult(new ElevatedHostResponse(request.RequestId, request.RequestType, CommandResponse: response));
            }

            throw new InvalidOperationException("Unexpected request type.");
        }
    }
}
