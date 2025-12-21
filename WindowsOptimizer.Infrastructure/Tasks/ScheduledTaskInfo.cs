namespace WindowsOptimizer.Infrastructure.Tasks;

public sealed record ScheduledTaskInfo(
    bool Exists,
    bool Enabled);
