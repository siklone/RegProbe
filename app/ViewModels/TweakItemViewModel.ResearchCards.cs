using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using RegProbe.App.Services;
using RegProbe.Core;

namespace RegProbe.App.ViewModels;

public sealed partial class TweakItemViewModel
{
    private readonly List<ReferenceLink> _validatedSemanticsLinks = new();
    private readonly List<ReferenceLink> _runtimeProofLinks = new();
    private readonly List<ReferenceLink> _upstreamLineageLinks = new();

    public string ResearchAccentKey => TweakResearchPresentation.DetermineAccentKey(Id, Category);

    public string ResearchAccentLabel => ResearchAccentKey;

    public Brush ResearchAccentBrush => TweakResearchPresentation.GetAccentBrush(ResearchAccentKey);

    public string DefaultChoiceLabel =>
        _tweak is IChoiceTweak choiceTweak
            ? choiceTweak.DefaultChoiceLabel ?? string.Empty
            : string.Empty;

    public string WhatWeKnowSummary => TweakClaimBoundaryPresentation.BuildWhatWeKnowSummary(
        FriendlyDescription,
        ValidatedSemanticsSummary,
        DocsSnapshotText,
        RollbackSnapshotState,
        RollbackStoryText);

    public string WhatWeDoNotClaimSummary => TweakClaimBoundaryPresentation.BuildWhatWeDoNotClaimSummary(
        VerdictState,
        RuntimeSnapshotState,
        RuntimeProofSummary,
        UpstreamLineageSummary,
        PublicMutationGatingReason,
        IsMutationAllowed);

    public bool HasClaimBoundary =>
        !string.IsNullOrWhiteSpace(WhatWeKnowSummary)
        || !string.IsNullOrWhiteSpace(WhatWeDoNotClaimSummary);

    public Brush EvidenceTierBackgroundBrush => TweakResearchPresentation.GetTierBackgroundBrush(EvidenceClassId);

    public Brush EvidenceTierBorderBrush => TweakResearchPresentation.GetTierBorderBrush(EvidenceClassId);

    public Brush EvidenceTierForegroundBrush => TweakResearchPresentation.GetTierForegroundBrush(EvidenceClassId);

    public string ResearchStatusTone => TweakResearchPresentation.BuildResearchStatusTone(
        VerdictState,
        IsMutationAllowed,
        IsResearchGated,
        IsPromotionActionable,
        IsEvidenceClassActionable);

    public string ResearchStatusBadgeText => TweakResearchPresentation.BuildResearchStatusText(ResearchStatusTone);

    public Brush ResearchStatusBadgeBackgroundBrush => TweakResearchPresentation.GetResearchStatusBackgroundBrush(ResearchStatusTone);

    public Brush ResearchStatusBadgeBorderBrush => TweakResearchPresentation.GetResearchStatusBorderBrush(ResearchStatusTone);

    public Brush ResearchStatusBadgeForegroundBrush => TweakResearchPresentation.GetResearchStatusForegroundBrush(ResearchStatusTone);

    public string ResearchHoldMessage =>
        !string.IsNullOrWhiteSpace(PublicMutationGatingReason)
            ? PublicMutationGatingReason
            : !string.IsNullOrWhiteSpace(EvidenceClassGatingReason)
                ? EvidenceClassGatingReason
                : VerdictSummary;

    public bool HasResearchHoldMessage =>
        string.Equals(ResearchStatusTone, "intentional-hold", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(ResearchHoldMessage);

    public IReadOnlyList<TweakProofLaneViewModel> ProofLanes => BuildProofLanes();

    public IReadOnlyList<TweakProofBarViewModel> ProofBars => BuildProofBars();

    public IReadOnlyList<TweakValueSummaryRowViewModel> ValueSummaryRows => BuildValueSummaryRows();

    private IReadOnlyList<TweakProofLaneViewModel> BuildProofLanes()
    {
        var docsLinks = MergeLinks(
            _validatedSemanticsLinks,
            ReferenceLinks.Where(static link => link.Kind is ReferenceLinkKind.Docs or ReferenceLinkKind.Details));
        var runtimeLinks = MergeLinks(_runtimeProofLinks);
        var sourceLinks = MergeLinks(
            _upstreamLineageLinks,
            ReferenceLinks.Where(static link => link.Kind is ReferenceLinkKind.Source));
        var sourceSummary = PublicEvidenceLinkPolicy.SanitizeSourceSummary(
            string.IsNullOrWhiteSpace(UpstreamLineageSummary) ? ProvenanceSummary : UpstreamLineageSummary,
            sourceLinks.Count);

        return new List<TweakProofLaneViewModel>
        {
            new(
                "docs",
                "DOCS",
                DocsSnapshotState,
                string.IsNullOrWhiteSpace(ValidatedSemanticsSummary) ? DocsSnapshotText : ValidatedSemanticsSummary,
                ValidatedSemanticsSource,
                docsLinks,
                TweakResearchPresentation.GetProofAccentBrush("docs", ResearchAccentBrush)),
            new(
                "runtime",
                "RUNTIME",
                RuntimeSnapshotState,
                string.IsNullOrWhiteSpace(RuntimeProofSummary) ? RuntimeSnapshotText : RuntimeProofSummary,
                RuntimeProofSource,
                runtimeLinks,
                TweakResearchPresentation.GetProofAccentBrush("runtime", ResearchAccentBrush)),
            new(
                "source",
                "SOURCE",
                SourceSnapshotState,
                sourceSummary,
                UpstreamLineageSource,
                sourceLinks,
                TweakResearchPresentation.GetProofAccentBrush("source", ResearchAccentBrush)),
            new(
                "rollback",
                "ROLLBACK",
                RollbackSnapshotState,
                string.IsNullOrWhiteSpace(RollbackStoryText) ? RollbackSnapshotText : RollbackStoryText,
                _rollbackVerificationMethod,
                Array.Empty<ReferenceLink>(),
                TweakResearchPresentation.GetProofAccentBrush("rollback", ResearchAccentBrush))
        };
    }

    private IReadOnlyList<TweakProofBarViewModel> BuildProofBars()
    {
        return new List<TweakProofBarViewModel>
        {
            new(
                "docs",
                "DOCS",
                DocsSnapshotState,
                TweakResearchPresentation.GetProofAccentBrush("docs", ResearchAccentBrush)),
            new(
                "runtime",
                "RUNTIME",
                RuntimeSnapshotState,
                TweakResearchPresentation.GetProofAccentBrush("runtime", ResearchAccentBrush)),
            new(
                "source",
                "SOURCE",
                SourceSnapshotState,
                TweakResearchPresentation.GetProofAccentBrush("source", ResearchAccentBrush)),
            new(
                "rollback",
                "ROLLBACK",
                RollbackSnapshotState,
                TweakResearchPresentation.GetProofAccentBrush("rollback", ResearchAccentBrush))
        };
    }

    private IReadOnlyList<TweakValueSummaryRowViewModel> BuildValueSummaryRows()
    {
        var rows = new List<TweakValueSummaryRowViewModel>();

        if (HasRegistryPath)
        {
            rows.Add(new TweakValueSummaryRowViewModel("KEY PATH", RegistryPath, ScopeDisplayText));
        }

        if (!string.IsNullOrWhiteSpace(CurrentValue))
        {
            rows.Add(new TweakValueSummaryRowViewModel(
                "CURRENT",
                CurrentValue,
                HasDetectedState ? ConfigurationInventoryFreshnessText : "Current state"));
        }

        if (!string.IsNullOrWhiteSpace(TargetValue))
        {
            rows.Add(new TweakValueSummaryRowViewModel("TARGET", TargetValue, ActionButtonText));
        }

        if (SelectedChoiceOption is not null)
        {
            rows.Add(new TweakValueSummaryRowViewModel(
                "CHOICE",
                SelectedChoiceOption.Label,
                SelectedChoiceDescription));
        }

        if (HasDefaultChoice)
        {
            var defaultValue = string.IsNullOrWhiteSpace(DefaultChoiceLabel)
                ? "Restore default available"
                : DefaultChoiceLabel;
            rows.Add(new TweakValueSummaryRowViewModel("KNOWN DEFAULT", defaultValue, DefaultVsPreviousSummary));
        }

        if (!string.IsNullOrWhiteSpace(RollbackStoryText))
        {
            rows.Add(new TweakValueSummaryRowViewModel(
                "ROLLBACK",
                RollbackStoryText,
                "Restore previous state"));
        }

        if (!string.IsNullOrWhiteSpace(ConfigurationCompactInfoLine))
        {
            rows.Add(new TweakValueSummaryRowViewModel("SUMMARY", ConfigurationCompactInfoLine));
        }

        return rows;
    }

    private static void ReplaceLinks(List<ReferenceLink> target, IReadOnlyList<ReferenceLink> source)
    {
        target.Clear();
        target.AddRange(source);
    }

    private static IReadOnlyList<ReferenceLink> MapEvidenceLinks(IReadOnlyList<TweakEvidenceLink>? links)
    {
        if (links is null || links.Count == 0)
        {
            return Array.Empty<ReferenceLink>();
        }

        var mapped = new List<ReferenceLink>(links.Count);
        foreach (var link in links)
        {
            if (link is null
                || string.IsNullOrWhiteSpace(link.Url)
                || PublicEvidenceLinkPolicy.IsSuppressedExternalPseudocodeUrl(link.Url))
            {
                continue;
            }

            var title = string.IsNullOrWhiteSpace(link.Title) ? link.Url : link.Title.Trim();
            mapped.Add(new ReferenceLink(
                title,
                link.Url.Trim(),
                link.Summary,
                MapEvidenceLinkKind(link.Kind)));
        }

        return mapped;
    }

    private static ReferenceLinkKind MapEvidenceLinkKind(string? kind)
    {
        return (kind?.Trim().ToLowerInvariant() ?? string.Empty) switch
        {
            "catalog" => ReferenceLinkKind.Catalog,
            "details" => ReferenceLinkKind.Details,
            "docs" => ReferenceLinkKind.Docs,
            "internals" => ReferenceLinkKind.Docs,
            "microsoft" => ReferenceLinkKind.Docs,
            "source" => ReferenceLinkKind.Source,
            "nohuto" => ReferenceLinkKind.Source,
            _ => ReferenceLinkKind.Other
        };
    }

    private static IReadOnlyList<ReferenceLink> MergeLinks(params IEnumerable<ReferenceLink>[] groups)
    {
        var merged = new List<ReferenceLink>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
        {
            if (group is null)
            {
                continue;
            }

            foreach (var link in group)
            {
                if (link is null
                    || string.IsNullOrWhiteSpace(link.Url)
                    || PublicEvidenceLinkPolicy.IsSuppressedExternalPseudocodeUrl(link.Url)
                    || !seen.Add(link.Url))
                {
                    continue;
                }

                merged.Add(link);
            }
        }

        return merged;
    }
}
