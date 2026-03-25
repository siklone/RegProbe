using System.Collections.Generic;
using OpenTraceProject.Core;
using OpenTraceProject.Core.Services;

namespace OpenTraceProject.Engine.Services;

public interface ITweakProvider
{
    string CategoryName { get; }
    IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated);
}
