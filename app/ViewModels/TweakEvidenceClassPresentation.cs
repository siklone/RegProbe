using System.Windows.Media;

namespace RegProbe.App.ViewModels;

internal static class TweakEvidenceClassPresentation
{
    private static readonly SolidColorBrush ClassABrush = CreateFrozenBrush("#A3BE8C");
    private static readonly SolidColorBrush ClassABackgroundBrush = CreateFrozenBrush("#2AA3BE8C");
    private static readonly SolidColorBrush ClassBBrush = CreateFrozenBrush("#88C0D0");
    private static readonly SolidColorBrush ClassBBackgroundBrush = CreateFrozenBrush("#2A88C0D0");
    private static readonly SolidColorBrush ClassCBrush = CreateFrozenBrush("#EBCB8B");
    private static readonly SolidColorBrush ClassCBackgroundBrush = CreateFrozenBrush("#2AEBCB8B");
    private static readonly SolidColorBrush ClassDBrush = CreateFrozenBrush("#D08770");
    private static readonly SolidColorBrush ClassDBackgroundBrush = CreateFrozenBrush("#2AD08770");
    private static readonly SolidColorBrush ClassEBrush = CreateFrozenBrush("#4C566A");
    private static readonly SolidColorBrush ClassEBackgroundBrush = CreateFrozenBrush("#2A4C566A");

    public static Brush GetBrush(string evidenceClassId) => evidenceClassId switch
    {
        "A" => ClassABrush,
        "B" => ClassBBrush,
        "C" => ClassCBrush,
        "D" => ClassDBrush,
        _ => ClassEBrush
    };

    public static Brush GetBackgroundBrush(string evidenceClassId) => evidenceClassId switch
    {
        "A" => ClassABackgroundBrush,
        "B" => ClassBBackgroundBrush,
        "C" => ClassCBackgroundBrush,
        "D" => ClassDBackgroundBrush,
        _ => ClassEBackgroundBrush
    };

    private static SolidColorBrush CreateFrozenBrush(string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
