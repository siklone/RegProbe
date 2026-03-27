using RegProbe.Core.Services;

namespace RegProbe.Engine.Tweaks;

public sealed record ServiceStartModeEntry(
    string ServiceName,
    ServiceStartMode TargetStartMode);
