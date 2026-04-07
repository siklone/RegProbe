using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RegProbe.Infrastructure.RegistryResearch;

public sealed record NormalizedRegistryBundle
{
    [JsonPropertyName("$schema")]
    public string Schema { get; init; } = "registry-research-framework/schemas/normalized-registry-bundle.schema.json";

    [JsonPropertyName("run_id")]
    public required string RunId { get; init; }

    [JsonPropertyName("source_tool")]
    public required string SourceTool { get; init; }

    [JsonPropertyName("capture_phase")]
    public required string CapturePhase { get; init; }

    [JsonPropertyName("generated_utc")]
    public required string GeneratedUtc { get; init; }

    [JsonPropertyName("normalizer_name")]
    public required string NormalizerName { get; init; }

    [JsonPropertyName("input_path")]
    public required string InputPath { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = "ok";

    [JsonPropertyName("error_kind")]
    public string? ErrorKind { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<string> Errors { get; init; } = [];

    [JsonPropertyName("event_count")]
    public int EventCount { get; init; }

    [JsonPropertyName("filtered_event_count")]
    public int FilteredEventCount { get; init; }

    [JsonPropertyName("evidence_refs")]
    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    [JsonPropertyName("events")]
    public IReadOnlyList<NormalizedRegistryEvent> Events { get; init; } = [];
}
