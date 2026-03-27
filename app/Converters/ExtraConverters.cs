using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RegProbe.App.Converters;

public class NestedMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isNested && isNested)
        {
            return new Thickness(24, 0, 0, 0);
        }
        return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return Visibility.Collapsed;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Collapsed;
    }
}

public class SparklinePointsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<double> values)
        {
            var points = new PointCollection();
            var list = values.ToList();
            if (list.Count == 0) return points;

            // Polyline Stretch="Fill", so strictly X coordinates can be 0..N-1
            // Y coordinates should be values.
            
            // Assuming values are normalized 0-100 or simply raw values.
            // If raw values, we might need to scale? 
            // The XAML uses Stretch="Fill", so X range doesn't matter too much, just need sequence.
            // Y range: The generic graph usually needs scaling if execution values vary wildly.
            // But let's assume pre-normalized or safe values.
            // Let's iterate index i.
            
            for (int i = 0; i < list.Count; i++)
            {
                points.Add(new Point(i * 10, list[i]));
            }
            return points;
        }
        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
