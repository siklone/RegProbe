using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace WindowsOptimizer.App.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private void InnerScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var scv = sender as ScrollViewer;
        if (scv == null) return;

        // Check if ScrollViewer is completely shown (no scroll needed)
        // OR we are at the top and scrolling up
        // OR we are at the bottom and scrolling down
        bool noScrollNeeded = scv.ScrollableHeight <= 0;
        bool atTop = scv.VerticalOffset <= 0;
        bool atBottom = scv.VerticalOffset >= scv.ScrollableHeight;

        if (noScrollNeeded || (e.Delta > 0 && atTop) || (e.Delta < 0 && atBottom))
        {
            e.Handled = true;
            
            // Re-raise the event to the parent
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = sender
            };
            
            var parent = VisualTreeHelper.GetParent(scv) as UIElement;
            parent?.RaiseEvent(eventArg);
        }
    }
}

/// <summary>
/// Converts a percentage value to a width based on the parent container's width.
/// </summary>
public class PercentToWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 &&
            values[0] is int percent &&
            values[1] is double totalWidth)
        {
            return totalWidth * percent / 100.0;
        }
        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a string to Visibility - Visible if not null/empty, Collapsed otherwise.
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
