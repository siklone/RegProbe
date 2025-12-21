using System;

namespace WindowsOptimizer.Infrastructure.Commands;

public sealed record CommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut,
    TimeSpan Duration);
