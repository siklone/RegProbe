namespace WindowsOptimizer.App.Models;

public sealed class SpecItem
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = "N/A";
    public bool IsGroupHeader { get; init; }

    private SpecItem() { }

    public static SpecItem Header(string label) => new()
    {
        Label = label,
        Value = string.Empty,
        IsGroupHeader = true
    };

    public static SpecItem Row(string label, string? value) => new()
    {
        Label = label,
        Value = value ?? "N/A",
        IsGroupHeader = false
    };

    public static SpecItem Row(string label, int? value) => new()
    {
        Label = label,
        Value = value?.ToString() ?? "N/A",
        IsGroupHeader = false
    };

    public static SpecItem Row(string label, long? value) => new()
    {
        Label = label,
        Value = value?.ToString() ?? "N/A",
        IsGroupHeader = false
    };

    public static SpecItem Row(string label, double? value, string format = "F1") => new()
    {
        Label = label,
        Value = value.HasValue ? value.Value.ToString(format) : "N/A",
        IsGroupHeader = false
    };

    public static SpecItem RowIf(string label, string? value) =>
        !string.IsNullOrWhiteSpace(value) ? Row(label, value) : Row(label, (string?)null);

    public static SpecItem RowIf(string label, int? value) =>
        value.HasValue && value.Value > 0 ? Row(label, value) : Row(label, (int?)null);

    public static SpecItem RowIf(string label, long? value) =>
        value.HasValue && value.Value > 0 ? Row(label, value) : Row(label, (long?)null);

    public static SpecItem RowIf(string label, double? value, string format = "F1") =>
        value.HasValue && value.Value > 0 ? Row(label, value, format) : Row(label, (double?)null);
}
