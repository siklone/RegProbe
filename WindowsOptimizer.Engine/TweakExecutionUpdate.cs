using System;
using WindowsOptimizer.Core;

namespace WindowsOptimizer.Engine;

public sealed record TweakExecutionUpdate(
    string TweakId,
    string TweakName,
    TweakAction Action,
    TweakStatus Status,
    string Message,
    DateTimeOffset Timestamp);
