using System;

namespace WindowsOptimizer.Infrastructure.Elevation;

public sealed record ElevatedFileRequest(
    Guid RequestId,
    ElevatedFileOperation Operation,
    string SourcePath,
    string? DestinationPath = null);
