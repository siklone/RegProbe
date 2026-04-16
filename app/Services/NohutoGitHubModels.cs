using System.Text.Json.Serialization;

namespace RegProbe.App.Services;

internal sealed class GitHubCommitEnvelope
{
    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;

    [JsonPropertyName("commit")]
    public GitHubCommitDetails Commit { get; set; } = new();
}

internal sealed class GitHubCommitDetails
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("committer")]
    public GitHubCommitter Committer { get; set; } = new();
}

internal sealed class GitHubCommitter
{
    [JsonPropertyName("date")]
    public DateTimeOffset Date { get; set; }
}

internal sealed class GitHubCompareEnvelope
{
    [JsonPropertyName("files")]
    public List<GitHubChangedFileEnvelope> Files { get; set; } = new();
}

internal sealed class GitHubChangedFileEnvelope
{
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    [JsonPropertyName("additions")]
    public int Additions { get; set; }

    [JsonPropertyName("deletions")]
    public int Deletions { get; set; }
}
