using System;
using System.ComponentModel;
using System.Windows.Input;
using RegProbe.Core;
using RegProbe.Engine.Tweaks;

namespace RegProbe.App.ViewModels;

public sealed class RepairsItemViewModel : ViewModelBase, IDisposable
{
    private readonly TweakItemViewModel _source;
    private bool _isDisposed;

    public RepairsItemViewModel(TweakItemViewModel source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _source.PropertyChanged += OnSourcePropertyChanged;
    }

    internal TweakItemViewModel Source => _source;

    public string Name => _source.Name;

    public TweakRiskLevel Risk => _source.Risk;

    public TweakAppliedStatus AppliedStatus => _source.AppliedStatus;

    public string StatusText => _source.StatusText;

    public string EvidenceClassId => _source.EvidenceClassId;

    public string EvidenceClassBadgeText => _source.EvidenceClassBadgeText;

    public string FriendlyDescription => _source.FriendlyDescription;

    public string ResearchGateMessage => _source.ResearchGateMessage;

    public bool HasResearchGateMessage => _source.HasResearchGateMessage;

    public string RowMetaText => _source.RowMetaText;

    public string RepairsRiskHint => _source.RepairsRiskHint;

    public bool HasRepairsRiskHint => _source.HasRepairsRiskHint;

    public string BatchSummaryLine => _source.BatchSummaryLine;

    public bool HasBatchSummaryLine => _source.HasBatchSummaryLine;

    public string RepairsActionButtonText => _source.RepairsActionButtonText;

    public string PrimaryActionTooltip => _source.PrimaryActionTooltip;

    public bool IsAdvancedRisk => _source.IsAdvancedRisk;

    public bool IsRisky => _source.IsRisky;

    public bool IsSelected
    {
        get => _source.IsSelected;
        set
        {
            if (_source.IsSelected == value)
            {
                return;
            }

            _source.IsSelected = value;
            OnPropertyChanged();
        }
    }

    public ICommand ApplyCommand => _source.ApplyCommand;

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            OnPropertyChanged(string.Empty);
            return;
        }

        OnPropertyChanged(e.PropertyName);

        if (e.PropertyName == nameof(TweakItemViewModel.ActionButtonText))
        {
            OnPropertyChanged(nameof(RepairsActionButtonText));
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _source.PropertyChanged -= OnSourcePropertyChanged;
    }
}
