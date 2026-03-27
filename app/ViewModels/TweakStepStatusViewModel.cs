using System;
using RegProbe.Core;

namespace RegProbe.App.ViewModels;

public sealed class TweakStepStatusViewModel : ViewModelBase
{
    private TweakStepState _state;
    private string _statusText = "Pending";
    private string _message = string.Empty;
    private string _timestampText = "-";

    public TweakStepStatusViewModel(TweakAction action)
    {
        Action = action;
        ActionLabel = action.ToString();
        Reset();
    }

    public TweakAction Action { get; }

    public string ActionLabel { get; }

    public TweakStepState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    public string TimestampText
    {
        get => _timestampText;
        private set => SetProperty(ref _timestampText, value);
    }

    public void Reset()
    {
        State = TweakStepState.Pending;
        StatusText = "Pending";
        Message = string.Empty;
        TimestampText = "-";
    }

    public void MarkInProgress()
    {
        State = TweakStepState.InProgress;
        StatusText = "Running";
        Message = string.Empty;
        TimestampText = "-";
    }

    public void ApplyResult(TweakStatus status, string message, DateTimeOffset timestamp)
    {
        State = MapState(status);
        StatusText = FormatStatus(status);
        Message = message;
        TimestampText = timestamp == default
            ? "-"
            : timestamp.ToLocalTime().ToString("HH:mm:ss");
    }

    public void MarkNotRequired(string reason)
    {
        State = TweakStepState.NotApplicable;
        StatusText = "Not required";
        Message = reason;
        TimestampText = "-";
    }

    private static TweakStepState MapState(TweakStatus status)
    {
        return status switch
        {
            TweakStatus.Failed => TweakStepState.Failed,
            TweakStatus.Skipped => TweakStepState.Skipped,
            TweakStatus.NotApplicable => TweakStepState.NotApplicable,
            _ => TweakStepState.Success
        };
    }

    private static string FormatStatus(TweakStatus status)
    {
        return status switch
        {
            TweakStatus.NotApplicable => "Not applicable",
            TweakStatus.RolledBack => "Rolled back",
            _ => status.ToString()
        };
    }
}
