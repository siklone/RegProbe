using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenTraceProject.App.Services;

/// <summary>
/// Auto-update service that checks GitHub releases for new versions.
/// </summary>
public class AutoUpdateService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _owner;
    private readonly string _repo;
    private bool _isDisposed;

    public AutoUpdateService(string owner = "siklone", string repo = "Open-Trace-Project")
    {
        _owner = owner;
        _repo = repo;
        _httpClient = new HttpClient
        {
            DefaultRequestHeaders =
            {
                { "User-Agent", "OpenTraceProject-AutoUpdate" },
                { "Accept", "application/vnd.github.v3+json" }
            }
        };
    }

    public Version CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);

    /// <summary>
    /// Check for available updates from GitHub releases.
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
            var response = await _httpClient.GetStringAsync(url);
            var release = JsonSerializer.Deserialize<GitHubRelease>(response);

            if (release == null) return null;

            var latestVersion = ParseVersion(release.TagName);
            if (latestVersion == null || latestVersion <= CurrentVersion) return null;

            var downloadUrl = release.Assets?.FirstOrDefault(a => 
                a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                a.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))?.BrowserDownloadUrl;

            return new UpdateInfo
            {
                CurrentVersion = CurrentVersion,
                LatestVersion = latestVersion,
                ReleaseNotes = release.Body ?? "",
                DownloadUrl = downloadUrl ?? "",
                ReleaseDate = release.PublishedAt,
                ReleaseName = release.Name ?? $"v{latestVersion}"
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update check failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Download and install the update.
    /// </summary>
    public async Task<bool> DownloadAndInstallAsync(UpdateInfo update, IProgress<double>? progress = null)
    {
        if (string.IsNullOrEmpty(update.DownloadUrl)) return false;

        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"OpenTraceProject_Update_{update.LatestVersion}.exe");

            // Download with progress
            using var response = await _httpClient.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = File.Create(tempPath);
            
            var buffer = new byte[8192];
            long downloadedBytes = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                downloadedBytes += bytesRead;
                
                if (totalBytes > 0)
                    progress?.Report((double)downloadedBytes / totalBytes * 100);
            }

            // Launch installer and exit app
            Process.Start(new ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true,
                Verb = "runas"
            });

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update download failed: {ex.Message}");
            return false;
        }
    }

    private static Version? ParseVersion(string? tagName)
    {
        if (string.IsNullOrEmpty(tagName)) return null;
        
        var versionString = tagName.TrimStart('v', 'V');
        return Version.TryParse(versionString, out var version) ? version : null;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _httpClient.Dispose();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Information about an available update.
/// </summary>
public class UpdateInfo
{
    public Version CurrentVersion { get; init; } = new(1, 0, 0);
    public Version LatestVersion { get; init; } = new(1, 0, 0);
    public string ReleaseNotes { get; init; } = "";
    public string DownloadUrl { get; init; } = "";
    public DateTime ReleaseDate { get; init; }
    public string ReleaseName { get; init; } = "";
    
    public bool IsUpdateAvailable => LatestVersion > CurrentVersion;
}

/// <summary>
/// GitHub release API response model.
/// </summary>
internal class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("body")]
    public string? Body { get; set; }
    
    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }
    
    [JsonPropertyName("assets")]
    public GitHubAsset[]? Assets { get; set; }
}

internal class GitHubAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = "";
    
    [JsonPropertyName("size")]
    public long Size { get; set; }
}
