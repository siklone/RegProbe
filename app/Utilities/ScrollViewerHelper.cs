using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace RegProbe.App.Utilities;

public static class ScrollViewerHelper
{
    public static readonly DependencyProperty EnableMouseWheelScrollProperty =
        DependencyProperty.RegisterAttached(
            "EnableMouseWheelScroll",
            typeof(bool),
            typeof(ScrollViewerHelper),
            new PropertyMetadata(false, OnEnableMouseWheelScrollChanged));

    public static readonly DependencyProperty ScrollToEndOnChangeProperty =
        DependencyProperty.RegisterAttached(
            "ScrollToEndOnChange",
            typeof(object),
            typeof(ScrollViewerHelper),
            new PropertyMetadata(null, OnScrollToEndOnChangeChanged));

    public static void SetEnableMouseWheelScroll(DependencyObject element, bool value)
    {
        element.SetValue(EnableMouseWheelScrollProperty, value);
    }

    public static bool GetEnableMouseWheelScroll(DependencyObject element)
    {
        return (bool)element.GetValue(EnableMouseWheelScrollProperty);
    }

    public static void SetScrollToEndOnChange(DependencyObject element, object? value)
    {
        element.SetValue(ScrollToEndOnChangeProperty, value);
    }

    public static object? GetScrollToEndOnChange(DependencyObject element)
    {
        return element.GetValue(ScrollToEndOnChangeProperty);
    }
    private static void OnEnableMouseWheelScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        if (e.NewValue is true)
        {
            element.PreviewMouseWheel += OnPreviewMouseWheel;
        }
        else
        {
            element.PreviewMouseWheel -= OnPreviewMouseWheel;
        }
    }

    private static void OnScrollToEndOnChangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer scrollViewer || Equals(e.NewValue, e.OldValue))
        {
            return;
        }

        scrollViewer.Dispatcher.BeginInvoke(
            new Action(scrollViewer.ScrollToEnd),
            DispatcherPriority.Background);
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not DependencyObject source)
        {
            return;
        }

        var scrollViewer = FindScrollViewer(source) ?? FindAncestorScrollViewer(source);
        while (scrollViewer != null)
        {
            if (TryScroll(scrollViewer, e.Delta))
            {
                e.Handled = true;
                return;
            }

            scrollViewer = FindAncestorScrollViewer(scrollViewer);
        }
    }

    private static bool TryScroll(ScrollViewer scrollViewer, int wheelDelta)
    {
        if (scrollViewer.ScrollableHeight <= 0)
        {
            return false;
        }

        var lines = Math.Max(1, SystemParameters.WheelScrollLines);
        var delta = -wheelDelta / 120.0;
        var offset = scrollViewer.VerticalOffset + delta * lines * 16;
        offset = Math.Max(0, Math.Min(offset, scrollViewer.ScrollableHeight));
        if (Math.Abs(offset - scrollViewer.VerticalOffset) < 0.1)
        {
            return false;
        }

        scrollViewer.ScrollToVerticalOffset(offset);
        return true;
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        if (root is ScrollViewer viewer)
        {
            return viewer;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var found = FindScrollViewer(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static ScrollViewer? FindAncestorScrollViewer(DependencyObject element)
    {
        var parent = VisualTreeHelper.GetParent(element);
        while (parent != null)
        {
            if (parent is ScrollViewer viewer)
            {
                return viewer;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }
}
