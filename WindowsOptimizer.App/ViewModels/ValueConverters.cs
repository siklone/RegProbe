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
            var autoScale = parameter is string s && s.Contains("auto", StringComparison.OrdinalIgnoreCase);
            var pointsList = System.Linq.Enumerable.ToList(points);
            if (pointsList.Count == 0) return DependencyProperty.UnsetValue;

            // Guard against NaN/Infinity values from perf counters and other sources.
            for (var i = 0; i < pointsList.Count; i++)
            {
                var v = pointsList[i];
                if (!double.IsFinite(v) || v < 0)
                {
                    pointsList[i] = 0;
                }
            }

            var maxValue = System.Linq.Enumerable.Max(pointsList);
            var scaleMax = autoScale ? Math.Max(1.0, maxValue) : (maxValue <= 100 ? 100.0 : Math.Max(1.0, maxValue));

            var pc = new System.Windows.Media.PointCollection(pointsList.Count);
            double x = 0;
            double canvasWidth = 600.0;
            double canvasHeight = 100.0;
            double xStep = pointsList.Count > 1 ? canvasWidth / (pointsList.Count - 1) : 0;

            foreach (var p in pointsList)
            {
                pc.Add(new Point(x, canvasHeight - (p / scaleMax * canvasHeight)));
                x += xStep;
            }
            pc.Freeze();
            return pc;
        }
        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a list of values to a closed polygon for area fill under the sparkline.
/// Creates points along bottom edge to form a filled area shape.
/// </summary>
public sealed class SparklineAreaConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Collections.Generic.IEnumerable<double> points)
        {
            var autoScale = parameter is string s && s.Contains("auto", StringComparison.OrdinalIgnoreCase);
            var pointsList = System.Linq.Enumerable.ToList(points);
            if (pointsList.Count == 0) return DependencyProperty.UnsetValue;

            // Guard against NaN/Infinity values
            for (var i = 0; i < pointsList.Count; i++)
            {
                var v = pointsList[i];
                if (!double.IsFinite(v) || v < 0)
                {
                    pointsList[i] = 0;
                }
            }

            var maxValue = System.Linq.Enumerable.Max(pointsList);
            var scaleMax = autoScale ? Math.Max(1.0, maxValue) : (maxValue <= 100 ? 100.0 : Math.Max(1.0, maxValue));

            // Create closed polygon: line points + bottom edge
            var pc = new System.Windows.Media.PointCollection(pointsList.Count + 2);
            double x = 0;
            double canvasWidth = 600.0;
            double canvasHeight = 100.0;
            double xStep = pointsList.Count > 1 ? canvasWidth / (pointsList.Count - 1) : 0;

            // Add line points
            foreach (var p in pointsList)
            {
                pc.Add(new Point(x, canvasHeight - (p / scaleMax * canvasHeight)));
                x += xStep;
            }

            // Close the polygon by adding bottom-right and bottom-left corners
            pc.Add(new Point(canvasWidth, canvasHeight));
            pc.Add(new Point(0, canvasHeight));

            pc.Freeze();
            return pc;
        }
        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class SparklinePointsWithMaxConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not System.Collections.Generic.IEnumerable<double> points)
        {
            return DependencyProperty.UnsetValue;
        }

        var pointsList = System.Linq.Enumerable.ToList(points);
        if (pointsList.Count == 0) return DependencyProperty.UnsetValue;

        for (var i = 0; i < pointsList.Count; i++)
        {
            var v = pointsList[i];
            if (!double.IsFinite(v) || v < 0)
            {
                pointsList[i] = 0;
            }
        }

        var seriesMax = System.Linq.Enumerable.Max(pointsList);
        var providedMax = values[1] is double max && double.IsFinite(max) ? max : 0;
        var scaleMax = Math.Max(1.0, Math.Max(seriesMax, providedMax));

        var pc = new System.Windows.Media.PointCollection(pointsList.Count);
        double x = 0;
        double canvasWidth = 600.0;
        double canvasHeight = 100.0;
        double xStep = pointsList.Count > 1 ? canvasWidth / (pointsList.Count - 1) : 0;

        foreach (var p in pointsList)
        {
            pc.Add(new Point(x, canvasHeight - (p / scaleMax * canvasHeight)));
            x += xStep;
        }

        pc.Freeze();
        return pc;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class SparklineAreaWithMaxConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not System.Collections.Generic.IEnumerable<double> points)
        {
            return DependencyProperty.UnsetValue;
        }

        var pointsList = System.Linq.Enumerable.ToList(points);
        if (pointsList.Count == 0) return DependencyProperty.UnsetValue;

        for (var i = 0; i < pointsList.Count; i++)
        {
            var v = pointsList[i];
            if (!double.IsFinite(v) || v < 0)
            {
                pointsList[i] = 0;
            }
        }

        var seriesMax = System.Linq.Enumerable.Max(pointsList);
        var providedMax = values[1] is double max && double.IsFinite(max) ? max : 0;
        var scaleMax = Math.Max(1.0, Math.Max(seriesMax, providedMax));

        var pc = new System.Windows.Media.PointCollection(pointsList.Count + 2);
        double x = 0;
        double canvasWidth = 600.0;
        double canvasHeight = 100.0;
        double xStep = pointsList.Count > 1 ? canvasWidth / (pointsList.Count - 1) : 0;

        foreach (var p in pointsList)
        {
            pc.Add(new Point(x, canvasHeight - (p / scaleMax * canvasHeight)));
            x += xStep;
        }

        pc.Add(new Point(canvasWidth, canvasHeight));
        pc.Add(new Point(0, canvasHeight));

        pc.Freeze();
        return pc;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class LastValueToYWithMaxConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not System.Collections.Generic.IEnumerable<double> points)
        {
            return 50.0;
        }

        var pointsList = System.Linq.Enumerable.ToList(points);
        if (pointsList.Count == 0) return 50.0;

        for (var i = 0; i < pointsList.Count; i++)
        {
            var v = pointsList[i];
            if (!double.IsFinite(v) || v < 0)
            {
                pointsList[i] = 0;
            }
        }

        var lastValue = pointsList[pointsList.Count - 1];
        var seriesMax = System.Linq.Enumerable.Max(pointsList);
        var providedMax = values[1] is double max && double.IsFinite(max) ? max : 0;
        var scaleMax = Math.Max(1.0, Math.Max(seriesMax, providedMax));

        double canvasHeight = 100.0;
        return canvasHeight - (lastValue / scaleMax * canvasHeight) - 5;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Gets the last value from a collection to position the current value indicator.
/// </summary>
public sealed class LastValueToYConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Collections.Generic.IEnumerable<double> points)
        {
            var autoScale = parameter is string s && s.Contains("auto", StringComparison.OrdinalIgnoreCase);
            var pointsList = System.Linq.Enumerable.ToList(points);
            if (pointsList.Count == 0) return 50.0;

            var lastValue = pointsList[pointsList.Count - 1];
            if (!double.IsFinite(lastValue) || lastValue < 0)
            {
                lastValue = 0;
            }

            var maxValue = System.Linq.Enumerable.Max(pointsList);
            for (var i = 0; i < pointsList.Count; i++)
            {
                var v = pointsList[i];
                if (!double.IsFinite(v) || v < 0)
                {
                    pointsList[i] = 0;
                }
            }
            maxValue = System.Linq.Enumerable.Max(pointsList);
            var scaleMax = autoScale ? Math.Max(1.0, maxValue) : (maxValue <= 100 ? 100.0 : Math.Max(1.0, maxValue));

            double canvasHeight = 100.0;
            return canvasHeight - (lastValue / scaleMax * canvasHeight) - 5; // -5 to center the dot
        }
        return 50.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
