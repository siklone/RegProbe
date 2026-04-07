using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RegProbe.Infrastructure.RegistryResearch;

public sealed record NormalizedRegistryEvent
{
    [JsonPropertyName("run_id")]
    public required string RunId { get; init; }

    [JsonPropertyName("source_tool")]
    public required string SourceTool { get; init; }

    [JsonPropertyName("capture_phase")]
    public required string CapturePhase { get; init; }

    [JsonPropertyName("process_name")]
    public string? ProcessName { get; init; }

    [JsonPropertyName("pid")]
    public int? Pid { get; init; }

    [JsonPropertyName("operation")]
    public required string Operation { get; init; }

    [JsonPropertyName("timestamp_utc")]
    public string? TimestampUtc { get; init; }

    [JsonPropertyName("hive")]
    public string? Hive { get; init; }

    [JsonPropertyName("key_path")]
    public string? KeyPath { get; init; }

    [JsonPropertyName("value_name")]
    public string? ValueName { get; init; }

    [JsonPropertyName("value_type")]
    public string? ValueType { get; init; }

    [JsonPropertyName("data_text")]
    public string? DataText { get; init; }

    [JsonPropertyName("result")]
    public string? Result { get; init; }

    [JsonPropertyName("evidence_refs")]
    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];
}
