using System;
using OpenTraceProject.Core;

namespace OpenTraceProject.Infrastructure;

public sealed record TweakLogEntry(
    DateTimeOffset Timestamp,
    string TweakId,
    string TweakName,
    TweakAction Action,
    TweakStatus Status,
    string Message,
    string? Error);
