using System;
using System.Collections.Generic;
using System.Linq;
using OpenTraceProject.Core;

namespace OpenTraceProject.Engine;

public sealed record TweakExecutionReport(
    string TweakId,
    string TweakName,
    bool DryRun,
    bool Applied,
    bool Verified,
    bool RolledBack,
    IReadOnlyList<TweakExecutionStep> Steps,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt)
{
    public bool Succeeded => Steps.All(step => step.Result.Status != TweakStatus.Failed);
}
