using System;
using System.Collections.Generic;
using WindowsOptimizer.App.HardwareDb.Models;

namespace WindowsOptimizer.App.HardwareDb;

public enum HardwareMatchKind
{
    None = 0,
    ExactName = 1,
    ExactAlias = 2,
    PartialName = 3,
    PartialAlias = 4,
    ProvidedModel = 5
}

public sealed record HardwareMatchResult<TModel>(TModel? Model, HardwareMatchKind MatchKind)
    where TModel : HardwareModelBase
{
    public bool HasMatch => Model != null;
}

public static class HardwareMatcher
{
    public static TModel? Match<TModel>(
        string rawName,
        IReadOnlyDictionary<string, TModel> normalizedIndex,
        IReadOnlyDictionary<string, TModel> aliasIndex)
        where TModel : HardwareModelBase
    {
        return MatchDetailed(rawName, normalizedIndex, aliasIndex).Model;
    }

    public static HardwareMatchResult<TModel> MatchDetailed<TModel>(
        string rawName,
        IReadOnlyDictionary<string, TModel> normalizedIndex,
        IReadOnlyDictionary<string, TModel> aliasIndex)
        where TModel : HardwareModelBase
    {
        var normalized = HardwareNameNormalizer.Normalize(rawName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new HardwareMatchResult<TModel>(null, HardwareMatchKind.None);
        }

        if (normalizedIndex.TryGetValue(normalized, out var direct))
        {
            return new HardwareMatchResult<TModel>(direct, HardwareMatchKind.ExactName);
        }

        if (aliasIndex.TryGetValue(normalized, out var byAlias))
        {
            return new HardwareMatchResult<TModel>(byAlias, HardwareMatchKind.ExactAlias);
        }

        var best = FindBestPartialMatch(normalized, normalizedIndex, preferSource: 2);
        var bestAlias = FindBestPartialMatch(normalized, aliasIndex, preferSource: 1);

        if (bestAlias != null && (best == null || bestAlias.Value.Score > best.Value.Score))
        {
            return new HardwareMatchResult<TModel>(bestAlias.Value.Model, HardwareMatchKind.PartialAlias);
        }

        return best != null
            ? new HardwareMatchResult<TModel>(best.Value.Model, HardwareMatchKind.PartialName)
            : new HardwareMatchResult<TModel>(null, HardwareMatchKind.None);
    }

    private static (TModel Model, int Score)? FindBestPartialMatch<TModel>(
        string normalized,
        IReadOnlyDictionary<string, TModel> index,
        int preferSource)
        where TModel : HardwareModelBase
    {
        (TModel Model, int Score)? best = null;
        var normalizedTokenCount = CountTokens(normalized);

        foreach (var kv in index)
        {
            var key = kv.Key;
            var keyTokenCount = CountTokens(key);
            var canUseForwardContains = keyTokenCount >= 2 || key.Length >= 6;
            var canUseReverseContains = normalizedTokenCount >= 2 && normalized.Length >= 6;

            var isPartialMatch =
                (canUseForwardContains && normalized.Contains(key, StringComparison.Ordinal)) ||
                (canUseReverseContains && key.Contains(normalized, StringComparison.Ordinal));

            if (!isPartialMatch)
            {
                continue;
            }

            var score = (key.Length * 10) + (keyTokenCount * 100) + preferSource;
            if (best == null || score > best.Value.Score)
            {
                best = (kv.Value, score);
            }
        }

        return best;
    }

    private static int CountTokens(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
