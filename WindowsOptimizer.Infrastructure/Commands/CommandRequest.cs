using System.Collections.Generic;

namespace WindowsOptimizer.Infrastructure.Commands;

public sealed record CommandRequest(
    string Executable,
    IReadOnlyList<string> Arguments,
    int TimeoutSeconds = 30,
    string? WorkingDirectory = null);
