using System;

namespace OpenTraceProject.Infrastructure.Elevation;

public sealed record ElevatedFileResponse(
    Guid RequestId,
    bool Success,
    string? Error,
    bool? Exists = null);
