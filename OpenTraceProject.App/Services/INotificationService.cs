using System;

namespace OpenTraceProject.App.Services;

/// <summary>
/// Service for displaying toast notifications to the user.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows a success notification.
    /// </summary>
    /// <param name="message">Message to display.</param>
    /// <param name="title">Optional title.</param>
    /// <param name="duration">Display duration (default: 3s).</param>
    void ShowSuccess(string message, string? title = null, TimeSpan? duration = null);

    /// <summary>
    /// Shows a warning notification.
    /// </summary>
    void ShowWarning(string message, string? title = null, TimeSpan? duration = null);

    /// <summary>
    /// Shows an error notification.
    /// </summary>
    void ShowError(string message, string? title = null, TimeSpan? duration = null);

    /// <summary>
    /// Shows an informational notification.
    /// </summary>
    void ShowInfo(string message, string? title = null, TimeSpan? duration = null);

    /// <summary>
    /// Shows a custom notification.
    /// </summary>
    void Show(ToastNotification notification);

    /// <summary>
    /// Dismisses all active notifications.
    /// </summary>
    void DismissAll();
}

/// <summary>
/// Represents a toast notification.
/// </summary>
public class ToastNotification
{
    /// <summary>
    /// Unique identifier for this notification.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Notification message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Optional title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Notification type (determines styling).
    /// </summary>
    public NotificationType Type { get; init; } = NotificationType.Info;

    /// <summary>
    /// How long to display (null = until dismissed).
    /// </summary>
    public TimeSpan? Duration { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Icon to display (emoji or text).
    /// </summary>
    public string Icon => Type switch
    {
        NotificationType.Success => "âœ“",
        NotificationType.Warning => "âš ",
        NotificationType.Error => "âœ•",
        NotificationType.Info => "â„¹",
        _ => "â€¢"
    };

    /// <summary>
    /// When the notification was created.
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.Now;

    /// <summary>
    /// Whether the notification can be dismissed by clicking.
    /// </summary>
    public bool IsDismissible { get; init; } = true;

    /// <summary>
    /// Optional action when clicked.
    /// </summary>
    public Action? OnClick { get; init; }
}

/// <summary>
/// Types of notifications.
/// </summary>
public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}
