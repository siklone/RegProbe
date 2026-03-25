using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpenTraceProject.App.HardwareDb.Models;

namespace OpenTraceProject.App.HardwareDb;

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
    private static readonly Regex RevisionSuffixRegex = new(@"\brev\s+[a-z0-9]+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

        if (best != null &&
            bestAlias != null &&
            best.Value.Score == bestAlias.Value.Score &&
            !SameIdentity(best.Value.Model, bestAlias.Value.Model))
        {
            return new HardwareMatchResult<TModel>(null, HardwareMatchKind.None);
        }

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
        var hasAmbiguousBest = false;
        var normalizedTokenCount = CountTokens(normalized);

        foreach (var kv in index)
        {
            var key = kv.Key;
            var keyTokenCount = CountTokens(key);
            var canUseForwardContains = keyTokenCount >= 2 || key.Length >= 6;
            var canUseReverseContains = normalizedTokenCount >= 3 && normalized.Length >= 6;

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
                hasAmbiguousBest = false;
                continue;
            }

            if (score == best.Value.Score && !SameIdentity(best.Value.Model, kv.Value))
            {
                hasAmbiguousBest = true;
            }
        }

        return hasAmbiguousBest ? null : best;
    }

    private static int CountTokens(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static bool SameIdentity<TModel>(TModel left, TModel right)
        where TModel : HardwareModelBase
    {
        if (!string.IsNullOrWhiteSpace(left.Id) &&
            !string.IsNullOrWhiteSpace(right.Id) &&
            string.Equals(left.Id, right.Id, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(left.Brand) &&
            !string.IsNullOrWhiteSpace(right.Brand) &&
            !string.IsNullOrWhiteSpace(left.ModelName) &&
            !string.IsNullOrWhiteSpace(right.ModelName) &&
            !string.IsNullOrWhiteSpace(left.IconKey) &&
            string.Equals(left.Brand, right.Brand, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(left.IconKey, right.IconKey, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(NormalizeIdentityModel(left.ModelName), NormalizeIdentityModel(right.ModelName), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(
            HardwareNameNormalizer.Normalize(left.NormalizedName),
            HardwareNameNormalizer.Normalize(right.NormalizedName),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeIdentityModel(string value)
    {
        var normalized = HardwareNameNormalizer.Normalize(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        normalized = RevisionSuffixRegex.Replace(normalized, " ");
        return Regex.Replace(normalized, @"\s+", " ").Trim();
    }
}
