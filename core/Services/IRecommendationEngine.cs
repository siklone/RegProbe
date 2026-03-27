using System.Collections.Generic;
using RegProbe.Core.Intelligence;

namespace RegProbe.Core.Services;

public interface IRecommendationEngine
{
    IEnumerable<TweakRecommendation> GetRecommendations(HardwareProfile profile);
}
