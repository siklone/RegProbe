using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WindowsOptimizer.App.Diagnostics;
using WindowsOptimizer.App.ViewModels;

namespace WindowsOptimizer.App.Views;

public partial class MonitorView : UserControl
{
    private Point _layoutDragStart;
    private MonitorSectionLayout? _draggedSection;

    public MonitorView()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("CRITICAL: MonitorView constructor failed", ex);

            Content = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x2E, 0x34, 0x40)),
                Padding = new System.Windows.Thickness(16),
                Child = new TextBox
                {
                    IsReadOnly = true,
                    TextWrapping = System.Windows.TextWrapping.Wrap,
                    Background = new SolidColorBrush(Color.FromRgb(0x3B, 0x42, 0x52)),
                    Foreground = new SolidColorBrush(Color.FromRgb(0xEC, 0xEF, 0xF4)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x56, 0x6A)),
                    Text =
                        "Monitor view failed to load.\n\n" +
                        $"{ex.GetType().Name}: {ex.Message}\n\n" +
                        "Details were written to the application diagnostics log."
                }
            };
        }
    }

    private void OnLayoutItemPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _layoutDragStart = e.GetPosition(this);
        _draggedSection = GetSectionFromOriginalSource(e.OriginalSource);
    }

    private void OnLayoutItemPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedSection is null)
        {
            return;
        }

        var position = e.GetPosition(this);
        if (Math.Abs(position.X - _layoutDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - _layoutDragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        DragDrop.DoDragDrop(this, _draggedSection, DragDropEffects.Move);
    }

    private void OnLayoutItemDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(MonitorSectionLayout)))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void OnLayoutItemDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(MonitorSectionLayout)))
        {
            return;
        }

        var dropped = e.Data.GetData(typeof(MonitorSectionLayout)) as MonitorSectionLayout;
        if (dropped is null)
        {
            return;
        }

        var viewModel = DataContext as MonitorViewModel;
        if (viewModel is null)
        {
            return;
        }

        var target = GetSectionFromOriginalSource(e.OriginalSource);
        var sections = viewModel.MonitorSections;
        var oldIndex = sections.IndexOf(dropped);
        if (oldIndex < 0)
        {
            return;
        }

        var newIndex = target is null ? sections.Count - 1 : sections.IndexOf(target);
        if (newIndex < 0 || newIndex == oldIndex)
        {
            return;
        }

        sections.Move(oldIndex, newIndex);
    }

    private static MonitorSectionLayout? GetSectionFromOriginalSource(object? originalSource)
    {
        if (originalSource is not DependencyObject dependencyObject)
        {
            return null;
        }

        var container = FindAncestor<ListBoxItem>(dependencyObject);
        return container?.DataContext as MonitorSectionLayout;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T candidate)
            {
                return candidate;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!e.Handled)
        {
            e.Handled = true;
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            var parent = ((Control)sender).Parent as UIElement;
            parent?.RaiseEvent(eventArg);
        }
    }

    private void PerformanceTiles_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MonitorViewModel viewModel)
        {
            return;
        }

        var container = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (container?.DataContext is MonitorViewModel.PerformanceItemViewModel item)
        {
            viewModel.TogglePerformanceDetail(item);
        }
    }

    private void PerformanceTiles_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter and not Key.Space)
        {
            return;
        }

        if (DataContext is not MonitorViewModel viewModel ||
            sender is not ListBox listBox ||
            listBox.SelectedItem is not MonitorViewModel.PerformanceItemViewModel item)
        {
            return;
        }

        viewModel.OpenPerformanceDetail(item);
        e.Handled = true;
    }

    private void PerformanceSections_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MonitorViewModel viewModel)
        {
            return;
        }

        var container = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (container?.DataContext is MonitorSectionLayout section)
        {
            viewModel.TogglePerformanceSectionDetail(section);
        }
    }

    private void PerformanceSections_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter and not Key.Space)
        {
            return;
        }

        if (DataContext is not MonitorViewModel viewModel ||
            sender is not ListBox listBox ||
            listBox.SelectedItem is not MonitorSectionLayout section)
        {
            return;
        }

        viewModel.OpenPerformanceSectionDetail(section);
        e.Handled = true;
    }
}
