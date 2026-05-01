using RegProbe.Application.Services.TweakProviders;
using RegProbe.Engine.Services;

namespace RegProbe.Application.Services;

internal static class TweakProviderCatalog
{
    public static IReadOnlyList<ITweakProvider> CreateDefault()
    {
        return
        [
            new ResearchAppSurfaceTweakProvider(),
            new SystemTweakProvider(),
            new SystemRegistryTweakProvider(),
            new PrivacyTweakProvider(),
            new SecurityTweakProvider(),
            new NetworkTweakProvider(),
            new PowerTweakProvider(),
            new PeripheralTweakProvider(),
            new PerformanceTweakProvider(),
            new AudioTweakProvider(),
            new MiscTweakProvider(),
            new DeveloperTweakProvider()
        ];
    }
}
