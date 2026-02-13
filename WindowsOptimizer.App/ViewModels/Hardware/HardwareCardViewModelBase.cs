using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace WindowsOptimizer.App.ViewModels.Hardware;

/// <summary>
/// Base class for hardware card view models.
/// Provides common properties for displaying hardware metrics.
/// </summary>
public abstract class HardwareCardViewModelBase : INotifyPropertyChanged, IDisposable
{
    private string _icon = "\uE9D9"; // MDL2: BarChart
    private string _title = "";
    private string _subtitle = "";
    private Brush _iconBackground = Brushes.Gray;
    private Brush _statusColor = Brushes.LimeGreen;
    private string _primaryValue = "--";
    private string _primaryUnit = "";
    private Brush _primaryValueColor = Brushes.White;
    private string _liveSummary = "";
    private bool _hasSpecs;
    private object? _chartContent;
    private bool _isLoading = true;

    /// <summary>
    /// Icon emoji or character for the card header.
    /// </summary>
    public string Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    /// <summary>
    /// Main title (e.g., "CPU", "GPU", "RAM").
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// Subtitle (e.g., model name).
    /// </summary>
    public string Subtitle
    {
        get => _subtitle;
        set => SetProperty(ref _subtitle, value);
    }

    /// <summary>
    /// Background color for the icon container.
    /// </summary>
    public Brush IconBackground
    {
        get => _iconBackground;
        set => SetProperty(ref _iconBackground, value);
    }

    /// <summary>
    /// Status LED color (green = good, yellow = warning, red = critical).
    /// </summary>
    public Brush StatusColor
    {
        get => _statusColor;
        set => SetProperty(ref _statusColor, value);
    }

    /// <summary>
    /// Primary metric value (large display).
    /// </summary>
    public string PrimaryValue
    {
        get => _primaryValue;
        set => SetProperty(ref _primaryValue, value);
    }

    /// <summary>
    /// Primary metric unit (e.g., "%", "°C", "GB").
    /// </summary>
    public string PrimaryUnit
    {
        get => _primaryUnit;
        set => SetProperty(ref _primaryUnit, value);
    }

    /// <summary>
    /// Color for the primary value (based on threshold).
    /// </summary>
    public Brush PrimaryValueColor
    {
        get => _primaryValueColor;
        set => SetProperty(ref _primaryValueColor, value);
    }

    /// <summary>
    /// Live summary text for real-time metrics (optional).
    /// </summary>
    public string LiveSummary
    {
        get => _liveSummary;
        set => SetProperty(ref _liveSummary, value);
    }

    /// <summary>
    /// Whether detailed specs are available.
    /// </summary>
    public bool HasSpecs
    {
        get => _hasSpecs;
        set => SetProperty(ref _hasSpecs, value);
    }

    /// <summary>
    /// Chart or graph content for the center area.
    /// </summary>
    public object? ChartContent
    {
        get => _chartContent;
        set => SetProperty(ref _chartContent, value);
    }

    /// <summary>
    /// Whether the card is still loading initial data.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// Secondary metrics displayed in a grid below the main value.
    /// </summary>
    public ObservableCollection<MetricItem> SecondaryMetrics { get; } = new();

    /// <summary>
    /// Updates or adds a secondary metric.
    /// </summary>
    protected void UpdateSecondaryMetric(string label, string value, string unit)
    {
        for (int i = 0; i < SecondaryMetrics.Count; i++)
        {
            if (SecondaryMetrics[i].Label == label)
            {
                SecondaryMetrics[i] = new MetricItem(label, value, unit);
                return;
            }
        }
        SecondaryMetrics.Add(new MetricItem(label, value, unit));
    }

    /// <summary>
    /// Gets a color based on a percentage value (0-100).
    /// </summary>
    protected static Brush GetPercentageColor(double value) => value switch
    {
        >= 90 => Brushes.Red,
        >= 70 => Brushes.OrangeRed,
        >= 50 => Brushes.Orange,
        _ => Brushes.LimeGreen
    };

    /// <summary>
    /// Gets a color based on temperature (Celsius).
    /// </summary>
    protected static Brush GetTemperatureColor(double temp) => temp switch
    {
        >= 90 => Brushes.Red,
        >= 80 => Brushes.OrangeRed,
        >= 70 => Brushes.Orange,
        >= 60 => Brushes.Yellow,
        _ => Brushes.LimeGreen
    };

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region IDisposable

    private bool _disposed;

    public virtual void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion
}

/// <summary>
/// Represents a single metric in the secondary metrics grid.
/// </summary>
/// <param name="Label">Metric label (e.g., "Temp", "Clock").</param>
/// <param name="Value">Metric value.</param>
/// <param name="Unit">Metric unit (e.g., "°C", "MHz").</param>
public record MetricItem(string Label, string Value, string Unit);
