using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Threading;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// Implementation of INotificationService that displays toast notifications.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ObservableCollection<ToastNotification> _notifications = new();
    private readonly Dispatcher _dispatcher;
    private readonly object _lock = new();

    /// <summary>
    /// Gets the collection of active notifications (for binding in XAML).
    /// </summary>
    public ObservableCollection<ToastNotification> Notifications => _notifications;

    /// <summary>
    /// Maximum number of visible notifications.
    /// </summary>
    public int MaxVisibleNotifications { get; set; } = 5;

    /// <summary>
    /// Event raised when a notification is added.
    /// </summary>
    public event EventHandler<ToastNotification>? NotificationAdded;

    /// <summary>
    /// Event raised when a notification is removed.
    /// </summary>
    public event EventHandler<ToastNotification>? NotificationRemoved;

    /// <summary>
    /// Creates a new NotificationService.
    /// </summary>
    public NotificationService()
    {
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
    }

    /// <inheritdoc />
    public void ShowSuccess(string message, string? title = null, TimeSpan? duration = null)
    {
        Show(new ToastNotification
        {
            Message = message,
            Title = title ?? "Success",
            Type = NotificationType.Success,
            Duration = duration ?? TimeSpan.FromSeconds(3)
        });
    }

    /// <inheritdoc />
    public void ShowWarning(string message, string? title = null, TimeSpan? duration = null)
    {
        Show(new ToastNotification
        {
            Message = message,
            Title = title ?? "Warning",
            Type = NotificationType.Warning,
            Duration = duration ?? TimeSpan.FromSeconds(5)
        });
    }

    /// <inheritdoc />
    public void ShowError(string message, string? title = null, TimeSpan? duration = null)
    {
        Show(new ToastNotification
        {
            Message = message,
            Title = title ?? "Error",
            Type = NotificationType.Error,
            Duration = duration ?? TimeSpan.FromSeconds(7)
        });
    }

    /// <inheritdoc />
    public void ShowInfo(string message, string? title = null, TimeSpan? duration = null)
    {
        Show(new ToastNotification
        {
            Message = message,
            Title = title,
            Type = NotificationType.Info,
            Duration = duration ?? TimeSpan.FromSeconds(3)
        });
    }

    /// <inheritdoc />
    public void Show(ToastNotification notification)
    {
        Debug.WriteLine($"[NotificationService] Showing: {notification.Type} - {notification.Message}");

        _dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                // Remove oldest if at max
                while (_notifications.Count >= MaxVisibleNotifications)
                {
                    var oldest = _notifications[0];
                    _notifications.RemoveAt(0);
                    NotificationRemoved?.Invoke(this, oldest);
                }

                _notifications.Add(notification);
                NotificationAdded?.Invoke(this, notification);
            }
        });

        // Schedule auto-dismiss if duration is set
        if (notification.Duration.HasValue)
        {
            var timer = new System.Timers.Timer(notification.Duration.Value.TotalMilliseconds);
            timer.Elapsed += (_, _) =>
            {
                timer.Stop();
                timer.Dispose();
                Dismiss(notification);
            };
            timer.AutoReset = false;
            timer.Start();
        }
    }

    /// <summary>
    /// Dismisses a specific notification.
    /// </summary>
    public void Dismiss(ToastNotification notification)
    {
        _dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                if (_notifications.Remove(notification))
                {
                    Debug.WriteLine($"[NotificationService] Dismissed: {notification.Message}");
                    NotificationRemoved?.Invoke(this, notification);
                }
            }
        });
    }

    /// <summary>
    /// Dismisses a notification by ID.
    /// </summary>
    public void Dismiss(Guid id)
    {
        _dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                for (int i = _notifications.Count - 1; i >= 0; i--)
                {
                    if (_notifications[i].Id == id)
                    {
                        var notification = _notifications[i];
                        _notifications.RemoveAt(i);
                        NotificationRemoved?.Invoke(this, notification);
                        break;
                    }
                }
            }
        });
    }

    /// <inheritdoc />
    public void DismissAll()
    {
        _dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                var notifications = _notifications.ToArray();
                _notifications.Clear();
                foreach (var notification in notifications)
                {
                    NotificationRemoved?.Invoke(this, notification);
                }
            }
        });

        Debug.WriteLine("[NotificationService] All notifications dismissed");
    }
}
