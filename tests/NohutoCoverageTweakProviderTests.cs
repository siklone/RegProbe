using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Microsoft.Win32;
using RegProbe.Application.Services.TweakProviders;
using RegProbe.Core;
using RegProbe.Core.Commands;
using RegProbe.Core.Files;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Core.Tasks;
using RegProbe.Engine;
using RegProbe.Engine.Tweaks;
using RegProbe.Engine.Tweaks.Commands.RegistryOps;
using Xunit;

namespace RegProbe.Tests;

public sealed class NohutoCoverageTweakProviderTests
{
    [Fact]
    public void PrivacyProvider_Exposes_New_Nohuto_Privacy_Settings()
    {
        var provider = new PrivacyTweakProvider();
        var tweaks = provider.CreateTweaks(default!, BuildContext(), false).ToList();

        var crossDevice = tweaks.Single(tweak => tweak.Id == "privacy.disable-cross-device-experiences");
        var findMyDevice = tweaks.Single(tweak => tweak.Id == "privacy.disable-find-my-device");

        Assert.IsType<RegistryValuePresetBatchTweak>(crossDevice);
        Assert.NotEqual("DisableFindMyDeviceTweak", findMyDevice.GetType().Name);
    }

    [Fact]
    public void PrivacyProvider_Uses_CommandBacked_Registry_Tweaks_Only_For_Special_Batch_Cases()
    {
        var provider = new PrivacyTweakProvider();
        var tweaks = provider.CreateTweaks(default!, BuildContext(), false).ToList();

        var commandBackedIds = new[]
        {
            "privacy.disable-ceip",
            "privacy.disable-edge-search-suggestions"
        };

        foreach (var id in commandBackedIds)
        {
            Assert.Equal("RegistryCommandBatchTweak", tweaks.Single(tweak => tweak.Id == id).GetType().Name);
        }
    }

    [Fact]
    public async Task PrivacyProvider_DiagnosticDataGate_IsNotApplicable_OnUnsupportedEdition()
    {
        var registry = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        registry
            .Setup(accessor => accessor.ReadValueAsync(
                It.Is<RegistryValueReference>(reference =>
                    reference.Hive == RegistryHive.LocalMachine
                    && reference.KeyPath == @"SOFTWARE\Microsoft\Windows NT\CurrentVersion"
                    && reference.ValueName == "EditionID"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegistryValueReadResult(
                true,
                new RegistryValueData(RegistryValueKind.String, StringValue: "Professional")));

        var provider = new PrivacyTweakProvider();
        var tweak = provider.CreateTweaks(default!, BuildContext(registry.Object), false)
            .Single(item => item.Id == "privacy.disable-diagnostic-data");

        var result = await tweak.ApplyAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.NotApplicable, result.Status);
        Assert.Contains("Current edition: Professional.", result.Message);
        registry.Verify(accessor => accessor.SetValueAsync(
            It.IsAny<RegistryValueReference>(),
            It.IsAny<RegistryValueData>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PrivacyProvider_DiagnosticDataGate_Applies_OnSupportedEdition()
    {
        var registry = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        registry
            .Setup(accessor => accessor.ReadValueAsync(
                It.Is<RegistryValueReference>(reference =>
                    reference.Hive == RegistryHive.LocalMachine
                    && reference.KeyPath == @"SOFTWARE\Microsoft\Windows NT\CurrentVersion"
                    && reference.ValueName == "EditionID"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegistryValueReadResult(
                true,
                new RegistryValueData(RegistryValueKind.String, StringValue: "Enterprise")));
        registry
            .Setup(accessor => accessor.ReadValueAsync(
                It.Is<RegistryValueReference>(reference =>
                    reference.Hive == RegistryHive.LocalMachine
                    && reference.KeyPath == @"Software\Policies\Microsoft\Windows\DataCollection"
                    && reference.ValueName == "AllowTelemetry"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegistryValueReadResult(false, null));
        registry
            .Setup(accessor => accessor.SetValueAsync(
                It.Is<RegistryValueReference>(reference =>
                    reference.Hive == RegistryHive.LocalMachine
                    && reference.KeyPath == @"Software\Policies\Microsoft\Windows\DataCollection"
                    && reference.ValueName == "AllowTelemetry"),
                It.Is<RegistryValueData>(value =>
                    value.Kind == RegistryValueKind.DWord
                    && value.NumericValue == 0),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var provider = new PrivacyTweakProvider();
        var tweak = provider.CreateTweaks(default!, BuildContext(registry.Object), false)
            .Single(item => item.Id == "privacy.disable-diagnostic-data");

        var result = await tweak.ApplyAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Applied, result.Status);
        registry.VerifyAll();
    }

    [Fact]
    public async Task PrivacyProvider_DiagnosticDataGate_Fails_WhenEditionCannotBeRead()
    {
        var registry = new Mock<IRegistryAccessor>(MockBehavior.Strict);
        registry
            .Setup(accessor => accessor.ReadValueAsync(
                It.Is<RegistryValueReference>(reference =>
                    reference.Hive == RegistryHive.LocalMachine
                    && reference.KeyPath == @"SOFTWARE\Microsoft\Windows NT\CurrentVersion"
                    && reference.ValueName == "EditionID"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegistryValueReadResult(false, null));

        var provider = new PrivacyTweakProvider();
        var tweak = provider.CreateTweaks(default!, BuildContext(registry.Object), false)
            .Single(item => item.Id == "privacy.disable-diagnostic-data");

        var result = await tweak.DetectAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Failed, result.Status);
        Assert.Contains("Unable to determine the Windows edition", result.Message);
        registry.Verify(accessor => accessor.SetValueAsync(
            It.IsAny<RegistryValueReference>(),
            It.IsAny<RegistryValueData>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void SystemRegistryProvider_Exposes_New_Nohuto_Defaults()
    {
        var provider = new SystemRegistryTweakProvider();
        var tweaks = provider.CreateTweaks(default!, BuildContext(), false).ToList();

        Assert.Contains(tweaks, tweak => tweak.Id == "system.kernel-cache-aware-scheduling");
        Assert.Contains(tweaks, tweak => tweak.Id == "system.kernel-default-dynamic-hetero-cpu-policy");
        Assert.Contains(tweaks, tweak => tweak.Id == "system.memory-large-system-cache-client");
        Assert.Contains(tweaks, tweak => tweak.Id == "system.memory-paged-pool-dynamic");
        Assert.Contains(tweaks, tweak => tweak.Id == "system.memory-nonpaged-pool-dynamic");
        Assert.Contains(tweaks, tweak => tweak.Id == "system.memory-registry-quota-default");
        Assert.Contains(tweaks, tweak => tweak.Id == "system.graphics-page-fault-debug-mode");
    }

    [Fact]
    public void PowerProvider_Uses_PowerThrottling_Subkey()
    {
        var provider = new PowerTweakProvider();
        var tweaks = provider.CreateTweaks(default!, BuildContext(), false).ToList();

        Assert.Contains(tweaks, tweak => tweak.Id == "power.disable-power-throttling");
        Assert.DoesNotContain(tweaks, tweak => tweak.Id == "performance.disable-background-apps");
    }

    [Fact]
    public void VisibilityProvider_Uses_Local_Registry_Tweak_For_Common_Control_Animations()
    {
        var provider = new VisibilityTweakProvider();
        var tweaks = provider.CreateTweaks(default!, BuildContext(), false).ToList();

        var tweak = Assert.IsType<RegistryValueTweak>(
            tweaks.Single(item => item.Id == "visibility.disable-common-control-animations"));

        Assert.False(tweak.RequiresElevation);
        Assert.Equal(RegistryHive.CurrentUser, tweak.Reference.Hive);
        Assert.Equal(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", tweak.Reference.KeyPath);
        Assert.Equal("TurnOffSPIAnimations", tweak.Reference.ValueName);
        Assert.Equal(RegistryValueKind.DWord, tweak.ValueKind);
        Assert.Equal(1, Assert.IsType<int>(tweak.TargetValue));
    }

    [Fact]
    public void PowerProvider_Uses_Command_Based_CpuCoreParking_Tweak()
    {
        var provider = new PowerTweakProvider();
        var tweaks = provider.CreateTweaks(default!, BuildContext(), false).ToList();

        Assert.Contains(tweaks, tweak => tweak.Id == "power.disable-cpu-parking" && tweak.GetType().Name == "DisableCpuCoreParkingTweak");
    }

    [Fact]
    public void PowerProvider_Uses_CommandBacked_Registry_Tweaks_For_Registry_Power_Actions()
    {
        var provider = new PowerTweakProvider();
        var tweaks = provider.CreateTweaks(default!, BuildContext(), false).ToList();

        var commandBackedIds = new[]
        {
            "power.disable-fast-startup",
            "power.disable-power-throttling",
            "power.optimize-performance",
            "power.disable-cpu-idle-states",
            "power.optimize-cpu-boost",
            "power.disable-network-power-saving",
            "power.optimize-gaming-network"
        };

        foreach (var id in commandBackedIds)
        {
            var expectedType = id == "power.optimize-cpu-boost"
                ? "SetCpuBoostPerfModeTweak"
                : "RegistryCommandBatchTweak";

            var availableTweaks = string.Join(", ", tweaks.Select(tweak => $"{tweak.Id}:{tweak.GetType().Name}"));
            var tweak = tweaks.SingleOrDefault(item => item.Id == id);
            Assert.True(tweak is not null, $"Missing {id}. Available: {availableTweaks}");
            Assert.Equal(expectedType, tweak!.GetType().Name);
        }
    }

    [Fact]
    public void PerformanceProvider_Uses_ServiceBacked_WindowsSearch_Tweak()
    {
        var provider = new PerformanceTweakProvider();
        var tweaks = provider.CreateTweaks(default!, BuildContext(), false).ToList();

        Assert.Equal("ServiceStartModeBatchTweak", tweaks.Single(tweak => tweak.Id == "power.disable-windows-search").GetType().Name);
    }

    [Fact]
    public void SystemProvider_Exposes_Individual_Service_Tweaks_Instead_Of_One_Bulk_Toggle()
    {
        var provider = new SystemTweakProvider();
        var tweaks = provider.CreateTweaks(default!, BuildContext(), false).ToList();

        Assert.DoesNotContain(tweaks, tweak => tweak.Id == "system.disable-non-essential-services");
        Assert.Contains(tweaks, tweak => tweak.Id == "system.services.disable-connected-user-experiences");
        Assert.Contains(tweaks, tweak => tweak.Id == "system.services.disable-print-spooler");
        Assert.Contains(tweaks, tweak => tweak.Id == "system.services.disable-bluetooth-support");
    }

    [Fact]
    public void SystemProvider_Uses_CommandBacked_Search_Web_Results_Tweak()
    {
        var provider = new SystemRegistryTweakProvider();
        var tweaks = provider.CreateTweaks(default!, BuildContext(), false).ToList();

        Assert.Equal("RegistryCommandBatchTweak", tweaks.Single(tweak => tweak.Id == "system.disable-search-web-results").GetType().Name);
    }

    private static TweakContext BuildContext(IRegistryAccessor? registryAccessor = null)
    {
        var registry = registryAccessor ?? new Mock<IRegistryAccessor>(MockBehavior.Loose).Object;
        return new TweakContext(
            registry,
            registry,
            new Mock<IServiceManager>(MockBehavior.Loose).Object,
            new Mock<IScheduledTaskManager>(MockBehavior.Loose).Object,
            new Mock<IFileSystemAccessor>(MockBehavior.Loose).Object,
            new Mock<ICommandRunner>(MockBehavior.Loose).Object);
    }
}
