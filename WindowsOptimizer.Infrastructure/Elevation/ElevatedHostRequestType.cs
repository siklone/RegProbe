namespace WindowsOptimizer.Infrastructure.Elevation;

public enum ElevatedHostRequestType
{
    Registry,
    Service,
    ScheduledTask,
    FileSystem,
    Command,
    Ping
}
