using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RegProbe.App.Services;

internal static class CrashReportStore
{
    private static readonly string CrashLogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RegProbe", "CrashLogs");

    public static void EnsureDirectory()
    {
        Directory.CreateDirectory(CrashLogDirectory);
    }

    public static async Task SaveAsync(CrashReport report)
    {
        try
        {
            EnsureDirectory();
            var fileName = $"crash_{report.Timestamp:yyyyMMdd_HHmmss}_{report.Id[..8]}.json";
            var filePath = Path.Combine(CrashLogDirectory, fileName);

            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);

            Debug.WriteLine($"Crash report saved: {filePath}");
            CleanupOldReports(50);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save crash report: {ex.Message}");
        }
    }

    public static IEnumerable<string> GetReportFiles()
    {
        if (!Directory.Exists(CrashLogDirectory))
        {
            return Enumerable.Empty<string>();
        }

        return Directory.GetFiles(CrashLogDirectory, "crash_*.json")
            .OrderByDescending(File.GetCreationTime);
    }

    private static void CleanupOldReports(int keepCount)
    {
        try
        {
            var files = Directory.GetFiles(CrashLogDirectory, "crash_*.json")
                .OrderByDescending(File.GetCreationTime)
                .Skip(keepCount)
                .ToList();

            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
        catch
        {
            // Cleanup should never interfere with crash capture.
        }
    }
}
