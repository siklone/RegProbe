using System.Windows.Media;

namespace RegProbe.App.ViewModels;

public sealed class TweakProofBarViewModel
{
    public TweakProofBarViewModel(string key, string label, string state, Brush fillBrush)
    {
        Key = key;
        Label = label;
        State = state;
        FillBrush = fillBrush;
    }

    public string Key { get; }

    public string Label { get; }

    public string State { get; }

    public Brush FillBrush { get; }

    public double FillFactor => TweakResearchPresentation.GetProofFillFactor(State);

    public string StateText => TweakResearchPresentation.BuildProofStateText(State);
}
