using System;
using OpenTraceProject.Core;

namespace OpenTraceProject.Engine;

public sealed record TweakExecutionUpdate(
    string TweakId,
    string TweakName,
    TweakAction Action,
    TweakStatus Status,
    string Message,
    DateTimeOffset Timestamp);
