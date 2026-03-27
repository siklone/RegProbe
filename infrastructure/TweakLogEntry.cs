using System;
using RegProbe.Core;

namespace RegProbe.Infrastructure;

public sealed record TweakLogEntry(
    DateTimeOffset Timestamp,
    string TweakId,
    string TweakName,
    TweakAction Action,
    TweakStatus Status,
    string Message,
    string? Error);
