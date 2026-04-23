namespace RegProbe.App.ViewModels;

public sealed class TweakValueSummaryRowViewModel
{
    public TweakValueSummaryRowViewModel(string label, string value, string detail = "")
    {
        Label = label;
        Value = value ?? string.Empty;
        Detail = detail ?? string.Empty;
    }

    public string Label { get; }

    public string Value { get; }

    public string Detail { get; }

    public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);
}
