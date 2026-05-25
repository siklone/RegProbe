using System.Collections.Generic;
using System;
using System.Windows.Media;
using RegProbe.App.Services;

namespace RegProbe.App.ViewModels;

public sealed class TweakProofLaneViewModel
{
    public TweakProofLaneViewModel(
        string key,
        string label,
        string state,
        string summary,
        string primarySourceText,
        IReadOnlyList<ReferenceLink> links,
        Brush accentBrush)
    {
        Key = key;
        Label = label;
        State = state;
        Summary = summary ?? string.Empty;
        PrimarySourceText = primarySourceText ?? string.Empty;
        Links = links ?? Array.Empty<ReferenceLink>();
        AccentBrush = accentBrush;
    }

    public string Key { get; }

    public string Label { get; }

    public string State { get; }

    public string Summary { get; }

    public string PrimarySourceText { get; }

    public IReadOnlyList<ReferenceLink> Links { get; }

    public Brush AccentBrush { get; }

    public string StateText => TweakResearchPresentation.BuildProofStateText(State);

    public Brush StateBackgroundBrush => TweakResearchPresentation.GetProofStateBackgroundBrush(State);

    public Brush StateBorderBrush => TweakResearchPresentation.GetProofStateBorderBrush(State);

    public Brush StateForegroundBrush => TweakResearchPresentation.GetProofStateForegroundBrush(State);

    public bool HasSummary => !string.IsNullOrWhiteSpace(Summary);

    public bool HasPrimarySourceText => !string.IsNullOrWhiteSpace(PrimarySourceText);

    public bool HasLinks => Links.Count > 0;

    public bool HasSourceBoundaryCallout =>
        string.Equals(Key, "source", StringComparison.OrdinalIgnoreCase)
        && PublicEvidenceLinkPolicy.IsNoLocalSourceSummary(Summary);

    public string SourceBoundaryTitle => "Local source proof is not attached";

    public string SourceBoundaryDetail =>
        "Catalog context may explain naming or navigation, but it is not pseudocode/source proof and not value-semantics proof. Use Docs, Runtime, and Rollback for app safety until a RegProbe-controlled local source mirror exists.";

    public string EmptyMessage => $"{Label} proof is still pending.";
}
