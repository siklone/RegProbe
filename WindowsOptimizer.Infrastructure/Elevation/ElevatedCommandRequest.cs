using System;
using WindowsOptimizer.Core.Commands;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed record ElevatedCommandRequest(
    Guid RequestId,
    ElevatedCommandOperation Operation,
    CommandRequest Command);
