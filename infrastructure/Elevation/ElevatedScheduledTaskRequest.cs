using System;

namespace RegProbe.Infrastructure.Elevation;

public sealed record ElevatedScheduledTaskRequest(
    Guid RequestId,
    ElevatedScheduledTaskOperation Operation,
    string TaskPath,
    bool? Enabled = null);
