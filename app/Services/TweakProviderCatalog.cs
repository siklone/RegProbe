using RegProbe.Application.Services.TweakProviders;

namespace RegProbe.Application.Services;

internal static class TweakProviderCatalog
{
    public static IReadOnlyList<ITweakProvider> CreateDefault()
    {
        return
        [
            new SystemTweakProvider(),
            new SystemRegistryTweakProvider(),
            new PrivacyTweakProvider(),
            new SecurityTweakProvider(),
            new NetworkTweakProvider(),
            new PowerTweakProvider(),
            new PeripheralTweakProvider(),
            new VisibilityTweakProvider(),
            new PerformanceTweakProvider(),
            new AudioTweakProvider(),
            new MiscTweakProvider(),
            new DeveloperTweakProvider()
        ];
    }
}
