using System;
using WindowsOptimizer.Core.Tasks;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed record ElevatedScheduledTaskResponse(
    Guid RequestId,
    bool Success,
    string? Error,
    ScheduledTaskInfo? Info = null);
