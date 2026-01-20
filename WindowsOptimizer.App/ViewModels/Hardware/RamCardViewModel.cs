using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WindowsOptimizer.Infrastructure.Hardware;
using WindowsOptimizer.Infrastructure.Threading;

namespace WindowsOptimizer.App.ViewModels.Hardware;

/// <summary>
/// ViewModel for the RAM hardware card.
/// </summary>
public class RamCardViewModel : HardwareCardViewModelBase
{
    private readonly MetricDataBus? _bus;
    private RamIdentity? _ramIdentity;

    public RamCardViewModel(MetricDataBus? bus = null)
    {
        _bus = bus;

        Icon = "💾";
        Title = "RAM";
        IconBackground = new SolidColorBrush(Color.FromRgb(168, 85, 247)); // Purple
        PrimaryUnit = "%";

        if (_bus != null)
        {
            _bus.MetricsUpdated += OnMetricsUpdated;
        }

        Task.Run(LoadSpecsAsync);
    }

    private async Task LoadSpecsAsync()
    {
        try
        {
            _ramIdentity = await Task.Run(() => HardwareIdentifier.GetRamId());

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Subtitle = _ramIdentity.LookupKey;

                // Add static metrics
                SecondaryMetrics.Add(new MetricItem("Total", $"{_ramIdentity.TotalCapacityGB:F0}", "GB"));
                SecondaryMetrics.Add(new MetricItem("Slots", _ramIdentity.Modules.Count.ToString(), ""));

                if (_ramIdentity.Modules.Count > 0)
                {
                    var firstModule = _ramIdentity.Modules[0];
                    SecondaryMetrics.Add(new MetricItem("Speed", firstModule.SpeedMHz.ToString(), "MHz"));
                }

                IsLoading = false;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RamCardViewModel] Failed to load specs: {ex.Message}");
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Subtitle = "RAM Info Unavailable";
                IsLoading = false;
            });
        }
    }

    private void OnMetricsUpdated(object? sender, MetricBatchEventArgs e)
    {
        if (e.TryGetValue<double>("ram.usage", out var usage))
        {
            PrimaryValue = $"{usage:F0}";
            PrimaryValueColor = GetPercentageColor(usage);
        }

        if (e.TryGetValue<double>("ram.used", out var used))
        {
            UpdateSecondaryMetric("Used", $"{used:F1}", "GB");
        }

        if (e.TryGetValue<double>("ram.available", out var available))
        {
            UpdateSecondaryMetric("Free", $"{available:F1}", "GB");
        }
    }

    public override void Dispose()
    {
        if (_bus != null)
        {
            _bus.MetricsUpdated -= OnMetricsUpdated;
        }
        base.Dispose();
    }
}
