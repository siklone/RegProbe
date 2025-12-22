using System.Collections.Generic;
using WindowsOptimizer.Core.Intelligence;

namespace WindowsOptimizer.Core.Services;

public interface IRecommendationEngine
{
    IEnumerable<TweakRecommendation> GetRecommendations(HardwareProfile profile);
}
