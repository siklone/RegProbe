using System;
using System.Windows;

namespace WindowsOptimizer.App.Services;

public enum AppTheme
{
    Dark,
    Light
}

public static class ThemeManager
{
    private static AppTheme _currentTheme = AppTheme.Dark;

    public static AppTheme CurrentTheme => _currentTheme;

    public static event Action<AppTheme>? ThemeChanged;

    public static void SetTheme(AppTheme theme, bool force = false)
    {
        if (!force && _currentTheme == theme) return;

        _currentTheme = theme;

        var app = Application.Current;
        if (app == null) return;

        // Find and remove existing theme dictionary (Colors.xaml or Colors.Light.xaml)
        ResourceDictionary? existingTheme = null;
        foreach (var dict in app.Resources.MergedDictionaries)
        {
            if (dict.Source != null)
            {
                var source = dict.Source.OriginalString;
                if (source.Contains("Colors.xaml") || source.Contains("Colors.Light.xaml"))
                {
                    existingTheme = dict;
                    break;
                }
            }
        }

        if (existingTheme != null)
        {
            app.Resources.MergedDictionaries.Remove(existingTheme);
        }

        // Add new theme dictionary
        var themePath = theme switch
        {
            AppTheme.Light => "Resources/Colors.Light.xaml",
            _ => "Resources/Colors.xaml"
        };

        var newTheme = new ResourceDictionary
        {
            Source = new Uri(themePath, UriKind.Relative)
        };

        // Insert at beginning so it's loaded first
        app.Resources.MergedDictionaries.Insert(0, newTheme);

        ThemeChanged?.Invoke(theme);
    }

    public static void Initialize(AppTheme theme)
    {
        // Just track the theme state without changing dictionaries
        // SetTheme with force=true will be called separately if needed
        _currentTheme = theme;
    }
}
