using OpenTraceProject.Core.Services;

namespace OpenTraceProject.Engine.Tweaks;

public sealed record ServiceStartModeEntry(
    string ServiceName,
    ServiceStartMode TargetStartMode);
