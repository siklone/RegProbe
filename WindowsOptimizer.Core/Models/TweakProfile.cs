using System;
using System.Collections.Generic;

namespace WindowsOptimizer.Core.Models;

public sealed class TweakProfile
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = "User";
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public string Version { get; set; } = "1.0";
    public List<string> SelectedTweakIds { get; set; } = new();
    public List<string> AppliedTweakIds { get; set; } = new();
    public ProfileMetadata? Metadata { get; set; }
}

public sealed class ProfileMetadata
{
    public string TargetUseCase { get; set; } = string.Empty;
    public int TotalTweakCount { get; set; }
    public Dictionary<string, int> TweaksByCategory { get; set; } = new();
    public Dictionary<string, int> TweaksByRiskLevel { get; set; } = new();
}
