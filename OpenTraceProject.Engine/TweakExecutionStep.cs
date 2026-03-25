using OpenTraceProject.Core;

namespace OpenTraceProject.Engine;

public sealed record TweakExecutionStep(TweakAction Action, TweakResult Result);
