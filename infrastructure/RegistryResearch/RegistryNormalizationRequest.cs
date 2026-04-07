using System.Collections.Generic;

namespace RegProbe.Infrastructure.RegistryResearch;

public sealed record RegistryNormalizationRequest(
    string InputPath,
    string RunId,
    string SourceTool,
    string CapturePhase,
    IReadOnlyList<string>? EvidenceRefs = null);
