using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Linq;
using WindowsOptimizer.Infrastructure.Hardware;
using WindowsOptimizer.Infrastructure.Threading;

namespace WindowsOptimizer.App.ViewModels.Hardware;

/// <summary>
/// ViewModel for the RAM hardware card.
/// </summary>
public class RamCardViewModel : HardwareCardViewModelBase
{
    private readonly MetricDataBus? _bus;
    private readonly HardwareSpecsService _specsService = new();
    private RamIdentity? _ramIdentity;
    private double? _usedGb;
    private double? _availableGb;

    public RamCardViewModel(MetricDataBus? bus = null)
    {
        _bus = bus;

        Icon = "\uE964"; // MDL2: Memory
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
            var matchedSpec = await ResolveRamSpecsAsync(_ramIdentity);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Subtitle = matchedSpec?.Model ?? _ramIdentity.LookupKey;
                HasSpecs = matchedSpec?.IsFromDatabase == true;

                // Add static metrics
                SecondaryMetrics.Clear();
                SecondaryMetrics.Add(new MetricItem("Total", $"{_ramIdentity.TotalCapacityGB:F0}", "GB"));
                SecondaryMetrics.Add(new MetricItem("Slots", _ramIdentity.Modules.Count.ToString(), ""));

                var speed = matchedSpec?.SpeedMhz ?? _ramIdentity.Modules.FirstOrDefault()?.SpeedMHz ?? 0;
                if (speed > 0)
                {
                    SecondaryMetrics.Add(new MetricItem("Speed", speed.ToString(), "MHz"));
                }

                var typeLabel = matchedSpec?.Type ?? _ramIdentity.Modules.FirstOrDefault()?.MemoryType;
                if (!string.IsNullOrWhiteSpace(typeLabel))
                {
                    SecondaryMetrics.Add(new MetricItem("Type", typeLabel, ""));
                }

                if (matchedSpec?.CasLatency is { } casLatency)
                {
                    SecondaryMetrics.Add(new MetricItem("CL", casLatency.ToString(), ""));
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

    private async Task<RamSpecs?> ResolveRamSpecsAsync(RamIdentity identity)
    {
        if (identity.Modules.Count == 0)
        {
            return null;
        }

        RamSpecs? fallback = null;
        foreach (var module in identity.Modules)
        {
            var spec = await _specsService.GetRamSpecsAsync(module, CancellationToken.None);
            if (fallback == null)
            {
                fallback = spec;
            }

            if (spec.IsFromDatabase)
            {
                return spec;
            }
        }

        return fallback;
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
            _usedGb = used;
            UpdateSecondaryMetric("Used", $"{used:F1}", "GB");
            UpdateLiveSummary();
        }

        if (e.TryGetValue<double>("ram.available", out var available))
        {
            _availableGb = available;
            UpdateSecondaryMetric("Free", $"{available:F1}", "GB");
            UpdateLiveSummary();
        }
    }

    private void UpdateLiveSummary()
    {
        var parts = new List<string>();
        if (_usedGb.HasValue)
        {
            parts.Add($"{_usedGb.Value:F1} GB used");
        }

        if (_availableGb.HasValue)
        {
            parts.Add($"{_availableGb.Value:F1} GB free");
        }

        LiveSummary = parts.Count > 0 ? string.Join(" | ", parts) : string.Empty;
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
