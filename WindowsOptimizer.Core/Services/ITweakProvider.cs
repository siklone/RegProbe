using System.Collections.Generic;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine;

namespace WindowsOptimizer.Core.Services;

public interface ITweakProvider
{
    string CategoryName { get; }
    IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated);
}
