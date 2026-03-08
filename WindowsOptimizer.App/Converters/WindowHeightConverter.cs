using System;
using System.Globalization;
using System.Windows.Data;

namespace WindowsOptimizer.App.Converters;

public sealed class WindowHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double actualHeight && actualHeight > 0)
        {
            var factor = 0.75;
            if (parameter != null)
            {
                if (double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var paramFactor))
                {
                    factor = paramFactor;
                }
            }
            return actualHeight * factor;
        }
        return 480d;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
