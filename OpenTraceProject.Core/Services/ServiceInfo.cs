namespace OpenTraceProject.Core.Services;

public sealed record ServiceInfo(
    bool Exists,
    ServiceStartMode StartMode,
    ServiceStatus Status);
