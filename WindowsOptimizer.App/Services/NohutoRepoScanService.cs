using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Infrastructure;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// Checks nohuto/win-registry changes and stores a local analysis snapshot.
/// The app consumes local state; remote access is only used during this check.
/// </summary>
public sealed class NohutoRepoScanService : IDisposable
{
    private const string Owner = "nohuto";
    private const string Repo = "win-registry";
    private readonly AppPaths _paths;
    private readonly HttpClient _httpClient;
    private bool _disposed;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public NohutoRepoScanService(AppPaths paths)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "WindowsOptimizer-NohutoScan");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    }

    public async Task<NohutoRepoScanResult> CheckAndAnalyzeAsync(CancellationToken ct)
    {
        try
        {
            _paths.EnsureDirectories();

            var previous = LoadState();
            var latest = await GetLatestCommitAsync(ct);
            if (!latest.HasValue || string.IsNullOrWhiteSpace(latest.Value.Sha))
            {
                return new NohutoRepoScanResult
                {
                    CheckedSuccessfully = false,
                    HasNewCommit = false,
                    Summary = "Nohuto scan unavailable (commit metadata).",
                    Analysis = previous.LastAnalysis
                };
            }

            var latestSha = latest.Value.Sha;
            var latestDate = latest.Value.Date;

            if (!string.IsNullOrWhiteSpace(previous.LastSeenCommitSha) &&
                string.Equals(previous.LastSeenCommitSha, latestSha, StringComparison.OrdinalIgnoreCase))
            {
                previous.LastCheckedAtUtc = DateTimeOffset.UtcNow;
                previous.LastSeenCommitDateUtc = latestDate;
                SaveState(previous);

                return new NohutoRepoScanResult
                {
                    CheckedSuccessfully = true,
                    HasNewCommit = false,
                    LatestCommitSha = latestSha,
                    LatestCommitDateUtc = latestDate,
                    Summary = $"Nohuto unchanged ({ShortSha(latestSha)}).",
                    Analysis = previous.LastAnalysis
                };
            }

            var changedFiles = new List<NohutoChangedFile>();
            if (!string.IsNullOrWhiteSpace(previous.LastSeenCommitSha))
            {
                changedFiles = await GetCompareFilesAsync(previous.LastSeenCommitSha, latestSha, ct);
            }
            else
            {
                changedFiles = await GetCommitFilesAsync(latestSha, ct);
            }

            var analysis = NohutoChangeAnalyzer.Analyze(changedFiles);
            var summary = BuildSummary(latestSha, analysis);

            var updatedState = new NohutoRepoScanState
            {
                LastSeenCommitSha = latestSha,
                LastSeenCommitDateUtc = latestDate,
                LastCheckedAtUtc = DateTimeOffset.UtcNow,
                LastAnalysis = analysis
            };

            SaveState(updatedState);
            SaveReport(latestSha, latestDate, analysis, changedFiles);

            return new NohutoRepoScanResult
            {
                CheckedSuccessfully = true,
                HasNewCommit = true,
                LatestCommitSha = latestSha,
                LatestCommitDateUtc = latestDate,
                Summary = summary,
                Analysis = analysis
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NohutoRepoScan] Failed: {ex.Message}");
            return new NohutoRepoScanResult
            {
                CheckedSuccessfully = false,
                HasNewCommit = false,
                Summary = $"Nohuto scan failed: {ex.Message}"
            };
        }
    }

    private async Task<(string Sha, DateTimeOffset Date)?> GetLatestCommitAsync(CancellationToken ct)
    {
        var url = $"https://api.github.com/repos/{Owner}/{Repo}/commits/main";
        using var stream = await _httpClient.GetStreamAsync(url, ct);
        var payload = await JsonSerializer.DeserializeAsync<GitHubCommitEnvelope>(stream, JsonOptions, ct);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Sha))
        {
            return null;
        }

        return (payload.Sha, payload.Commit.Committer.Date);
    }

    private async Task<List<NohutoChangedFile>> GetCompareFilesAsync(string fromSha, string toSha, CancellationToken ct)
    {
        var url = $"https://api.github.com/repos/{Owner}/{Repo}/compare/{fromSha}...{toSha}";
        using var stream = await _httpClient.GetStreamAsync(url, ct);
        var payload = await JsonSerializer.DeserializeAsync<GitHubCompareEnvelope>(stream, JsonOptions, ct);
        if (payload?.Files is null)
        {
            return new List<NohutoChangedFile>();
        }

        var files = new List<NohutoChangedFile>(payload.Files.Count);
        foreach (var file in payload.Files)
        {
            files.Add(new NohutoChangedFile
            {
                Path = file.Filename,
                Additions = file.Additions,
                Deletions = file.Deletions
            });
        }

        return files;
    }

    private async Task<List<NohutoChangedFile>> GetCommitFilesAsync(string sha, CancellationToken ct)
    {
        var url = $"https://api.github.com/repos/{Owner}/{Repo}/commits/{sha}";
        using var stream = await _httpClient.GetStreamAsync(url, ct);
        var payload = await JsonSerializer.DeserializeAsync<GitHubCompareEnvelope>(stream, JsonOptions, ct);
        if (payload?.Files is null)
        {
            return new List<NohutoChangedFile>();
        }

        var files = new List<NohutoChangedFile>(payload.Files.Count);
        foreach (var file in payload.Files)
        {
            files.Add(new NohutoChangedFile
            {
                Path = file.Filename,
                Additions = file.Additions,
                Deletions = file.Deletions
            });
        }

        return files;
    }

    private NohutoRepoScanState LoadState()
    {
        try
        {
            if (!File.Exists(_paths.NohutoScanStateFilePath))
            {
                return new NohutoRepoScanState();
            }

            var json = File.ReadAllText(_paths.NohutoScanStateFilePath);
            var state = JsonSerializer.Deserialize<NohutoRepoScanState>(json, JsonOptions);
            return state ?? new NohutoRepoScanState();
        }
        catch
        {
            return new NohutoRepoScanState();
        }
    }

    private void SaveState(NohutoRepoScanState state)
    {
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(_paths.NohutoScanStateFilePath, json);
    }

    private void SaveReport(string sha, DateTimeOffset date, NohutoChangeAnalysis analysis, IReadOnlyList<NohutoChangedFile> files)
    {
        var report = new
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            CommitSha = sha,
            CommitDateUtc = date,
            Analysis = analysis,
            ChangedFiles = files
        };

        var json = JsonSerializer.Serialize(report, JsonOptions);
        File.WriteAllText(_paths.NohutoAnalysisReportPath, json);
    }

    private static string BuildSummary(string sha, NohutoChangeAnalysis analysis)
    {
        var topCategory = analysis.TopCategories.Count > 0
            ? analysis.TopCategories[0].Category
            : "Misc";

        return $"Nohuto update {ShortSha(sha)}: {analysis.TotalChangedFiles} files, top impact {topCategory}.";
    }

    private static string ShortSha(string? sha)
    {
        if (string.IsNullOrWhiteSpace(sha))
        {
            return "unknown";
        }

        return sha.Length <= 8 ? sha : sha[..8];
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _httpClient.Dispose();
    }
}
