using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WindowsOptimizer.Infrastructure.Hardware;
using WindowsOptimizer.Infrastructure.Threading;

namespace WindowsOptimizer.App.ViewModels.Hardware;

/// <summary>
/// ViewModel for the GPU hardware card.
/// </summary>
public class GpuCardViewModel : HardwareCardViewModelBase
{
    private readonly MetricDataBus? _bus;
    private GpuIdentity? _gpuIdentity;

    public GpuCardViewModel(MetricDataBus? bus = null)
    {
        _bus = bus;

        Icon = "🎮";
        Title = "GPU";
        IconBackground = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // Green
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
            _gpuIdentity = await Task.Run(() => HardwareIdentifier.GetGpuId());

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Subtitle = _gpuIdentity.WmiName ?? _gpuIdentity.DriverDesc ?? "Unknown GPU";

                // Add static metrics
                if (_gpuIdentity.AdapterRamGB > 0)
                {
                    SecondaryMetrics.Add(new MetricItem("VRAM", $"{_gpuIdentity.AdapterRamGB:F1}", "GB"));
                }
                SecondaryMetrics.Add(new MetricItem("Vendor", _gpuIdentity.VendorName ?? "Unknown", ""));

                IsLoading = false;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GpuCardViewModel] Failed to load specs: {ex.Message}");
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Subtitle = "GPU Info Unavailable";
                IsLoading = false;
            });
        }
    }

    private void OnMetricsUpdated(object? sender, MetricBatchEventArgs e)
    {
        if (e.TryGetValue<double>("gpu.usage", out var usage))
        {
            PrimaryValue = $"{usage:F0}";
            PrimaryValueColor = GetPercentageColor(usage);
        }

        if (e.TryGetValue<double>("gpu.temp", out var temp))
        {
            UpdateSecondaryMetric("Temp", $"{temp:F0}", "°C");
            StatusColor = GetTemperatureColor(temp);
        }

        if (e.TryGetValue<double>("gpu.clock", out var clock))
        {
            UpdateSecondaryMetric("Clock", $"{clock:F0}", "MHz");
        }

        if (e.TryGetValue<double>("gpu.memory.used", out var memUsed))
        {
            UpdateSecondaryMetric("Mem", $"{memUsed / 1024:F1}", "GB");
        }

        if (e.TryGetValue<double>("gpu.power", out var power))
        {
            UpdateSecondaryMetric("Power", $"{power:F0}", "W");
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
