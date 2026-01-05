using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WindowsOptimizer.App.Converters;

public class ArcConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // usage: <Path Data="{Binding Percentage, Converter={StaticResource ArcConverter}, ConverterParameter=Radius}" />
        // values[0]: current value (double)
        // values[1]: total/maximum value (double, or 100 if omitted)
        // parameter: radius (double) - if not passed as parameter, maybe passed as 3rd value? 
        // Let's stick to standard simpler IMultiValueConverter for bindings

        double value = 0;
        double minimum = 0;
        double maximum = 100;
        double radius = 10; // Default

        if (values.Length > 0) value = ParseDouble(values[0]);
        if (values.Length > 1) maximum = ParseDouble(values[1], 100);
        if (values.Length > 2) radius = ParseDouble(values[2], 10);

        if (value < minimum) value = minimum;
        if (value > maximum) value = maximum;

        // Calculate angle
        double angle = (value - minimum) / (maximum - minimum) * 360;
        
        // Return Geometry
        return GetArcGeometry(angle, radius);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private Geometry GetArcGeometry(double angle, double radius)
    {
        // 0 degrees is usually 3 o'clock. We want 12 o'clock, so subtract 90.
        double startAngle = -90;
        double endAngle = startAngle + angle;

        // If full circle (360), drawing logic is slightly different (ArcSegment doesn't do full circle easily)
        if (angle >= 359.9) endAngle = startAngle + 359.9;

        double startRadians = startAngle * Math.PI / 180;
        double endRadians = endAngle * Math.PI / 180;

        Point center = new Point(radius, radius);
        Point startPoint = new Point(
            center.X + radius * Math.Cos(startRadians),
            center.Y + radius * Math.Sin(startRadians));

        Point endPoint = new Point(
            center.X + radius * Math.Cos(endRadians),
            center.Y + radius * Math.Sin(endRadians));

        Size size = new Size(radius, radius);
        bool isLargeArc = angle > 180;

        StreamGeometry geometry = new StreamGeometry();
        using (StreamGeometryContext ctx = geometry.Open())
        {
            ctx.BeginFigure(startPoint, false, false);
            ctx.ArcTo(endPoint, size, 0, isLargeArc, SweepDirection.Clockwise, true, true);
        }

        return geometry;
    }
    private double ParseDouble(object value, double defaultValue = 0)
    {
        if (value == null) return defaultValue;
        if (value is double d) return d;
        if (value is int i) return i;
        try
        {
            return System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return defaultValue;
        }
    }
}
