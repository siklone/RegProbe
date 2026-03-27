using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RegProbe.App.Converters;

public sealed class StringEmptyToVisibilityConverter : IValueConverter
{
    public Visibility EmptyValue { get; set; } = Visibility.Collapsed;
    public Visibility NotEmptyValue { get; set; } = Visibility.Visible;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.IsNullOrEmpty(value as string) ? EmptyValue : NotEmptyValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
