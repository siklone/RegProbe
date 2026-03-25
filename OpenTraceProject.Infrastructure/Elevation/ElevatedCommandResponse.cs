using System;
using OpenTraceProject.Core.Commands;

namespace OpenTraceProject.Infrastructure.Elevation;

public sealed record ElevatedCommandResponse(
    Guid RequestId,
    bool Success,
    string? ErrorMessage,
    CommandResult? Result);
