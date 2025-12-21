namespace WindowsOptimizer.Infrastructure.Services;

public sealed record ServiceInfo(
    bool Exists,
    ServiceStartMode StartMode,
    ServiceStatus Status);
