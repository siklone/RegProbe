using WindowsOptimizer.Infrastructure.Services;

namespace WindowsOptimizer.Engine.Tweaks;

public sealed record ServiceStartModeEntry(
    string ServiceName,
    ServiceStartMode TargetStartMode);
