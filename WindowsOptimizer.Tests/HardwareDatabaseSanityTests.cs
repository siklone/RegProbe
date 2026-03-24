using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using WindowsOptimizer.App.HardwareDb;

public sealed class HardwareDatabaseSanityTests
{
    private static string HardwareDbRoot => Path.Combine(AppContext.BaseDirectory, "Assets", "HardwareDb");

    [Fact]
    public void GeneratedHardwareDatabases_KeepStableNamesAndAliases()
    {
        Assert.True(Directory.Exists(HardwareDbRoot), $"Hardware DB root not found: {HardwareDbRoot}");

        var problems = new List<string>();
        var seenNormalizedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dbFiles = Directory.GetFiles(HardwareDbRoot, "hardware_db_*.json", SearchOption.TopDirectoryOnly)
            .Where(static path => !path.EndsWith("_schema.json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.NotEmpty(dbFiles);

        foreach (var dbFile in dbFiles)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(dbFile));
            if (!document.RootElement.TryGetProperty("items", out var itemsElement) ||
                itemsElement.ValueKind != JsonValueKind.Array)
            {
                problems.Add($"{Path.GetFileName(dbFile)}: missing items array");
                continue;
            }

            foreach (var item in itemsElement.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? "<missing-id>" : "<missing-id>";
                var normalizedName = item.TryGetProperty("normalizedName", out var normalizedNameElement)
                    ? normalizedNameElement.GetString() ?? string.Empty
                    : string.Empty;
                var dedupeKey = $"{Path.GetFileName(dbFile)}::{HardwareNameNormalizer.Normalize(normalizedName)}";

                if (string.IsNullOrWhiteSpace(normalizedName))
                {
                    problems.Add($"{Path.GetFileName(dbFile)}:{id}: normalizedName is empty");
                }
                else if (!seenNormalizedNames.Add(dedupeKey))
                {
                    problems.Add($"{Path.GetFileName(dbFile)}:{id}: duplicate normalizedName '{normalizedName}'");
                }

                if (!item.TryGetProperty("aliases", out var aliasesElement) || aliasesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var aliasElement in aliasesElement.EnumerateArray())
                {
                    var alias = aliasElement.GetString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(alias))
                    {
                        problems.Add($"{Path.GetFileName(dbFile)}:{id}: contains empty alias");
                        continue;
                    }

                    var normalizedAlias = HardwareNameNormalizer.Normalize(alias);
                    if (string.IsNullOrWhiteSpace(normalizedAlias))
                    {
                        problems.Add($"{Path.GetFileName(dbFile)}:{id}: alias '{alias}' normalizes to empty");
                    }
                }
            }
        }

        Assert.True(
            problems.Count == 0,
            "Hardware DB sanity failures:" + Environment.NewLine + string.Join(Environment.NewLine, problems.Take(25)));
    }
}
