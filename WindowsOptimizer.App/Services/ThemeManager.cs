using System;
using System.Linq;
using System.Windows;
using WindowsOptimizer.App.Diagnostics;

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
        AppDiagnostics.Log($"SetTheme called: theme={theme}, force={force}, current={_currentTheme}");

        if (!force && _currentTheme == theme)
        {
            AppDiagnostics.Log("SetTheme: skipped (same theme)");
            return;
        }

        _currentTheme = theme;

        var app = Application.Current;
        if (app == null)
        {
            AppDiagnostics.Log("SetTheme: Application.Current is null");
            return;
        }

        // Log all merged dictionaries for debugging
        AppDiagnostics.Log($"SetTheme: Found {app.Resources.MergedDictionaries.Count} merged dictionaries");
        foreach (var d in app.Resources.MergedDictionaries)
        {
            AppDiagnostics.Log($"  - {d.Source?.OriginalString ?? "(no source)"}");
        }

        // Find and remove existing theme dictionary
        var existingThemes = app.Resources.MergedDictionaries
            .Where(d => d.Source != null && d.Source.OriginalString.Contains("Colors"))
            .ToList();

        AppDiagnostics.Log($"SetTheme: Found {existingThemes.Count} color dictionaries to remove");

        foreach (var existing in existingThemes)
        {
            AppDiagnostics.Log($"SetTheme: Removing {existing.Source?.OriginalString}");
            app.Resources.MergedDictionaries.Remove(existing);
        }

        // Add new theme dictionary using pack URI
        var themePath = theme switch
        {
            AppTheme.Light => "pack://application:,,,/Resources/Colors.Light.xaml",
            _ => "pack://application:,,,/Resources/Colors.xaml"
        };

        AppDiagnostics.Log($"SetTheme: Loading new theme from {themePath}");

        try
        {
            var newTheme = new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Absolute)
            };

            // Insert at beginning so it's loaded first (before Styles.xaml etc.)
            app.Resources.MergedDictionaries.Insert(0, newTheme);

            AppDiagnostics.Log($"SetTheme: Successfully loaded {theme} theme");
            ThemeChanged?.Invoke(theme);
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("SetTheme", ex);
        }
    }

    public static void Initialize(AppTheme theme)
    {
        // Just track the theme state without changing dictionaries
        // SetTheme with force=true will be called separately if needed
        _currentTheme = theme;
        AppDiagnostics.Log($"ThemeManager.Initialize: {theme}");
    }
}
