using System;
using System.Windows;
using System.Windows.Controls;
using WindowsOptimizer.App.Services;

namespace WindowsOptimizer.App.Views.Controls;

/// <summary>
/// Host control for displaying toast notifications.
/// </summary>
public partial class NotificationHost : UserControl
{
    private NotificationService? _notificationService;

    public NotificationHost()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the notification service to display.
    /// </summary>
    public NotificationService? NotificationService
    {
        get => _notificationService;
        set
        {
            if (_notificationService != null)
            {
                _notificationService.NotificationAdded -= OnNotificationAdded;
            }

            _notificationService = value;
            
            if (_notificationService != null)
            {
                NotificationItems.ItemsSource = _notificationService.Notifications;
                _notificationService.NotificationAdded += OnNotificationAdded;
            }
        }
    }

    private void OnNotificationAdded(object? sender, ToastNotification notification)
    {
        // Trigger slide-in animation for new notifications
        Dispatcher.InvokeAsync(() =>
        {
            var container = NotificationItems.ItemContainerGenerator
                .ContainerFromItem(notification) as ContentPresenter;

            if (container != null)
            {
                var storyboard = (System.Windows.Media.Animation.Storyboard)Resources["SlideIn"];
                var border = FindVisualChild<Border>(container);
                if (border != null)
                {
                    storyboard.Begin(border);
                }
            }
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void DismissButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Guid id)
        {
            _notificationService?.Dismiss(id);
        }
    }

    /// <summary>
    /// Finds a visual child of the specified type.
    /// </summary>
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
                return typedChild;

            var foundChild = FindVisualChild<T>(child);
            if (foundChild != null)
                return foundChild;
        }
        return null;
    }
}
