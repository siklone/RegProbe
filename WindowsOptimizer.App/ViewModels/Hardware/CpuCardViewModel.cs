using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WindowsOptimizer.Infrastructure.Hardware;
using WindowsOptimizer.Infrastructure.Threading;

namespace WindowsOptimizer.App.ViewModels.Hardware;

/// <summary>
/// ViewModel for the CPU hardware card.
/// </summary>
public class CpuCardViewModel : HardwareCardViewModelBase
{
    private readonly MetricDataBus? _bus;
    private CpuIdentity? _cpuIdentity;
    private readonly HardwareSpecsService _specsService = new();

    public CpuCardViewModel(MetricDataBus? bus = null)
    {
        _bus = bus;

        Icon = "\uE950"; // MDL2: Processor
        Title = "CPU";
        IconBackground = new SolidColorBrush(Color.FromRgb(59, 130, 246)); // Blue
        PrimaryUnit = "%";

        if (_bus != null)
        {
            _bus.MetricsUpdated += OnMetricsUpdated;
        }

        // Load specs in background
        Task.Run(LoadSpecsAsync);
    }

    private async Task LoadSpecsAsync()
    {
        try
        {
            _cpuIdentity = await Task.Run(() => HardwareIdentifier.GetCpuId());
            var specs = await _specsService.GetCpuSpecsAsync(_cpuIdentity, CancellationToken.None);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Subtitle = specs.Brand;
                HasSpecs = specs.IsFromDatabase;

                // Add static metrics
                SecondaryMetrics.Clear();
                SecondaryMetrics.Add(new MetricItem("Cores", (specs.Cores ?? _cpuIdentity.Cores).ToString(), ""));
                SecondaryMetrics.Add(new MetricItem("Threads", (specs.Threads ?? _cpuIdentity.Threads).ToString(), ""));
                var baseClock = specs.BaseClockMhz ?? _cpuIdentity.MaxClockSpeed;
                SecondaryMetrics.Add(new MetricItem("Base", baseClock > 0 ? baseClock.ToString() : "N/A", "MHz"));

                IsLoading = false;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CpuCardViewModel] Failed to load specs: {ex.Message}");
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Subtitle = "CPU Info Unavailable";
                IsLoading = false;
            });
        }
    }

    private void OnMetricsUpdated(object? sender, MetricBatchEventArgs e)
    {
        if (e.TryGetValue<double>("cpu.usage", out var usage))
        {
            PrimaryValue = $"{usage:F0}";
            PrimaryValueColor = GetPercentageColor(usage);
        }

        if (e.TryGetValue<double>("cpu.temp", out var temp))
        {
            UpdateSecondaryMetric("Temp", $"{temp:F0}", "°C");
            StatusColor = GetTemperatureColor(temp);
        }

        if (e.TryGetValue<double>("cpu.clock", out var clock))
        {
            UpdateSecondaryMetric("Clock", $"{clock:F0}", "MHz");
        }

        if (e.TryGetValue<double>("cpu.power", out var power))
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
