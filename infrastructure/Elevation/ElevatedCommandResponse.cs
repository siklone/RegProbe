using System;
using RegProbe.Core.Commands;

namespace RegProbe.Infrastructure.Elevation;

public sealed record ElevatedCommandResponse(
    Guid RequestId,
    bool Success,
    string? ErrorMessage,
    CommandResult? Result);
