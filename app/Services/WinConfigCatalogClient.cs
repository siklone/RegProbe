using System.Net.Http;
using System.Text.Json;

namespace RegProbe.App.Services;

internal sealed class WinConfigCatalogClient
{
    private const string Owner = "nohuto";
    private const string Repo = "win-config";
    private const string Branch = "main";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly HttpClient _httpClient;

    public WinConfigCatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<(string Sha, DateTimeOffset Date)> GetLatestCommitAsync(CancellationToken ct)
    {
        var url = $"https://api.github.com/repos/{Owner}/{Repo}/commits/{Branch}";
        using var stream = await _httpClient.GetStreamAsync(url, ct);
        var payload = await JsonSerializer.DeserializeAsync<GitHubCommitEnvelope>(stream, JsonOptions, ct);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Sha))
        {
            throw new InvalidOperationException("Latest win-config commit metadata unavailable.");
        }

        return (payload.Sha, payload.Commit.Committer.Date);
    }

    public async Task<List<WinConfigCatalogCategory>> LoadCategoriesAsync(CancellationToken ct)
    {
        var topLevelEntries = await GetDirectoryContentsAsync(string.Empty, ct);
        var categoryDirectories = topLevelEntries
            .Where(static entry => string.Equals(entry.Type, "dir", StringComparison.OrdinalIgnoreCase))
            .Where(static entry => !entry.Name.StartsWith(".", StringComparison.Ordinal))
            .OrderBy(static entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var categories = new List<WinConfigCatalogCategory>();

        foreach (var directory in categoryDirectories)
        {
            var files = await GetDirectoryContentsRecursiveAsync(directory.Path, ct);
            var descriptionFile = files.FirstOrDefault(static entry =>
                string.Equals(entry.Name, "desc.md", StringComparison.OrdinalIgnoreCase));
            var markdown = descriptionFile?.DownloadUrl is { Length: > 0 }
                ? await GetRawTextAsync(descriptionFile.DownloadUrl, ct)
                : string.Empty;
            var topics = WinConfigCatalogParser.ExtractTopLevelTopics(markdown);

            categories.Add(BuildCategory(directory, files, markdown, topics));
        }

        return categories;
    }

    private async Task<List<GitHubContentEntry>> GetDirectoryContentsRecursiveAsync(string path, CancellationToken ct)
    {
        var results = new List<GitHubContentEntry>();
        var queue = new Queue<string>();
        queue.Enqueue(path);

        while (queue.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var current = queue.Dequeue();
            var entries = await GetDirectoryContentsAsync(current, ct);
            foreach (var entry in entries)
            {
                if (string.Equals(entry.Type, "dir", StringComparison.OrdinalIgnoreCase))
                {
                    queue.Enqueue(entry.Path);
                }
                else
                {
                    results.Add(entry);
                }
            }
        }

        return results;
    }

    private async Task<List<GitHubContentEntry>> GetDirectoryContentsAsync(string path, CancellationToken ct)
    {
        var url = string.IsNullOrWhiteSpace(path)
            ? $"https://api.github.com/repos/{Owner}/{Repo}/contents"
            : $"https://api.github.com/repos/{Owner}/{Repo}/contents/{path}";
        using var stream = await _httpClient.GetStreamAsync(url, ct);
        var payload = await JsonSerializer.DeserializeAsync<List<GitHubContentEntry>>(stream, JsonOptions, ct);
        return payload ?? new List<GitHubContentEntry>();
    }

    private async Task<string> GetRawTextAsync(string url, CancellationToken ct)
        => await _httpClient.GetStringAsync(url, ct);

    private static WinConfigCatalogCategory BuildCategory(
        GitHubContentEntry directory,
        IReadOnlyList<GitHubContentEntry> files,
        string markdown,
        IReadOnlyList<string> topics)
    {
        var documentationCount = 0;
        var scriptCount = 0;
        var assetCount = 0;

        foreach (var file in files.Where(static entry => string.Equals(entry.Type, "file", StringComparison.OrdinalIgnoreCase)))
        {
            switch (WinConfigCatalogParser.ClassifyFile(file.Path))
            {
                case WinConfigCatalogFileKind.Documentation:
                    documentationCount++;
                    break;
                case WinConfigCatalogFileKind.Script:
                    scriptCount++;
                    break;
                case WinConfigCatalogFileKind.Asset:
                    assetCount++;
                    break;
            }
        }

        var descriptionFile = files.FirstOrDefault(static entry =>
            string.Equals(entry.Name, "desc.md", StringComparison.OrdinalIgnoreCase));

        return new WinConfigCatalogCategory
        {
            Id = directory.Name,
            DisplayName = FormatDisplayName(directory.Name),
            SourceUrl = directory.HtmlUrl,
            DescriptionUrl = descriptionFile?.HtmlUrl ?? directory.HtmlUrl,
            Description = Truncate(WinConfigCatalogParser.ExtractLeadParagraph(markdown), 280),
            TopicCount = topics.Count,
            FileCount = files.Count(static entry => string.Equals(entry.Type, "file", StringComparison.OrdinalIgnoreCase)),
            DocumentationFileCount = documentationCount,
            ScriptFileCount = scriptCount,
            AssetFileCount = assetCount,
            Topics = topics
        };
    }

    private static string FormatDisplayName(string categoryName)
    {
        return string.Join(" ", categoryName
            .Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Length == 0
                ? string.Empty
                : char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..(maxLength - 3)].TrimEnd() + "...";
    }
}
