using System.Collections.Generic;

namespace RegProbe.Infrastructure;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public bool DemoTweakAlphaEnabled { get; set; }
    public bool DemoTweakBetaEnabled { get; set; }
    public string Theme { get; set; } = "Dark";
    public List<MonitorSectionState> MonitorSections { get; set; } = new();
}

public sealed class MonitorSectionState
{
    public string Key { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsVisible { get; set; } = true;
}
