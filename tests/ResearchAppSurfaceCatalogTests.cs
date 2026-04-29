using RegProbe.Application.Services;

namespace RegProbe.Tests;

public sealed class ResearchAppSurfaceCatalogTests
{
    [Fact]
    public void Catalog_Surfaces_Validated_Research_Cards_From_App_Surface_Manifest()
    {
        var catalog = new TweakCatalogService();

        var policyTweak = catalog.FindById("policy.system.enable-virtualization");
        var powerTweak = catalog.FindById("power.control.class1-initial-unpark-count");

        Assert.NotNull(policyTweak);
        Assert.NotNull(powerTweak);
        Assert.Equal("Enable Virtualization", policyTweak!.Name);
        Assert.Equal("Class1 Initial Unpark Count", powerTweak!.Name);
    }
}
