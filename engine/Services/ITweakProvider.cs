using System.Collections.Generic;
using RegProbe.Core;
using RegProbe.Core.Services;

namespace RegProbe.Engine.Services;

public interface ITweakProvider
{
    string CategoryName { get; }
    IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated);
}
