using System;

namespace RegProbe.Infrastructure.Elevation;

public sealed record ElevatedFileResponse(
    Guid RequestId,
    bool Success,
    string? Error,
    bool? Exists = null);
