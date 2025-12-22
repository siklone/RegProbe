using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WindowsOptimizer.App.ViewModels;

public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return false;
    }
}

public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return !b ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class NestedMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isNested && isNested) return new Thickness(20, 0, 0, 0);
        return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class NestedBackgroundConverter : IValueConverter
{
    private static readonly System.Windows.Media.Brush NestedBrush = System.Windows.Media.Brushes.Transparent;
    private static readonly System.Windows.Media.Brush RootBrush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#3B4252")!;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isNested && isNested) return NestedBrush;
        return RootBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class ScoreToOffsetConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int score && double.TryParse(parameter?.ToString(), out double circumference))
        {
            return circumference * (1.0 - (score / 100.0));
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class SparklinePointsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Collections.Generic.IEnumerable<double> points)
        {
            var pointsList = points as System.Collections.Generic.IList<double> ?? System.Linq.Enumerable.ToList(points);
            if (!pointsList.Any()) return null;

            var pc = new System.Windows.Media.PointCollection(pointsList.Count);
            double x = 0;
            double canvasWidth = 160.0;
            double canvasHeight = 40.0;
            double xStep = pointsList.Count > 1 ? canvasWidth / (pointsList.Count - 1) : 0;
            
            foreach (var p in pointsList)
            {
                pc.Add(new Point(x, canvasHeight - (p / 100.0 * canvasHeight))); 
                x += xStep;
            }
            return pc;
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
