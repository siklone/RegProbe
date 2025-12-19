namespace WindowsOptimizer.Infrastructure;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public bool DemoTweakAlphaEnabled { get; set; }
    public bool DemoTweakBetaEnabled { get; set; }
    public bool DemoTweakGammaEnabled { get; set; }
    public bool DemoTweakDeltaEnabled { get; set; }
}
