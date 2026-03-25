using System.Collections.Generic;

namespace OpenTraceProject.Infrastructure;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public bool DemoTweakAlphaEnabled { get; set; }
    public bool DemoTweakBetaEnabled { get; set; }
    public string Theme { get; set; } = "Dark";
    public bool EnableCardShadows { get; set; }
    public bool RunStartupScanOnLaunch { get; set; } = true;
    public bool ShowPreviewHint { get; set; } = true;
    public bool IsCompactMode { get; set; }
    public List<MonitorSectionState> MonitorSections { get; set; } = new();
}

public sealed class MonitorSectionState
{
    public string Key { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsVisible { get; set; } = true;
}
