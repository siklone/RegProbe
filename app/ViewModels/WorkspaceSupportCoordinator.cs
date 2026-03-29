using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RegProbe.App.Services;
using RegProbe.App.Utilities;

namespace RegProbe.App.ViewModels;

public sealed class WorkspaceSupportCoordinator : ViewModelBase
{
    private int _docsMissingCount;
    private string _docsCoverageSummary = "Docs report unavailable.";
    private string _docsCoverageReportPath = string.Empty;
    private int _provenanceReviewCount;
    private string _provenanceCoverageSummary = "Source links unavailable.";
    private string _provenanceReportPath = string.Empty;
    private readonly TweakDocumentationLinker _documentationLinker = new();
    private readonly TweakProvenanceCatalogService _provenanceCatalogService = new();
    private readonly TweakEvidenceClassCatalogService _evidenceClassCatalogService = new();

    public WorkspaceSupportCoordinator(Action<PolicyReferenceEntry> openPolicyReferenceEntry)
    {
        PolicyReference = new PolicyReferencePanelViewModel(openPolicyReferenceEntry);
    }

    public PolicyReferencePanelViewModel PolicyReference { get; }

    public int DocsMissingCount
    {
        get => _docsMissingCount;
        private set
        {
            if (SetProperty(ref _docsMissingCount, value))
            {
                OnPropertyChanged(nameof(DocsCoverageOk));
                OnPropertyChanged(nameof(DocsCoverageWarn));
                OnPropertyChanged(nameof(DocsCoverageCritical));
            }
        }
    }

    public string DocsCoverageSummary
    {
        get => _docsCoverageSummary;
        private set => SetProperty(ref _docsCoverageSummary, value);
    }

    public string DocsCoverageReportPath
    {
        get => _docsCoverageReportPath;
        private set => SetProperty(ref _docsCoverageReportPath, value);
    }

    public bool DocsCoverageOk => DocsMissingCount == 0;

    public bool DocsCoverageWarn => DocsMissingCount > 0 && DocsMissingCount <= 10;

    public bool DocsCoverageCritical => DocsMissingCount > 10;

    public int ProvenanceReviewCount
    {
        get => _provenanceReviewCount;
        private set => SetProperty(ref _provenanceReviewCount, value);
    }

    public string ProvenanceCoverageSummary
    {
        get => _provenanceCoverageSummary;
        private set => SetProperty(ref _provenanceCoverageSummary, value);
    }

    public string ProvenanceReportPath
    {
        get => _provenanceReportPath;
        private set => SetProperty(ref _provenanceReportPath, value);
    }

    public void ApplyCatalogs(IEnumerable<TweakItemViewModel> tweaks)
    {
        var tweakList = (tweaks ?? Enumerable.Empty<TweakItemViewModel>()).ToList();
        _documentationLinker.Apply(tweakList);
        _provenanceCatalogService.Apply(tweakList);
        _evidenceClassCatalogService.Apply(tweakList);
    }

    public void Initialize(IEnumerable<TweakItemViewModel> tweaks)
    {
        LoadDocsCoverageReport();
        LoadProvenanceCoverageReport();
        RefreshPolicyReferencePanel(tweaks);
    }

    public void RefreshPolicyReferencePanel(IEnumerable<TweakItemViewModel> tweaks)
    {
        PolicyReference.Refresh(tweaks ?? Enumerable.Empty<TweakItemViewModel>());
    }

    public void OpenDocsCoverageReport()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(DocsCoverageReportPath) || !File.Exists(DocsCoverageReportPath))
            {
                LoadDocsCoverageReport();
            }

            if (!string.IsNullOrWhiteSpace(DocsCoverageReportPath) && File.Exists(DocsCoverageReportPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = DocsCoverageReportPath,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    public void OpenProvenanceReport()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ProvenanceReportPath) || !File.Exists(ProvenanceReportPath))
            {
                LoadProvenanceCoverageReport();
            }

            if (!string.IsNullOrWhiteSpace(ProvenanceReportPath) && File.Exists(ProvenanceReportPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = ProvenanceReportPath,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    private void LoadDocsCoverageReport()
    {
        try
        {
            var docsRoot = DocsLocator.TryFindDocsRoot();
            if (string.IsNullOrWhiteSpace(docsRoot))
            {
                DocsCoverageReportPath = string.Empty;
                DocsMissingCount = 0;
                DocsCoverageSummary = "Docs folder not found.";
                return;
            }

            var priorityHtml = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing-priority.html");
            var priorityMd = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing-priority.md");
            var priorityCsv = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing-priority.csv");
            var fallbackHtml = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing.html");
            var fallbackMd = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing.md");
            var fallbackCsv = Path.Combine(docsRoot, "tweaks", "tweak-docs-missing.csv");

            var reportPath = File.Exists(priorityHtml)
                ? priorityHtml
                : File.Exists(priorityMd)
                    ? priorityMd
                    : File.Exists(fallbackHtml)
                        ? fallbackHtml
                        : File.Exists(fallbackMd)
                            ? fallbackMd
                            : string.Empty;

            DocsCoverageReportPath = reportPath;

            var csvPath = File.Exists(priorityCsv)
                ? priorityCsv
                : File.Exists(fallbackCsv)
                    ? fallbackCsv
                    : string.Empty;

            if (!string.IsNullOrWhiteSpace(csvPath))
            {
                var lines = File.ReadAllLines(csvPath);
                DocsMissingCount = Math.Max(0, lines.Length - 1);
                DocsCoverageSummary = DocsMissingCount == 0 ? "All documented" : $"{DocsMissingCount} missing";
            }
            else
            {
                DocsMissingCount = 0;
                DocsCoverageSummary = string.IsNullOrWhiteSpace(reportPath)
                    ? "Docs report unavailable."
                    : "Docs report ready.";
            }
        }
        catch
        {
            DocsCoverageReportPath = string.Empty;
            DocsMissingCount = 0;
            DocsCoverageSummary = "Docs report unavailable.";
        }
    }

    private void LoadProvenanceCoverageReport()
    {
        try
        {
            var catalog = _provenanceCatalogService.Catalog;
            ProvenanceReportPath = _provenanceCatalogService.ResolveMarkdownReportPath();

            if (catalog.TotalTweaks <= 0)
            {
                ProvenanceReviewCount = 0;
                ProvenanceCoverageSummary = "Source links unavailable.";
                return;
            }

            ProvenanceReviewCount = catalog.ReviewNeededTweaks;
            ProvenanceCoverageSummary =
                $"{catalog.RepoBackedTweaks}/{catalog.TotalTweaks} repo-linked | " +
                $"{catalog.InternalsBackedTweaks} internals refs | " +
                $"{catalog.ReviewNeededTweaks} review";
        }
        catch
        {
            ProvenanceReportPath = string.Empty;
            ProvenanceReviewCount = 0;
            ProvenanceCoverageSummary = "Source links unavailable.";
        }
    }
}
