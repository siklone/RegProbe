using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WindowsOptimizer.App.Converters;

/// <summary>
/// Converts an Enum (or string) to Boolean based on a parameter.
/// If Value == Parameter, returns True.
/// </summary>
public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        string checkValue = value.ToString() ?? string.Empty;
        string targetValue = parameter.ToString() ?? string.Empty;

        return checkValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter != null)
        {
            return parameter.ToString()!;
        }
        return Binding.DoNothing;
    }
}
