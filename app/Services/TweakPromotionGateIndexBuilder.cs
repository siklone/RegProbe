namespace RegProbe.Application.Services;

internal static class TweakPromotionGateIndexBuilder
{
    public static IReadOnlyDictionary<string, TweakPromotionGateEntry> BuildIndex(IEnumerable<TweakPromotionGateEntry> entries)
    {
        var index = new Dictionary<string, TweakPromotionGateEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            foreach (var key in new[]
                     {
                         entry.CandidateId,
                         entry.RecordId,
                         entry.TweakId,
                     }.Where(static value => !string.IsNullOrWhiteSpace(value)))
            {
                if (!index.ContainsKey(key))
                {
                    index[key] = entry;
                }
            }
        }

        return index;
    }

    public static IReadOnlyDictionary<string, BlockedWorklistEntry> BuildBlockedWorklistIndex(IEnumerable<BlockedWorklistEntry> entries)
    {
        var index = new Dictionary<string, BlockedWorklistEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.CandidateId) && !index.ContainsKey(entry.CandidateId))
            {
                index[entry.CandidateId] = entry;
            }
        }

        return index;
    }
}
