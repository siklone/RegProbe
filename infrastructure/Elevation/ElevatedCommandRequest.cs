using System;
using OpenTraceProject.Core.Commands;

namespace OpenTraceProject.Infrastructure.Elevation;

public sealed record ElevatedCommandRequest(
    Guid RequestId,
    ElevatedCommandOperation Operation,
    CommandRequest Command);
