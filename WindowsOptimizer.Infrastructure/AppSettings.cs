namespace WindowsOptimizer.Infrastructure;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public bool DemoTweakAlphaEnabled { get; set; }
    public bool DemoTweakBetaEnabled { get; set; }
}
