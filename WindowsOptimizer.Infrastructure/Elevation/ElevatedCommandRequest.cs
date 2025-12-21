using System;
using WindowsOptimizer.Infrastructure.Commands;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed record ElevatedCommandRequest(
    Guid RequestId,
    ElevatedCommandOperation Operation,
    CommandRequest Command);
