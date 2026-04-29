using RegProbe.Application.Services;
using RegProbe.Engine.Tweaks;

namespace RegProbe.Tests;

public sealed class ResearchAppSurfaceCatalogTests
{
    [Fact]
    public void Catalog_Surfaces_Validated_Research_Cards_From_App_Surface_Manifest()
    {
        var catalog = new TweakCatalogService();

        var beepTweak = catalog.FindById("audio.disable-beep");
        var policyTweak = catalog.FindById("policy.system.enable-virtualization");
        var disconnectedAudioTweak = catalog.FindById("audio.show-disconnected-devices");
        var hiddenAudioTweak = catalog.FindById("audio.show-hidden-devices");
        var powerTweak = catalog.FindById("power.control.class1-initial-unpark-count");
        var watchdogTweak = catalog.FindById("power.session-watchdog-timeouts");
        var subtreeTweak = catalog.FindById("power.control.power-request-override-subtree");
        var executiveTweak = catalog.FindById("system.executive-additional-worker-threads");
        var kernelTweak = catalog.FindById("system.kernel.disable-exception-chain-validation");

        Assert.NotNull(beepTweak);
        Assert.NotNull(hiddenAudioTweak);
        Assert.NotNull(disconnectedAudioTweak);
        Assert.NotNull(policyTweak);
        Assert.NotNull(powerTweak);
        Assert.NotNull(watchdogTweak);
        Assert.NotNull(subtreeTweak);
        Assert.NotNull(executiveTweak);
        Assert.NotNull(kernelTweak);
        Assert.Equal("System Beep Driver", beepTweak!.Name);
        Assert.Equal("Show Hidden Audio Devices", hiddenAudioTweak!.Name);
        Assert.Equal("Show Disconnected Audio Devices", disconnectedAudioTweak!.Name);
        Assert.Equal("Enable Virtualization", policyTweak!.Name);
        Assert.Equal("Class1 Initial Unpark Count", powerTweak!.Name);
        Assert.IsType<RegistryValuePresetBatchTweak>(beepTweak);
        Assert.IsType<RegistryValuePresetBatchTweak>(hiddenAudioTweak);
        Assert.IsType<RegistryValuePresetBatchTweak>(disconnectedAudioTweak);
        Assert.IsType<RegistryValuePresetBatchTweak>(policyTweak);
        Assert.IsType<RegistryValueBatchTweak>(watchdogTweak);
        Assert.IsType<RegistrySubtreeTweak>(subtreeTweak);
        Assert.IsType<RegistryValueBatchTweak>(executiveTweak);
        Assert.IsType<RegistryValueTweak>(kernelTweak);
    }
}
