using System;
using System.Linq;
using System.Diagnostics;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.Utilities;

namespace WindowsOptimizer.App.ViewModels;

public sealed class WinConfigCatalogCategoryViewModel
{
    public WinConfigCatalogCategoryViewModel(WinConfigCatalogCategory category, int mappedPolicyCount)
    {
        if (category is null)
        {
            throw new ArgumentNullException(nameof(category));
        }

        Id = category.Id;
        DisplayName = category.DisplayName;
        Description = category.Description;
        TopicCountLabel = $"{category.TopicCount} topics";
        FileCountLabel = $"{category.FileCount} files";
        CoverageLabel = mappedPolicyCount > 0 ? $"{mappedPolicyCount} local policies" : "No mapped local policy yet";
        DensityLabel = BuildDensityLabel(category);
        TopicPreview = category.Topics.Count == 0
            ? "Topic headings will appear after the next refresh."
            : string.Join(" · ", category.Topics.Take(3));
        SourceUrl = category.SourceUrl;
        DescriptionUrl = category.DescriptionUrl;
        OpenCategoryCommand = new RelayCommand(_ => OpenUrl(SourceUrl));
        OpenDescriptionCommand = new RelayCommand(_ => OpenUrl(DescriptionUrl));
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string TopicCountLabel { get; }
    public string FileCountLabel { get; }
    public string CoverageLabel { get; }
    public string DensityLabel { get; }
    public string TopicPreview { get; }
    public string SourceUrl { get; }
    public string DescriptionUrl { get; }
    public RelayCommand OpenCategoryCommand { get; }
    public RelayCommand OpenDescriptionCommand { get; }

    private static string BuildDensityLabel(WinConfigCatalogCategory category)
    {
        var parts = new[]
        {
            category.DocumentationFileCount > 0 ? $"{category.DocumentationFileCount} docs" : null,
            category.ScriptFileCount > 0 ? $"{category.ScriptFileCount} scripts" : null,
            category.AssetFileCount > 0 ? $"{category.AssetFileCount} assets" : null
        }.Where(static part => !string.IsNullOrWhiteSpace(part));

        return string.Join(" · ", parts);
    }

    private static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Ignore shell launch failures.
        }
    }
}
