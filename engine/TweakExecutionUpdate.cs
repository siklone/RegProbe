using System;
using RegProbe.Core;

namespace RegProbe.Engine;

public sealed record TweakExecutionUpdate(
    string TweakId,
    string TweakName,
    TweakAction Action,
    TweakStatus Status,
    string Message,
    DateTimeOffset Timestamp);
