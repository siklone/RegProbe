namespace RegProbe.App.Services;

public sealed class TweakEvidenceClassCatalog
{
    public string GeneratedUtc { get; set; } = string.Empty;
    public TweakEvidenceClassSummary Summary { get; set; } = new();
    public Dictionary<string, TweakEvidenceClassDefinition> Classes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<TweakEvidenceClassEntry> Entries { get; set; } = new();
}

public sealed class TweakEvidenceClassSummary
{
    public int TotalRecords { get; set; }
    public Dictionary<string, int> ClassCounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> ActionStateCounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class TweakEvidenceClassDefinition
{
    public string Label { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class TweakEvidenceClassEntry
{
    public string RecordId { get; set; } = string.Empty;
    public string TweakId { get; set; } = string.Empty;
    public string RecordStatus { get; set; } = string.Empty;
    public string EvidenceClass { get; set; } = string.Empty;
    public string ClassLabel { get; set; } = string.Empty;
    public string ClassTitle { get; set; } = string.Empty;
    public string ClassDescription { get; set; } = string.Empty;
    public bool ShowInApp { get; set; }
    public bool IsActionable { get; set; }
    public bool IsArchived { get; set; }
    public string ActionState { get; set; } = string.Empty;
    public string GatingReason { get; set; } = string.Empty;
    public string Confidence { get; set; } = string.Empty;
    public string AppMappingStatus { get; set; } = string.Empty;
    public bool RestoreStoryKnown { get; set; }
    public TweakEvidenceProofBlock? ValidatedSemantics { get; set; }
    public TweakEvidenceProofBlock? RuntimeProof { get; set; }
    public TweakEvidenceProofBlock? UpstreamLineage { get; set; }

    public static TweakEvidenceClassEntry CreateFallback(string tweakId) => new()
    {
        RecordId = tweakId,
        TweakId = tweakId,
        RecordStatus = "validated",
        EvidenceClass = "D",
        ClassLabel = "Class D",
        ClassTitle = "Key Known, Value Semantics Unknown",
        ClassDescription = "No derived evidence-class entry was found for this tweak yet.",
        ShowInApp = true,
        IsActionable = false,
        IsArchived = false,
        ActionState = "research-gated",
        GatingReason = "No derived evidence class is loaded for this tweak yet.",
        ValidatedSemantics = new TweakEvidenceProofBlock { Summary = "No derived semantics summary is available yet." },
        RuntimeProof = new TweakEvidenceProofBlock { Summary = "No derived runtime summary is available yet." },
        UpstreamLineage = new TweakEvidenceProofBlock { Summary = "No derived upstream lineage summary is available yet." },
    };
}

public sealed class TweakEvidenceProofBlock
{
    public string Summary { get; set; } = string.Empty;
    public bool HasValidationProof { get; set; }
    public bool HasSemanticsEvidence { get; set; }
    public bool NeedsVmValidation { get; set; }
    public bool HasRuntimeEvidence { get; set; }
    public bool HasNohutoLineage { get; set; }
    public List<TweakEvidenceLink> Links { get; set; } = new();
    public string PrimarySourceText { get; set; } = string.Empty;
}

public sealed class TweakEvidenceLink
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}
