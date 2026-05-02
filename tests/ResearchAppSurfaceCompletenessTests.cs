using System.Text.Json;
using RegProbe.Application.Services;

namespace RegProbe.Tests;

public sealed class ResearchAppSurfaceCompletenessTests
{
    private const string ResearchProviderSource = "app/Services/TweakProviders/ResearchAppSurfaceTweakProvider.cs";
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    private static readonly string RecordsRoot = Path.Combine(RepoRoot, "research", "records");
    private static readonly string IntentionalNotMappedLedgerPath = Path.Combine(
        RepoRoot,
        "Docs",
        "research",
        "app-surface",
        "intentional-not-mapped-records.json");

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
            var status = implementation.ValueKind == JsonValueKind.Object && implementation.TryGetProperty("status", out var statusElement)
                ? statusElement.GetString()
                : string.Empty;
            var providerSource = implementation.ValueKind == JsonValueKind.Object && implementation.TryGetProperty("provider_source", out var providerSourceElement)
                ? providerSourceElement.GetString()
                : string.Empty;
            var notes = implementation.ValueKind == JsonValueKind.Object && implementation.TryGetProperty("notes", out var notesElement)
                ? notesElement.GetString()
                : string.Empty;

            yield return new RecordMetadata(
                recordId ?? Path.GetFileNameWithoutExtension(path),
                status ?? string.Empty,
                providerSource ?? string.Empty,
                notes ?? string.Empty);
        }
    }

    private sealed record RecordMetadata(string RecordId, string Status, string ProviderSource, string Notes);

    private sealed record IntentionalNotMappedLedgerEntry(
        string RecordId,
        string Reason,
        string ProviderSource,
        string Notes);
}
