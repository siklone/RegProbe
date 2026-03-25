using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OpenTraceProject.App.Converters;

/// <summary>
/// Converts a tab name (string) to Visibility.
/// Parameter specifies the target tab name.
/// If Value == Parameter, returns Visible, else Collapsed.
/// </summary>
public class TabToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return Visibility.Collapsed;

        string currentTab = value.ToString() ?? string.Empty;
        string targetTab = parameter.ToString() ?? string.Empty;

        return currentTab.Equals(targetTab, StringComparison.InvariantCultureIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
