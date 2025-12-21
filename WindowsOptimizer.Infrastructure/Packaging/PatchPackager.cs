using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Infrastructure.Packaging;

public sealed class PatchPackager
{
    public async Task<string> CreatePatchArchiveAsync(
        string outputDirectory,
        string tweakLogPath,
        string appLogPath,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory is required.", nameof(outputDirectory));
        }

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var archiveName = $"WindowsOptimizer-Patch-{timestamp}.zip";
        var archivePath = Path.Combine(outputDirectory, archiveName);

        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);

        // Add README to explain the patch
        var readmeEntry = archive.CreateEntry("README.txt");
        using (var writer = new StreamWriter(readmeEntry.Open(), Encoding.UTF8))
        {
            await writer.WriteLineAsync("Windows Optimizer Suite - Patch Export");
            await writer.WriteLineAsync($"Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("This archive contains logs from tweak execution.");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("Contents:");
            await writer.WriteLineAsync("  - tweak-log.csv: Detailed tweak execution log");
            await writer.WriteLineAsync("  - app.log: Application event log");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("To review:");
            await writer.WriteLineAsync("  1. Extract this archive");
            await writer.WriteLineAsync("  2. Open tweak-log.csv in a spreadsheet application");
            await writer.WriteLineAsync("  3. Review app.log for detailed execution trace");
        }

        // Add tweak log if it exists
        if (File.Exists(tweakLogPath))
        {
            archive.CreateEntryFromFile(tweakLogPath, "tweak-log.csv");
        }

        // Add app log if it exists
        if (File.Exists(appLogPath))
        {
            archive.CreateEntryFromFile(appLogPath, "app.log");
        }

        return archivePath;
    }

    public async Task<PatchManifest> GenerateManifestAsync(
        string tweakLogPath,
        CancellationToken ct)
    {
        if (!File.Exists(tweakLogPath))
        {
            return new PatchManifest(
                DateTimeOffset.UtcNow,
                0,
                0,
                0);
        }

        var lines = await File.ReadAllLinesAsync(tweakLogPath, ct);

        // Skip header line
        var dataLines = lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

        var successCount = dataLines.Count(l => l.Contains(",Applied,") || l.Contains(",Verified,"));
        var failureCount = dataLines.Count(l => l.Contains(",Failed,"));
        var totalCount = dataLines.Length;

        return new PatchManifest(
            DateTimeOffset.UtcNow,
            totalCount,
            successCount,
            failureCount);
    }
}

public sealed record PatchManifest(
    DateTimeOffset CreatedAt,
    int TotalTweaks,
    int SuccessCount,
    int FailureCount);
