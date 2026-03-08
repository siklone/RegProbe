using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WindowsOptimizer.App.Diagnostics;
using WindowsOptimizer.App.Services;
using WindowsOptimizer.App.Utilities;
using WindowsOptimizer.Infrastructure;

namespace WindowsOptimizer.App.ViewModels;

public sealed class WinConfigCatalogPanelViewModel : ViewModelBase
{
    private readonly AppPaths _paths;
    private readonly Func<IDictionary<string, int>> _localPolicyCountProvider;
    private readonly RelayCommand _refreshCommand;
    private readonly RelayCommand _openReportCommand;
    private readonly RelayCommand _openRepoCommand;
    private ObservableCollection<WinConfigCatalogCategoryViewModel> _categories = new();
    private string _headline = "win-config catalog pending";
    private string _detail = "The category catalog has not been loaded yet.";
    private string _context = "Network, power, security, privacy, system, visibility, peripheral";
    private string _reportPath = string.Empty;
    private bool _isRefreshing;
    private int _totalTopicCount;
    private int _mappedPolicyCount;

    public WinConfigCatalogPanelViewModel(AppPaths paths, Func<IDictionary<string, int>> localPolicyCountProvider)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _localPolicyCountProvider = localPolicyCountProvider ?? throw new ArgumentNullException(nameof(localPolicyCountProvider));
        _refreshCommand = new RelayCommand(_ => _ = RefreshAsync(isManual: true), _ => !IsRefreshing);
        _openReportCommand = new RelayCommand(_ => OpenReport(), _ => !string.IsNullOrWhiteSpace(ReportPath));
        _openRepoCommand = new RelayCommand(_ => OpenRepo());

        RefreshCommand = _refreshCommand;
        OpenReportCommand = _openReportCommand;
        OpenRepoCommand = _openRepoCommand;

        LoadFromCache();
        _ = RefreshAsync(isManual: false);
    }

    public ICommand RefreshCommand { get; }
    public ICommand OpenReportCommand { get; }
    public ICommand OpenRepoCommand { get; }
    public ObservableCollection<WinConfigCatalogCategoryViewModel> Categories { get => _categories; private set => SetProperty(ref _categories, value); }
    public string Headline { get => _headline; private set => SetProperty(ref _headline, value); }
    public string Detail { get => _detail; private set => SetProperty(ref _detail, value); }
    public string Context { get => _context; private set => SetProperty(ref _context, value); }
    public string ReportPath
    {
        get => _reportPath;
        private set
        {
            if (SetProperty(ref _reportPath, value))
            {
                _openReportCommand.RaiseCanExecuteChanged();
            }
        }
    }
    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set
        {
            if (SetProperty(ref _isRefreshing, value))
            {
                _refreshCommand.RaiseCanExecuteChanged();
            }
        }
    }
    public int TotalTopicCount { get => _totalTopicCount; private set => SetProperty(ref _totalTopicCount, value); }
    public int MappedPolicyCount { get => _mappedPolicyCount; private set => SetProperty(ref _mappedPolicyCount, value); }
    public bool HasCategories => Categories.Count > 0;

    private void LoadFromCache()
    {
        try
        {
            using var service = new WinConfigCatalogService(_paths);
            var state = service.LoadCachedState();
            if (state.Categories.Count == 0)
            {
                return;
            }

            ApplyResult(new WinConfigCatalogResult
            {
                CheckedSuccessfully = true,
                UsedCachedData = true,
                Summary = state.LastSummary,
                CheckedAtUtc = state.LastCheckedAtUtc,
                MarkdownReportPath = _paths.WinConfigCatalogMarkdownReportPath,
                Categories = state.Categories
            });
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("win-config catalog cache load failed", ex);
        }
    }

    public async Task RefreshAsync(bool isManual)
    {
        if (IsRefreshing)
        {
            return;
        }

        IsRefreshing = true;

        try
        {
            using var cts = new CancellationTokenSource(isManual
                ? TimeSpan.FromSeconds(18)
                : TimeSpan.FromSeconds(10));
            using var service = new WinConfigCatalogService(_paths);
            var result = await service.RefreshAsync(cts.Token, isManual ? null : TimeSpan.FromHours(4));
            ApplyResult(result);
        }
        catch (OperationCanceledException)
        {
            Headline = "win-config refresh timed out";
            Detail = "Cached catalog remains available.";
            Context = "Try Refresh again when you want a live pull from GitHub.";
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("win-config catalog refresh failed", ex);
            Headline = "win-config refresh failed";
            Detail = ex.Message;
            Context = "Cached category metadata remains the fallback.";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void ApplyResult(WinConfigCatalogResult result)
    {
        var localCounts = _localPolicyCountProvider();
        Categories = new ObservableCollection<WinConfigCatalogCategoryViewModel>(
            result.Categories.Select(category =>
            {
                localCounts.TryGetValue(category.Id, out var mappedCount);
                return new WinConfigCatalogCategoryViewModel(category, mappedCount);
            }));
        OnPropertyChanged(nameof(HasCategories));

        TotalTopicCount = result.Categories.Sum(static category => category.TopicCount);
        MappedPolicyCount = result.Categories.Sum(category =>
        {
            localCounts.TryGetValue(category.Id, out var count);
            return count;
        });

        var liveOrCache = result.UsedCachedData ? "cache" : "live";
        Headline = $"{result.Categories.Count} win-config categories indexed";
        Detail = string.IsNullOrWhiteSpace(result.Summary)
            ? "Category metadata is ready."
            : result.Summary;
        Context = BuildContext(result.CheckedAtUtc, liveOrCache, result.Categories);

        ReportPath = File.Exists(result.MarkdownReportPath)
            ? result.MarkdownReportPath
            : string.Empty;
    }

    private static string BuildContext(DateTimeOffset checkedAtUtc, string liveOrCache, IReadOnlyList<WinConfigCatalogCategory> categories)
    {
        var highlights = categories
            .OrderByDescending(static category => category.TopicCount)
            .ThenByDescending(static category => category.FileCount)
            .Take(3)
            .Select(static category => category.DisplayName)
            .ToArray();

        var parts = new List<string>();
        if (highlights.Length > 0)
        {
            parts.Add($"Top domains {string.Join(", ", highlights)}");
        }

        if (checkedAtUtc != default)
        {
            parts.Add($"Checked {checkedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm}");
        }

        parts.Add(liveOrCache);
        return string.Join(" · ", parts);
    }

    private void OpenReport()
    {
        if (string.IsNullOrWhiteSpace(ReportPath) || !File.Exists(ReportPath))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(ReportPath) { UseShellExecute = true });
        }
        catch
        {
            // Ignore shell launch failures.
        }
    }

    private void OpenRepo()
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/nohuto/win-config") { UseShellExecute = true });
        }
        catch
        {
            // Ignore shell launch failures.
        }
    }
}
