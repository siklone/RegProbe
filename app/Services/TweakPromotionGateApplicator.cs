using System.Reflection;

namespace RegProbe.Application.Services;

internal sealed class TweakPromotionGateApplicator
{
    public bool TryCreateTarget<T>(
        T tweak,
        out string tweakId,
        out Action<TweakPromotionGateEntry> applyResearchPromotionGate) where T : class
    {
        tweakId = string.Empty;
        applyResearchPromotionGate = static _ => { };

        if (tweak is null)
        {
            return false;
        }

        var tweakType = tweak.GetType();
        var idProperty = tweakType.GetProperty("Id");
        if (idProperty?.GetValue(tweak) is not string id || string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        var applyMethod = tweakType.GetMethod("ApplyResearchPromotionGate", [typeof(TweakPromotionGateEntry)]);
        if (applyMethod is null)
        {
            return false;
        }

        tweakId = id;
        applyResearchPromotionGate = entry => applyMethod.Invoke(tweak, [entry]);
        return true;
    }
}
