using System.Collections.Generic;
using OpenTraceProject.Core.Intelligence;

namespace OpenTraceProject.Core.Services;

public interface IRecommendationEngine
{
    IEnumerable<TweakRecommendation> GetRecommendations(HardwareProfile profile);
}
