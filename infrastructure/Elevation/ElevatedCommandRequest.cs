using System;
using RegProbe.Core.Commands;

namespace RegProbe.Infrastructure.Elevation;

public sealed record ElevatedCommandRequest(
    Guid RequestId,
    ElevatedCommandOperation Operation,
    CommandRequest Command);
