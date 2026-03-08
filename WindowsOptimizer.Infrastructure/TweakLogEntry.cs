using System;
using WindowsOptimizer.Core;

namespace WindowsOptimizer.Infrastructure;

public sealed record TweakLogEntry(
    DateTimeOffset Timestamp,
    string TweakId,
    string TweakName,
    TweakAction Action,
    TweakStatus Status,
    string Message,
    string? Error);
