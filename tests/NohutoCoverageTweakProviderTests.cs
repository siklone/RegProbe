using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Microsoft.Win32;
using RegProbe.Application.Services;
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
    public void Catalog_Surfaces_New_Nohuto_Privacy_Settings()
    {
        var catalog = new TweakCatalogService();

        var crossDevice = catalog.FindById("privacy.disable-cross-device-experiences.policy");
        var findMyDevice = catalog.FindById("privacy.disable-find-my-device");

        Assert.IsType<RegistryValuePresetBatchTweak>(crossDevice);
        Assert.IsType<RegistryValuePresetBatchTweak>(findMyDevice);
    }

    [Fact]
    public void PrivacyProvider_And_Catalog_Use_Current_Batch_Shapes_For_Special_Cases()
    {
        var provider = new PrivacyTweakProvider();
        var tweaks = provider.CreateTweaks(default!, BuildContext(), false).ToList();
        var catalog = new TweakCatalogService();

        Assert.Equal("RegistryCommandBatchTweak", tweaks.Single(tweak => tweak.Id == "privacy.disable-ceip").GetType().Name);
        Assert.IsType<RegistryValueBatchTweak>(catalog.FindById("privacy.disable-edge-search-suggestions"));
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
        var catalog = new TweakCatalogService();

        Assert.Contains(tweaks, tweak => tweak.Id == "system.kernel-cache-aware-scheduling");
        Assert.Contains(tweaks, tweak => tweak.Id == "system.kernel-default-dynamic-hetero-cpu-policy");
        Assert.Contains(tweaks, tweak => tweak.Id == "system.graphics-page-fault-debug-mode");
        Assert.IsType<RegistryValuePresetBatchTweak>(catalog.FindById("system.memory-large-system-cache-client"));
        Assert.IsType<RegistryValuePresetBatchTweak>(catalog.FindById("system.memory-paged-pool-dynamic"));
        Assert.IsType<RegistryValueTweak>(catalog.FindById("system.memory-nonpaged-pool-dynamic"));
        Assert.IsType<RegistryValueTweak>(catalog.FindById("system.memory-registry-quota-default"));
    }

    [Fact]
    public void Catalog_Surfaces_PowerThrottling_Subkey()
    {
        var catalog = new TweakCatalogService();
        Assert.IsType<RegistryValuePresetBatchTweak>(catalog.FindById("power.disable-power-throttling"));
    }

    [Fact]
    public void Catalog_Surfaces_Common_Control_Animations_As_Preset_Batch()
    {
        var catalog = new TweakCatalogService();
        Assert.IsType<RegistryValuePresetBatchTweak>(catalog.FindById("visibility.disable-common-control-animations"));
    }

    [Fact]
    public void PowerProvider_Uses_Command_Based_CpuCoreParking_Tweak()
    {
        var provider = new PowerTweakProvider();
        var tweaks = provider.CreateTweaks(default!, BuildContext(), false).ToList();

        Assert.Contains(tweaks, tweak => tweak.Id == "power.disable-cpu-parking" && tweak.GetType().Name == "DisableCpuCoreParkingTweak");
    }

    [Fact]
    public void PowerProvider_Exposes_RecordBacked_Power_Batches()
    {
        var provider = new PowerTweakProvider();
        var tweaks = provider.CreateTweaks(default!, BuildContext(), false).ToList();

        var commandBackedIds = new[]
        {
            "power.optimize-performance",
            "power.disable-network-power-saving.policy",
        };

        foreach (var id in commandBackedIds)
        {
            var availableTweaks = string.Join(", ", tweaks.Select(tweak => $"{tweak.Id}:{tweak.GetType().Name}"));
            var tweak = tweaks.SingleOrDefault(item => item.Id == id);
            Assert.True(tweak is not null, $"Missing {id}. Available: {availableTweaks}");
            Assert.Equal("RegistryCommandBatchTweak", tweak!.GetType().Name);
        }
    }

    [Fact]
    public void Catalog_Surfaces_ServiceBacked_WindowsSearch_Tweak()
    {
        var catalog = new TweakCatalogService();
        Assert.Equal("ServiceStartModeBatchTweak", catalog.FindById("power.disable-windows-search")!.GetType().Name);
    }

    [Fact]
    public void Catalog_Surfaces_Individual_Service_Tweaks_Instead_Of_One_Bulk_Toggle()
    {
        var catalog = new TweakCatalogService().GetAll().Select(entry => entry.Tweak).ToList();

        Assert.DoesNotContain(catalog, tweak => tweak.Id == "system.disable-non-essential-services");
        Assert.Contains(catalog, tweak => tweak.Id == "system.services.disable-connected-user-experiences");
        Assert.Contains(catalog, tweak => tweak.Id == "system.services.disable-print-spooler");
        Assert.Contains(catalog, tweak => tweak.Id == "system.services.disable-bluetooth-support");
    }

    [Fact]
    public void Catalog_Surfaces_Search_Web_Results_Tweak_As_Preset_Batch()
    {
        var catalog = new TweakCatalogService();
        Assert.IsType<RegistryValuePresetBatchTweak>(catalog.FindById("system.disable-search-web-results"));
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
