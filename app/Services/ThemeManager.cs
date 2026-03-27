using System;
using System.Windows;
using System.Windows.Media;

namespace RegProbe.App.Services;

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
        (Color)ColorConverter.ConvertFromString("#86BBD6"), // Nord8
        (Color)ColorConverter.ConvertFromString("#6F88C7"), // Nord10
        (Color)ColorConverter.ConvertFromString("#8ECAC3"), // Nord7
        (Color)ColorConverter.ConvertFromString("#8AA6E0")  // Nord9
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

    public static readonly ThemePalette RubyRed = new(
        "Ruby",
        (Color)ColorConverter.ConvertFromString("#FF6B6B"), // Coral Red
        (Color)ColorConverter.ConvertFromString("#C92A2A"), // Deep Red
        (Color)ColorConverter.ConvertFromString("#FFA8A8"), // Light Pink-Red
        (Color)ColorConverter.ConvertFromString("#E03131")  // Medium Red
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

    /// <summary>
    /// Enable or disable card shadow effects application-wide.
    /// </summary>
    public void SetCardShadows(bool enabled)
    {
        var effect = enabled 
            ? new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 15,
                ShadowDepth = 2,
                Opacity = 0.3,
                Direction = 270
            }
            : null;

        if (_resources.Contains("CardShadowEffect"))
            _resources["CardShadowEffect"] = effect;
        else
            _resources.Add("CardShadowEffect", effect);
    }

    /// <summary>
    /// Set compact mode spacing values.
    /// </summary>
    public void SetCompactMode(bool compact)
    {
        var padding = compact ? new Thickness(8, 4, 8, 4) : new Thickness(12, 8, 12, 8);
        var margin = compact ? new Thickness(0, 2, 0, 2) : new Thickness(0, 4, 0, 4);
        var fontSize = compact ? 12.0 : 13.0;

        if (_resources.Contains("CardPadding"))
            _resources["CardPadding"] = padding;
        else
            _resources.Add("CardPadding", padding);

        if (_resources.Contains("CardMargin"))
            _resources["CardMargin"] = margin;
        else
            _resources.Add("CardMargin", margin);

        if (_resources.Contains("CompactFontSize"))
            _resources["CompactFontSize"] = fontSize;
        else
            _resources.Add("CompactFontSize", fontSize);
    }

    /// <summary>
    /// Set the base theme (Dark or Light) by swapping ResourceDictionaries.
    /// </summary>
    public void SetBaseTheme(bool isDark)
    {
        try
        {
            var dictName = isDark ? "Colors.xaml" : "Colors.Light.xaml";
            var uri = new Uri($"/RegProbe.App;component/Resources/{dictName}", UriKind.Relative);
            
            var newDict = new ResourceDictionary { Source = uri };

            // Find and replace the existing Colors dictionary
            // We identify it by checking a known key like "BackgroundDarkestBrush" or simply by its Source URI if possible
            // But MergedDictionaries access by Source can be tricky if not absolute.
            // Safer strategy: Remove any dictionary that contains "BackgroundDarkestBrush" and add the new one.
            
            var mergedDicts = Application.Current.Resources.MergedDictionaries;
            ResourceDictionary? oldDict = null;

            foreach (var dict in mergedDicts)
            {
                if (dict.Contains("BackgroundDarkestBrush"))
                {
                    oldDict = dict;
                    break;
                }
            }

            if (oldDict != null)
            {
                mergedDicts.Remove(oldDict);
            }
            
            mergedDicts.Add(newDict);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting base theme: {ex.Message}");
        }
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
