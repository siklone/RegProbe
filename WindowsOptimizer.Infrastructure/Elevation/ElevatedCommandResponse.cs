using System;
using WindowsOptimizer.Core.Commands;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed record ElevatedCommandResponse(
    Guid RequestId,
    bool Success,
    string? ErrorMessage,
    CommandResult? Result);
