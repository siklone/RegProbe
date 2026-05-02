using System.Text.Json;
using RegProbe.Application.Services;

namespace RegProbe.Tests;

public sealed class ResearchAppSurfaceCompletenessTests
{
    private const string ResearchProviderSource = "app/Services/TweakProviders/ResearchAppSurfaceTweakProvider.cs";
    private const string ResearchProviderCategoryName = "Research App Surface";
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    private static readonly string RecordsRoot = Path.Combine(RepoRoot, "research", "records");
    private static readonly string ManifestPath = Path.Combine(
        RepoRoot,
        "Docs",
        "research",
        "app-surface",
        "validated-registry-values.json");
    private static readonly string IntentionalNotMappedLedgerPath = Path.Combine(
        RepoRoot,
        "Docs",
        "research",
        "app-surface",
        "intentional-not-mapped-records.json");
    private static readonly string AppOnlyCatalogLedgerPath = Path.Combine(
        RepoRoot,
        "Docs",
        "research",
        "app-surface",
        "app-only-catalog-tweaks.json");

    [Fact]
    public void Catalog_Surfaces_All_ResearchProvider_MatchesResearch_Records()
    {
        var catalog = new TweakCatalogService();

        var missing = EnumerateRecordMetadata()
            .Where(record =>
                string.Equals(record.Status, "matches-research", StringComparison.Ordinal) &&
                string.Equals(record.ProviderSource, ResearchProviderSource, StringComparison.Ordinal))
            .Select(record => record.RecordId)
            .Where(recordId => catalog.FindById(recordId) is null)
            .OrderBy(recordId => recordId, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missing.Length == 0,
            "Missing research-backed cards: " + string.Join(", ", missing));
    }

    [Fact]
    public void Catalog_Surfaces_All_Legacy_ResearchTracked_Cards()
    {
        var catalog = new TweakCatalogService();

        var missing = EnumerateRecordMetadata()
            .Where(record =>
                !string.Equals(record.RecordStatus, "deprecated", StringComparison.Ordinal) &&
                record.Status is "unknown" or "partially-matches" or "mismatch-suspected" &&
                !string.IsNullOrWhiteSpace(record.ProviderSource) &&
                record.WriteCount > 0)
            .Select(record => record.RecordId)
            .Where(recordId => catalog.FindById(recordId) is null)
            .OrderBy(recordId => recordId, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missing.Length == 0,
            "Missing legacy research-tracked cards: " + string.Join(", ", missing));
    }

    [Fact]
    public void Catalog_Ids_Are_Covered_By_Record_Corpus_Or_AppOnly_Ledger()
    {
        var catalog = new TweakCatalogService();
        var catalogIds = catalog.GetAll()
            .Select(entry => entry.Tweak.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToArray();

        var duplicateIds = catalogIds
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(
            duplicateIds.Length == 0,
            "Duplicate live tweak ids detected: " + string.Join(", ", duplicateIds));

        var distinctCatalogIds = catalogIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var recordIds = EnumerateRecordMetadata()
            .Select(record => record.RecordId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var actualAppOnlyIds = distinctCatalogIds
            .Except(recordIds, StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var expectedAppOnly = LoadAppOnlyCatalogLedger();

        var invalidLedgerEntries = expectedAppOnly.Values
            .Where(entry =>
                string.IsNullOrWhiteSpace(entry.Reason) ||
                string.IsNullOrWhiteSpace(entry.ProviderSource) ||
                string.IsNullOrWhiteSpace(entry.Notes))
            .Select(entry => entry.TweakId)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(
            invalidLedgerEntries.Length == 0,
            "App-only ledger entries must include reason/provider_source/notes: " + string.Join(", ", invalidLedgerEntries));

        Assert.Equal(
            expectedAppOnly.Keys.OrderBy(id => id, StringComparer.OrdinalIgnoreCase),
            actualAppOnlyIds);
    }

    [Fact]
    public void ResearchProvider_Category_Exactly_Matches_Manifest_Entries()
    {
        var catalog = new TweakCatalogService();
        var manifestIds = LoadManifestIds();
        var categoryIds = catalog.GetAll()
            .Where(entry => string.Equals(entry.Category, ResearchProviderCategoryName, StringComparison.Ordinal))
            .Select(entry => entry.Tweak.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(manifestIds, categoryIds);
    }

    [Fact]
    public void Intentional_NotMapped_Ledger_Matches_CheckedIn_Record_Metadata()
    {
        var expected = LoadIntentionalNotMappedLedger();
        var actual = EnumerateRecordMetadata()
            .Where(record => string.Equals(record.Status, "not-mapped", StringComparison.Ordinal))
            .ToDictionary(record => record.RecordId, StringComparer.Ordinal);

        Assert.Equal(
            expected.Keys.OrderBy(recordId => recordId, StringComparer.Ordinal),
            actual.Keys.OrderBy(recordId => recordId, StringComparer.Ordinal));

        foreach (var (recordId, expectedEntry) in expected)
        {
            var actualEntry = actual[recordId];
            Assert.Equal(expectedEntry.ProviderSource, actualEntry.ProviderSource);
            Assert.Equal(expectedEntry.Notes, actualEntry.Notes);
        }
    }

    private static string[] LoadManifestIds()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ManifestPath));
        var ids = new List<string>();

        foreach (var category in document.RootElement.GetProperty("categories").EnumerateObject())
        {
            foreach (var entry in category.Value.GetProperty("entries").EnumerateArray())
            {
                var id = entry.GetProperty("id").GetString();
                if (!string.IsNullOrWhiteSpace(id))
                {
                    ids.Add(id);
                }
            }
        }

        ids.Sort(StringComparer.Ordinal);
        return ids.ToArray();
    }

    private static IReadOnlyDictionary<string, IntentionalNotMappedLedgerEntry> LoadIntentionalNotMappedLedger()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(IntentionalNotMappedLedgerPath));
        var entries = new Dictionary<string, IntentionalNotMappedLedgerEntry>(StringComparer.Ordinal);

        foreach (var entry in document.RootElement.GetProperty("records").EnumerateArray())
        {
            var recordId = entry.GetProperty("record_id").GetString() ?? string.Empty;
            entries.Add(
                recordId,
                new IntentionalNotMappedLedgerEntry(
                    recordId,
                    entry.GetProperty("reason").GetString() ?? string.Empty,
                    entry.GetProperty("provider_source").GetString() ?? string.Empty,
                    entry.GetProperty("notes").GetString() ?? string.Empty));
        }

        return entries;
    }

    private static IReadOnlyDictionary<string, AppOnlyCatalogLedgerEntry> LoadAppOnlyCatalogLedger()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(AppOnlyCatalogLedgerPath));
        var entries = new Dictionary<string, AppOnlyCatalogLedgerEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in document.RootElement.GetProperty("tweaks").EnumerateArray())
        {
            var tweakId = entry.GetProperty("tweak_id").GetString() ?? string.Empty;
            entries.Add(
                tweakId,
                new AppOnlyCatalogLedgerEntry(
                    tweakId,
                    entry.GetProperty("reason").GetString() ?? string.Empty,
                    entry.GetProperty("provider_source").GetString() ?? string.Empty,
                    entry.GetProperty("notes").GetString() ?? string.Empty));
        }

        return entries;
    }

    private static IEnumerable<RecordMetadata> EnumerateRecordMetadata()
    {
        foreach (var path in Directory.EnumerateFiles(RecordsRoot, "*.json").OrderBy(path => path, StringComparer.Ordinal))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            var implementation = root.TryGetProperty("app_current_implementation", out var implementationElement)
                ? implementationElement
                : default;

            var recordId = root.TryGetProperty("record_id", out var recordIdElement)
                ? recordIdElement.GetString()
                : Path.GetFileNameWithoutExtension(path);
            var recordStatus = root.TryGetProperty("record_status", out var recordStatusElement)
                ? recordStatusElement.GetString()
                : string.Empty;
            var status = implementation.ValueKind == JsonValueKind.Object && implementation.TryGetProperty("status", out var statusElement)
                ? statusElement.GetString()
                : string.Empty;
            var providerSource = implementation.ValueKind == JsonValueKind.Object && implementation.TryGetProperty("provider_source", out var providerSourceElement)
                ? providerSourceElement.GetString()
                : string.Empty;
            var notes = implementation.ValueKind == JsonValueKind.Object && implementation.TryGetProperty("notes", out var notesElement)
                ? notesElement.GetString()
                : string.Empty;
            var writeCount = implementation.ValueKind == JsonValueKind.Object && implementation.TryGetProperty("writes", out var writesElement) && writesElement.ValueKind == JsonValueKind.Array
                ? writesElement.GetArrayLength()
                : 0;

            yield return new RecordMetadata(
                recordId ?? Path.GetFileNameWithoutExtension(path),
                recordStatus ?? string.Empty,
                status ?? string.Empty,
                providerSource ?? string.Empty,
                notes ?? string.Empty,
                writeCount);
        }
    }

    private sealed record RecordMetadata(string RecordId, string RecordStatus, string Status, string ProviderSource, string Notes, int WriteCount);

    private sealed record AppOnlyCatalogLedgerEntry(
        string TweakId,
        string Reason,
        string ProviderSource,
        string Notes);

    private sealed record IntentionalNotMappedLedgerEntry(
        string RecordId,
        string Reason,
        string ProviderSource,
        string Notes);
}
