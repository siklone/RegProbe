using System.Collections.Generic;

namespace WindowsOptimizer.Core.Commands;

public sealed record CommandRequest(
    string Executable,
    IReadOnlyList<string> Arguments,
    int TimeoutSeconds = 30,
    string? WorkingDirectory = null);
