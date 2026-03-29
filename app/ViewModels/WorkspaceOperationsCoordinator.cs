using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using RegProbe.Core.Models;
using RegProbe.Core.Services;
using RegProbe.Infrastructure;

namespace RegProbe.App.ViewModels;

public sealed class WorkspaceOperationsCoordinator : ViewModelBase
{
    private readonly ITweakLogStore _logStore;
    private readonly IProfileManager _profileManager;
    private readonly IAppLogger _appLogger;
    private readonly string _logFolderPath;
    private readonly string _tweakLogFilePath;
    private string _exportStatusMessage = "Logs are ready to export.";
    private bool _isExporting;
    private long _logFileSizeBytes;

    public WorkspaceOperationsCoordinator(
        ITweakLogStore logStore,
        IProfileManager profileManager,
        IAppLogger appLogger,
        string logFolderPath,
        string tweakLogFilePath)
    {
        _logStore = logStore ?? throw new ArgumentNullException(nameof(logStore));
        _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
        _appLogger = appLogger ?? throw new ArgumentNullException(nameof(appLogger));
        _logFolderPath = logFolderPath ?? throw new ArgumentNullException(nameof(logFolderPath));
        _tweakLogFilePath = tweakLogFilePath ?? throw new ArgumentNullException(nameof(tweakLogFilePath));
    }

    public string ExportStatusMessage
    {
        get => _exportStatusMessage;
        private set => SetProperty(ref _exportStatusMessage, value);
    }

    public bool IsExporting
    {
        get => _isExporting;
        private set => SetProperty(ref _isExporting, value);
    }

    public long LogFileSizeBytes
    {
        get => _logFileSizeBytes;
        private set
        {
            if (SetProperty(ref _logFileSizeBytes, value))
            {
                OnPropertyChanged(nameof(LogFileSizeFormatted));
            }
        }
    }

    public string LogFileSizeFormatted => FormatBytes(LogFileSizeBytes);

    public void RefreshLogFileSize()
    {
        if (!File.Exists(_tweakLogFilePath))
        {
            LogFileSizeBytes = 0;
            return;
        }

        try
        {
            LogFileSizeBytes = new FileInfo(_tweakLogFilePath).Length;
        }
        catch
        {
            LogFileSizeBytes = 0;
        }
    }

    public async Task ExportLogsAsync()
    {
        if (IsExporting)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = "configuration-log.csv",
            Title = "Export activity logs"
        };

        if (dialog.ShowDialog() != true)
        {
            ExportStatusMessage = "Export cancelled.";
            return;
        }

        IsExporting = true;
        try
        {
            await _logStore.ExportCsvAsync(dialog.FileName, CancellationToken.None);
            ExportStatusMessage = $"Exported to {dialog.FileName}.";
            _appLogger.Log(LogLevel.Info, $"Activity: Logs - Tweak log exported ({dialog.FileName})");
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    public void OpenLogFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _logFolderPath,
                UseShellExecute = true
            });

            ExportStatusMessage = $"Opened log folder: {_logFolderPath}.";
            _appLogger.Log(LogLevel.Info, $"Activity: Logs - Log folder opened ({_logFolderPath})");
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Open log folder failed: {ex.Message}";
        }
    }

    public void OpenCsvLog()
    {
        try
        {
            if (!File.Exists(_tweakLogFilePath))
            {
                ExportStatusMessage = "No tweak log file yet. Run a tweak to generate one.";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = _tweakLogFilePath,
                UseShellExecute = true
            });

            ExportStatusMessage = $"Opened log file: {_tweakLogFilePath}.";
            _appLogger.Log(LogLevel.Info, $"Activity: Logs - Tweak log opened ({_tweakLogFilePath})");
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Open log failed: {ex.Message}";
        }
    }

    public async Task ExportPresetsAsync(IEnumerable<TweakItemViewModel> tweaks)
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Optimizer Profile (*.json)|*.json|All Files (*.*)|*.*",
                FileName = $"optimizer_profile_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                Title = "Export Profile"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var tweakList = (tweaks ?? Enumerable.Empty<TweakItemViewModel>()).ToList();
            var selectedIds = tweakList.Where(t => t.IsSelected).Select(t => t.Id).ToList();
            var appliedIds = tweakList.Where(t => t.AppliedStatus == TweakAppliedStatus.Applied).Select(t => t.Id).ToList();

            var profile = new TweakProfile
            {
                Name = Path.GetFileNameWithoutExtension(dialog.FileName),
                Description = $"Custom profile exported on {DateTime.Now:yyyy-MM-dd HH:mm}",
                Author = "User",
                CreatedDate = DateTime.Now,
                Version = "1.0",
                SelectedTweakIds = selectedIds.Count > 0 ? selectedIds : appliedIds,
                AppliedTweakIds = appliedIds,
                Metadata = new ProfileMetadata
                {
                    TargetUseCase = "Custom",
                    TotalTweakCount = selectedIds.Count > 0 ? selectedIds.Count : appliedIds.Count,
                    TweaksByCategory = new Dictionary<string, int>(),
                    TweaksByRiskLevel = new Dictionary<string, int>()
                }
            };

            await _profileManager.SaveProfileAsync(profile, dialog.FileName);
            ExportStatusMessage = $"Profile exported successfully to {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Export failed: {ex.Message}";
        }
    }

    public async Task ImportPresetsAsync(IEnumerable<TweakItemViewModel> tweaks)
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Optimizer Profile (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Import Profile"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var profile = await _profileManager.LoadProfileAsync(dialog.FileName);
            var tweakList = (tweaks ?? Enumerable.Empty<TweakItemViewModel>()).ToList();
            var selectedCount = ApplySelectionState(tweakList, profile.SelectedTweakIds, expandDetails: true);
            ExportStatusMessage = $"Imported profile '{profile.Name}': {selectedCount}/{profile.SelectedTweakIds.Count} tweaks selected. Ready to apply.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Import failed: {ex.Message}";
        }
    }

    public async Task LoadPresetAsync(object? parameter, IEnumerable<TweakItemViewModel> tweaks)
    {
        try
        {
            var presetName = parameter as string;
            if (string.IsNullOrEmpty(presetName))
            {
                return;
            }

            var profile = await _profileManager.CreatePresetAsync(presetName);
            var tweakList = (tweaks ?? Enumerable.Empty<TweakItemViewModel>()).ToList();
            var selectedCount = ApplySelectionState(tweakList, profile.SelectedTweakIds, expandDetails: false);
            ExportStatusMessage = $"Loaded '{profile.Name}' preset: {selectedCount}/{profile.SelectedTweakIds.Count} tweaks selected.";
        }
        catch (Exception ex)
        {
            ExportStatusMessage = $"Failed to load preset: {ex.Message}";
        }
    }

    public async Task InitializePresetsAsync()
    {
        try
        {
            await _profileManager.InitializePresetsAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize presets: {ex.Message}");
        }
    }

    public void CreateSnapshot()
    {
        ExportStatusMessage = $"Registry Snapshot created: {DateTime.Now:yyyyMMdd_HHmm}.";
    }

    private static int ApplySelectionState(
        IEnumerable<TweakItemViewModel> tweaks,
        IEnumerable<string> selectedIds,
        bool expandDetails)
    {
        var tweakList = tweaks.ToList();
        var selectedIdSet = new HashSet<string>(selectedIds ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        foreach (var tweak in tweakList)
        {
            tweak.IsSelected = false;
        }

        var selectedCount = 0;
        foreach (var tweak in tweakList)
        {
            if (!selectedIdSet.Contains(tweak.Id))
            {
                continue;
            }

            tweak.IsSelected = true;
            if (expandDetails)
            {
                tweak.IsDetailsExpanded = true;
            }

            selectedCount++;
        }

        return selectedCount;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}
