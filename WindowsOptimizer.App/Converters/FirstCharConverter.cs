using System.Globalization;
using System.Windows.Data;

namespace WindowsOptimizer.App.Converters;

/// <summary>
/// Converts a string to its first character (for icon placeholders).
/// </summary>
public class FirstCharConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrEmpty(str))
        {
            return str[0].ToString().ToUpper();
        }
        return "?";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
