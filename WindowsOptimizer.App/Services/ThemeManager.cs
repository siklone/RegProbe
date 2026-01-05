using System;
using System.Windows;
using System.Windows.Media;

namespace WindowsOptimizer.App.Services;

public record ThemePalette(
    string Name,
    Color AccentPrimary,    // Nord8
    Color AccentDark,       // Nord10
    Color AccentLight,      // Nord7
    Color AccentSecondary   // Nord9
);

public class ThemeManager
{
    private static readonly ResourceDictionary _resources = Application.Current.Resources;

    public static readonly ThemePalette Nord = new(
        "Nord",
        (Color)ColorConverter.ConvertFromString("#88C0D0"), // Nord8
        (Color)ColorConverter.ConvertFromString("#5E81AC"), // Nord10
        (Color)ColorConverter.ConvertFromString("#8FBCBB"), // Nord7
        (Color)ColorConverter.ConvertFromString("#81A1C1")  // Nord9
    );

    public static readonly ThemePalette ElectricPurple = new(
        "Electric",
        (Color)ColorConverter.ConvertFromString("#BD93F9"), // Dracula Purple
        (Color)ColorConverter.ConvertFromString("#6272A4"), // Dracula Comment
        (Color)ColorConverter.ConvertFromString("#FF79C6"), // Dracula Pink
        (Color)ColorConverter.ConvertFromString("#8BE9FD")  // Dracula Cyan
    );

    public static readonly ThemePalette SunsetOrange = new(
        "Sunset",
        (Color)ColorConverter.ConvertFromString("#D08770"), // Nord12
        (Color)ColorConverter.ConvertFromString("#BF616A"), // Nord11
        (Color)ColorConverter.ConvertFromString("#EBCB8B"), // Nord13
        (Color)ColorConverter.ConvertFromString("#D08770")  // Nord12
    );

    public static readonly ThemePalette CyberGreen = new(
        "Cyber",
        (Color)ColorConverter.ConvertFromString("#50FA7B"), // Bright Green
        (Color)ColorConverter.ConvertFromString("#008F39"), // Dark Green
        (Color)ColorConverter.ConvertFromString("#8AFF80"), // Light Green
        (Color)ColorConverter.ConvertFromString("#00D15B")  // Mid Green
    );

    public void ApplyTheme(ThemePalette palette)
    {
        // Update Colors
        UpdateColor("AccentBrightCyanBrush", palette.AccentPrimary); // Main Accent
        UpdateColor("AccentDarkBlueBrush", palette.AccentDark);      // Darker Accent
        UpdateColor("AccentCyanBrush", palette.AccentLight);         // Light Accent
        UpdateColor("AccentBlueBrush", palette.AccentSecondary);     // Secondary Accent

        // Re-construct Gradient
        var gradient = new LinearGradientBrush();
        gradient.StartPoint = new Point(0, 0);
        gradient.EndPoint = new Point(1, 0);
        gradient.GradientStops.Add(new GradientStop(palette.AccentPrimary, 0));
        gradient.GradientStops.Add(new GradientStop(palette.AccentDark, 1));
        
        if (_resources.Contains("AccentGradientBrush"))
            _resources["AccentGradientBrush"] = gradient;
        else
            _resources.Add("AccentGradientBrush", gradient);
    }

    private void UpdateColor(string resourceKey, Color newColor)
    {
        var brush = new SolidColorBrush(newColor);
        // Freezable for performance since we might swap it
        if (brush.CanFreeze) brush.Freeze();

        if (_resources.Contains(resourceKey))
            _resources[resourceKey] = brush;
        else
            _resources.Add(resourceKey, brush);
    }
}
