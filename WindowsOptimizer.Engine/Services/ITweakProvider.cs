using System.Collections.Generic;
using WindowsOptimizer.Core;

namespace WindowsOptimizer.Engine.Services;

public interface ITweakProvider
{
    string CategoryName { get; }
    IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated);
}
