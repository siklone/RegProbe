using System.Windows.Media;

namespace RegProbe.App.ViewModels;

internal static class TweakStatusPresentation
{
    private static readonly SolidColorBrush AppliedStatusBrush = CreateFrozenBrush("#A3BE8C");
    private static readonly SolidColorBrush NotAppliedStatusBrush = CreateFrozenBrush("#666666");
    private static readonly SolidColorBrush NotAppliedStatusBorderBrush = CreateFrozenBrush("#333333");
    private static readonly SolidColorBrush MixedStatusBrush = CreateFrozenBrush("#D08770");
    private static readonly SolidColorBrush ErrorStatusBrush = CreateFrozenBrush("#BF616A");
    private static readonly SolidColorBrush UnknownStatusBrush = CreateFrozenBrush("#88C0D0");

    private static readonly SolidColorBrush AppliedStatusBackgroundBrush = CreateFrozenBrush("#2AA3BE8C");
    private static readonly SolidColorBrush NotAppliedStatusBackgroundBrush = CreateFrozenBrush("#1A1A1A");
    private static readonly SolidColorBrush MixedStatusBackgroundBrush = CreateFrozenBrush("#2AD08770");
    private static readonly SolidColorBrush ErrorStatusBackgroundBrush = CreateFrozenBrush("#2ABF616A");
    private static readonly SolidColorBrush UnknownStatusBackgroundBrush = CreateFrozenBrush("#2A88C0D0");

    public static string BuildTooltip(
        TweakAppliedStatus appliedStatus,
        bool showMixedStatus,
        bool requiresAdminScan) => appliedStatus switch
    {
        _ when showMixedStatus => "Mixed. Some sub-items match the desired configuration.",
        TweakAppliedStatus.Applied => "Applied. Current state matches the desired configuration.",
        TweakAppliedStatus.NotApplied => "Not applied. Detected state differs from the desired configuration.",
        TweakAppliedStatus.Error => "Error. Open Execution Log for details.",
        _ when requiresAdminScan => "Unknown. Run an admin detect to read current state.",
        _ => "Unknown. Click Detect to read current state."
    };

    public static string BuildIcon(
        TweakAppliedStatus appliedStatus,
        bool showMixedStatus,
        bool requiresAdminScan) => appliedStatus switch
    {
        _ when showMixedStatus => "M",
        TweakAppliedStatus.Applied => "+",
        TweakAppliedStatus.NotApplied => "o",
        TweakAppliedStatus.Error => "x",
        _ when requiresAdminScan => "!",
        _ => "?"
    };

    public static Brush GetStatusBrush(
        TweakAppliedStatus appliedStatus,
        bool showMixedStatus,
        bool requiresAdminScan) => appliedStatus switch
    {
        _ when showMixedStatus => MixedStatusBrush,
        TweakAppliedStatus.Applied => AppliedStatusBrush,
        TweakAppliedStatus.NotApplied => NotAppliedStatusBrush,
        TweakAppliedStatus.Error => ErrorStatusBrush,
        _ when requiresAdminScan => NotAppliedStatusBrush,
        _ => UnknownStatusBrush
    };

    public static Brush GetBorderBrush(
        TweakAppliedStatus appliedStatus,
        bool showMixedStatus,
        bool requiresAdminScan) => appliedStatus switch
    {
        TweakAppliedStatus.NotApplied => NotAppliedStatusBorderBrush,
        _ when requiresAdminScan => NotAppliedStatusBorderBrush,
        _ => GetStatusBrush(appliedStatus, showMixedStatus, requiresAdminScan)
    };

    public static Brush GetTextBrush(
        TweakAppliedStatus appliedStatus,
        bool showMixedStatus,
        bool requiresAdminScan) => appliedStatus switch
    {
        TweakAppliedStatus.NotApplied => NotAppliedStatusBrush,
        _ when requiresAdminScan => NotAppliedStatusBrush,
        _ => GetStatusBrush(appliedStatus, showMixedStatus, requiresAdminScan)
    };

    public static Brush GetBadgeBackground(
        TweakAppliedStatus appliedStatus,
        bool showMixedStatus,
        bool requiresAdminScan) => appliedStatus switch
    {
        _ when showMixedStatus => MixedStatusBackgroundBrush,
        TweakAppliedStatus.Applied => AppliedStatusBackgroundBrush,
        TweakAppliedStatus.NotApplied => NotAppliedStatusBackgroundBrush,
        TweakAppliedStatus.Error => ErrorStatusBackgroundBrush,
        _ when requiresAdminScan => NotAppliedStatusBackgroundBrush,
        _ => UnknownStatusBackgroundBrush
    };

    public static string BuildText(
        TweakAppliedStatus appliedStatus,
        bool showMixedStatus,
        bool requiresAdminScan) => appliedStatus switch
    {
        _ when showMixedStatus => "Mixed",
        TweakAppliedStatus.Applied => "Applied",
        TweakAppliedStatus.NotApplied => "Not Applied",
        TweakAppliedStatus.Error => "Error",
        _ when requiresAdminScan => "Needs Admin",
        _ => "Unknown"
    };

    private static SolidColorBrush CreateFrozenBrush(string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
