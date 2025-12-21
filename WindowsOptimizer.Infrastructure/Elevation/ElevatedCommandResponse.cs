using System;
using WindowsOptimizer.Infrastructure.Commands;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed record ElevatedCommandResponse(
    Guid RequestId,
    bool Success,
    string? ErrorMessage,
    CommandResult? Result);
