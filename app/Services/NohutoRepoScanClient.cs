using System.Net.Http;
using System.Text.Json;

namespace RegProbe.App.Services;

internal sealed class NohutoRepoScanClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly HttpClient _httpClient;

    public NohutoRepoScanClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<(string Sha, DateTimeOffset Date, string Message)?> GetLatestCommitAsync(
        NohutoRepositoryDefinition definition,
        CancellationToken ct)
    {
        var url = $"https://api.github.com/repos/{definition.Owner}/{definition.Repository}/commits/{definition.Branch}";
        using var stream = await _httpClient.GetStreamAsync(url, ct);
        var payload = await JsonSerializer.DeserializeAsync<GitHubCommitEnvelope>(stream, JsonOptions, ct);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Sha))
        {
            return null;
        }

        return (payload.Sha, payload.Commit.Committer.Date, payload.Commit.Message);
    }

    public async Task<List<NohutoChangedFile>> GetCompareFilesAsync(
        NohutoRepositoryDefinition definition,
        string fromSha,
        string toSha,
        CancellationToken ct)
    {
        var url = $"https://api.github.com/repos/{definition.Owner}/{definition.Repository}/compare/{fromSha}...{toSha}";
        using var stream = await _httpClient.GetStreamAsync(url, ct);
        var payload = await JsonSerializer.DeserializeAsync<GitHubCompareEnvelope>(stream, JsonOptions, ct);
        return ConvertFiles(payload);
    }

    public async Task<List<NohutoChangedFile>> GetCommitFilesAsync(
        NohutoRepositoryDefinition definition,
        string sha,
        CancellationToken ct)
    {
        var url = $"https://api.github.com/repos/{definition.Owner}/{definition.Repository}/commits/{sha}";
        using var stream = await _httpClient.GetStreamAsync(url, ct);
        var payload = await JsonSerializer.DeserializeAsync<GitHubCompareEnvelope>(stream, JsonOptions, ct);
        return ConvertFiles(payload);
    }

    private static List<NohutoChangedFile> ConvertFiles(GitHubCompareEnvelope? payload)
    {
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
}
