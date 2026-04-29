using RegProbe.Application.Services;
using RegProbe.Engine.Tweaks;

namespace RegProbe.Tests;

public sealed class ResearchAppSurfaceCatalogTests
{
    [Fact]
    public void Catalog_Surfaces_Validated_Research_Cards_From_App_Surface_Manifest()
    {
        var catalog = new TweakCatalogService();

        var policyTweak = catalog.FindById("policy.system.enable-virtualization");
        var powerTweak = catalog.FindById("power.control.class1-initial-unpark-count");
        var watchdogTweak = catalog.FindById("power.session-watchdog-timeouts");
        var executiveTweak = catalog.FindById("system.executive-additional-worker-threads");
        var kernelTweak = catalog.FindById("system.kernel.disable-exception-chain-validation");

        Assert.NotNull(policyTweak);
        Assert.NotNull(powerTweak);
        Assert.NotNull(watchdogTweak);
        Assert.NotNull(executiveTweak);
        Assert.NotNull(kernelTweak);
        Assert.Equal("Enable Virtualization", policyTweak!.Name);
        Assert.Equal("Class1 Initial Unpark Count", powerTweak!.Name);
        Assert.IsType<RegistryValuePresetBatchTweak>(policyTweak);
        Assert.IsType<RegistryValueBatchTweak>(watchdogTweak);
        Assert.IsType<RegistryValueBatchTweak>(executiveTweak);
        Assert.IsType<RegistryValueTweak>(kernelTweak);
    }
}
