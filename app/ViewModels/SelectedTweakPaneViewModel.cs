using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace RegProbe.App.ViewModels;

public sealed class SelectedTweakPaneViewModel : ViewModelBase, IDisposable
{
    private readonly Action<string> _setStatusMessage;
    private readonly RelayCommand _copyPlanCommand;
    private readonly RelayCommand _exportPlanCommand;
    private TweakItemViewModel? _selectedTweak;
    private bool _isPlanDrawerExpanded;
    private bool _isEvidenceExpanded;

    public SelectedTweakPaneViewModel(Action<string> setStatusMessage)
    {
        _setStatusMessage = setStatusMessage ?? throw new ArgumentNullException(nameof(setStatusMessage));
        _copyPlanCommand = new RelayCommand(_ => CopyPlan(), _ => HasSelectedTweak);
        _exportPlanCommand = new RelayCommand(_ => ExportPlan(), _ => HasSelectedTweak);
    }

    public ICommand CopyPlanCommand => _copyPlanCommand;

    public ICommand ExportPlanCommand => _exportPlanCommand;

    public TweakItemViewModel? SelectedTweak
    {
        get => _selectedTweak;
        set
        {
            if (ReferenceEquals(_selectedTweak, value))
            {
                return;
            }

            if (_selectedTweak is not null)
            {
                _selectedTweak.PropertyChanged -= OnSelectedTweakPropertyChanged;
            }

            _selectedTweak = value;

            if (_selectedTweak is not null)
            {
                _selectedTweak.PropertyChanged += OnSelectedTweakPropertyChanged;
            }

            IsEvidenceExpanded = false;
            RaiseSelectionChanged();
        }
    }

    public bool HasSelectedTweak => SelectedTweak is not null;

    public bool IsPlanDrawerExpanded
    {
        get => _isPlanDrawerExpanded;
        set => SetProperty(ref _isPlanDrawerExpanded, value);
    }

    public bool IsEvidenceExpanded
    {
        get => _isEvidenceExpanded;
        set => SetProperty(ref _isEvidenceExpanded, value);
    }

    public bool IsExecutionMode =>
        SelectedTweak?.IsRunning == true
        || SelectedTweak?.ShowTerminal == true
        || SelectedTweak?.HasTerminalOutput == true;

    public string DrawerTitle => IsExecutionMode
        ? "Execution log"
        : HasSelectedTweak
            ? "Plan preview"
            : "Plan";

    public string DrawerSummary
    {
        get
        {
            if (SelectedTweak is null)
            {
                return "Plan unavailable";
            }

            if (IsExecutionMode)
            {
                return string.IsNullOrWhiteSpace(SelectedTweak.OutcomeSummary)
                    ? $"{SelectedTweak.Name} • running"
                    : SelectedTweak.OutcomeSummary;
            }

            return TweakExecutionPlanSnapshot.Create(SelectedTweak).CollapsedSummary;
        }
    }

    public IReadOnlyList<string> PlanLines => TweakExecutionPlanSnapshot.Create(SelectedTweak).Lines;

    public string ExecutionLogText => SelectedTweak?.TerminalOutput ?? string.Empty;

    public void SyncSelection(IReadOnlyList<TweakItemViewModel> visibleTweaks, bool forceFirstVisible = false)
    {
        ArgumentNullException.ThrowIfNull(visibleTweaks);

        if (visibleTweaks.Count == 0)
        {
            SelectedTweak = null;
            return;
        }

        if (!forceFirstVisible
            && SelectedTweak is not null
            && visibleTweaks.Contains(SelectedTweak))
        {
            return;
        }

        SelectedTweak = visibleTweaks[0];
    }

    public void NotifySelectionStateChanged()
    {
        RaiseSelectionChanged();
    }

    public void Dispose()
    {
        if (_selectedTweak is not null)
        {
            _selectedTweak.PropertyChanged -= OnSelectedTweakPropertyChanged;
        }
    }

    private void OnSelectedTweakPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            RaiseSelectionChanged();
            return;
        }

        if (e.PropertyName is nameof(TweakItemViewModel.IsRunning)
            or nameof(TweakItemViewModel.ShowTerminal)
            or nameof(TweakItemViewModel.TerminalOutput)
            or nameof(TweakItemViewModel.HasTerminalOutput))
        {
            if (IsExecutionMode)
            {
                IsPlanDrawerExpanded = true;
            }
        }

        if (e.PropertyName is nameof(TweakItemViewModel.IsRunning)
            or nameof(TweakItemViewModel.ShowTerminal)
            or nameof(TweakItemViewModel.TerminalOutput)
            or nameof(TweakItemViewModel.HasTerminalOutput)
            or nameof(TweakItemViewModel.OutcomeSummary)
            or nameof(TweakItemViewModel.RollbackSnapshotState)
            or nameof(TweakItemViewModel.TargetValue)
            or nameof(TweakItemViewModel.CurrentValue)
            or nameof(TweakItemViewModel.Name)
            or nameof(TweakItemViewModel.RegistryPath))
        {
            RaiseSelectionChanged();
        }
    }

    private void RaiseSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedTweak));
        OnPropertyChanged(nameof(HasSelectedTweak));
        OnPropertyChanged(nameof(IsExecutionMode));
        OnPropertyChanged(nameof(DrawerTitle));
        OnPropertyChanged(nameof(DrawerSummary));
        OnPropertyChanged(nameof(PlanLines));
        OnPropertyChanged(nameof(ExecutionLogText));
        _copyPlanCommand.RaiseCanExecuteChanged();
        _exportPlanCommand.RaiseCanExecuteChanged();
    }

    private void CopyPlan()
    {
        if (SelectedTweak is null)
        {
            return;
        }

        var exportText = TweakExecutionPlanSnapshot.Create(SelectedTweak).ExportText;
        if (string.IsNullOrWhiteSpace(exportText))
        {
            return;
        }

        try
        {
            Clipboard.SetText(exportText);
            _setStatusMessage("Plan copied.");
        }
        catch
        {
            _setStatusMessage("Plan copy failed.");
        }
    }

    private void ExportPlan()
    {
        if (SelectedTweak is null)
        {
            return;
        }

        var exportText = TweakExecutionPlanSnapshot.Create(SelectedTweak).ExportText;
        if (string.IsNullOrWhiteSpace(exportText))
        {
            return;
        }

        var safeFileName = string.Join(
            "-",
            (SelectedTweak.Id ?? SelectedTweak.Name)
            .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        var dialog = new SaveFileDialog
        {
            Title = "Export plan",
            DefaultExt = ".txt",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"{safeFileName}-plan.txt"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        File.WriteAllText(dialog.FileName, exportText);
        _setStatusMessage("Plan exported.");
    }
}
