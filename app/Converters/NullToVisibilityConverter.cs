using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RegProbe.App.Converters;

public sealed class NullToVisibilityConverter : IValueConverter
{
    public Visibility NullValue { get; set; } = Visibility.Collapsed;
    public Visibility NotNullValue { get; set; } = Visibility.Visible;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is null ? NullValue : NotNullValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
