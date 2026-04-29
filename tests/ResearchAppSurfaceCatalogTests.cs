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
        var compactModeTweak = catalog.FindById("explorer.enable-explorer-compact-mode");
        var driveLettersTweak = catalog.FindById("explorer.show-drive-letters-first");
        var infoTipsTweak = catalog.FindById("explorer.show-info-tips");
        var hiddenFilesTweak = catalog.FindById("explorer.show-hidden-files");
        var fileExtensionsTweak = catalog.FindById("explorer.show-file-extensions");
        var statusBarTweak = catalog.FindById("explorer.show-status-bar");
        var typeOverlayTweak = catalog.FindById("explorer.show-type-overlay");
        var powerTweak = catalog.FindById("power.control.class1-initial-unpark-count");
        var watchdogTweak = catalog.FindById("power.session-watchdog-timeouts");
        var subtreeTweak = catalog.FindById("power.control.power-request-override-subtree");
        var executiveTweak = catalog.FindById("system.executive-additional-worker-threads");
        var kernelTweak = catalog.FindById("system.kernel.disable-exception-chain-validation");

        Assert.NotNull(beepTweak);
        Assert.NotNull(hiddenAudioTweak);
        Assert.NotNull(disconnectedAudioTweak);
        Assert.NotNull(compactModeTweak);
        Assert.NotNull(driveLettersTweak);
        Assert.NotNull(hiddenFilesTweak);
        Assert.NotNull(fileExtensionsTweak);
        Assert.NotNull(infoTipsTweak);
        Assert.NotNull(policyTweak);
        Assert.NotNull(powerTweak);
        Assert.NotNull(statusBarTweak);
        Assert.NotNull(watchdogTweak);
        Assert.NotNull(subtreeTweak);
        Assert.NotNull(executiveTweak);
        Assert.NotNull(kernelTweak);
        Assert.NotNull(typeOverlayTweak);
        Assert.Equal("System Beep Driver", beepTweak!.Name);
        Assert.Equal("Show Hidden Audio Devices", hiddenAudioTweak!.Name);
        Assert.Equal("Show Disconnected Audio Devices", disconnectedAudioTweak!.Name);
        Assert.Equal("Enable Compact View", compactModeTweak!.Name);
        Assert.Equal("Show Drive Letters First", driveLettersTweak!.Name);
        Assert.Equal("Show Hidden Files and Folders", hiddenFilesTweak!.Name);
        Assert.Equal("Show File Extensions", fileExtensionsTweak!.Name);
        Assert.Equal("Show Explorer Info Tips", infoTipsTweak!.Name);
        Assert.Equal("Enable Virtualization", policyTweak!.Name);
        Assert.Equal("Class1 Initial Unpark Count", powerTweak!.Name);
        Assert.Equal("Show Explorer Status Bar", statusBarTweak!.Name);
        Assert.Equal("Display File Icons on Thumbnails", typeOverlayTweak!.Name);
        Assert.IsType<RegistryValuePresetBatchTweak>(beepTweak);
        Assert.IsType<RegistryValuePresetBatchTweak>(hiddenAudioTweak);
        Assert.IsType<RegistryValuePresetBatchTweak>(disconnectedAudioTweak);
        Assert.IsType<RegistryValuePresetBatchTweak>(compactModeTweak);
        Assert.IsType<RegistryValuePresetBatchTweak>(driveLettersTweak);
        Assert.IsType<RegistryValuePresetBatchTweak>(hiddenFilesTweak);
        Assert.IsType<RegistryValuePresetBatchTweak>(fileExtensionsTweak);
        Assert.IsType<RegistryValuePresetBatchTweak>(infoTipsTweak);
        Assert.IsType<RegistryValuePresetBatchTweak>(policyTweak);
        Assert.IsType<RegistryValuePresetBatchTweak>(statusBarTweak);
        Assert.IsType<RegistryValuePresetBatchTweak>(typeOverlayTweak);
        Assert.IsType<RegistryValueBatchTweak>(watchdogTweak);
        Assert.IsType<RegistrySubtreeTweak>(subtreeTweak);
        Assert.IsType<RegistryValueBatchTweak>(executiveTweak);
        Assert.IsType<RegistryValueTweak>(kernelTweak);
    }
}
