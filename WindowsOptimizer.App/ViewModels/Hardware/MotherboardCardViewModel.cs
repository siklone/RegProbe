using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WindowsOptimizer.Infrastructure.Hardware;
using WindowsOptimizer.Infrastructure.Threading;

namespace WindowsOptimizer.App.ViewModels.Hardware;

/// <summary>
/// ViewModel for the Motherboard hardware card.
/// </summary>
public sealed class MotherboardCardViewModel : HardwareCardViewModelBase
{
    private readonly MetricDataBus? _bus;
    private readonly HardwareSpecsService _specsService = new();
    private MotherboardIdentity? _identity;

    public MotherboardCardViewModel(MetricDataBus? bus = null)
    {
        _bus = bus;

        Icon = "\uD83D\uDEE0\uFE0F";
        Title = "Motherboard";
        IconBackground = new SolidColorBrush(Color.FromRgb(236, 72, 153)); // Pink
        PrimaryUnit = "";

        Task.Run(LoadSpecsAsync);
    }

    private async Task LoadSpecsAsync()
    {
        try
        {
            _identity = await Task.Run(HardwareIdentifier.GetMotherboardId).ConfigureAwait(false);
            var specs = await _specsService.GetMotherboardSpecsAsync(_identity, CancellationToken.None).ConfigureAwait(false);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var model = !string.IsNullOrWhiteSpace(specs.Model) ? specs.Model : _identity.Product;
                Subtitle = string.IsNullOrWhiteSpace(model) ? "Unknown Motherboard" : model;
                HasSpecs = specs.IsFromDatabase;
                StatusColor = HasSpecs ? Brushes.LimeGreen : Brushes.Orange;

                SecondaryMetrics.Clear();
                if (specs.MemorySlots is { } slots && slots > 0)
                {
                    PrimaryValue = slots.ToString();
                    PrimaryUnit = "slots";
                }
                else if (specs.MaxMemoryGb is { } maxMem && maxMem > 0)
                {
                    PrimaryValue = maxMem.ToString();
                    PrimaryUnit = "GB";
                }

                SecondaryMetrics.Add(new MetricItem("Chipset", !string.IsNullOrWhiteSpace(specs.Chipset) ? specs.Chipset : "N/A", ""));
                SecondaryMetrics.Add(new MetricItem("Socket", !string.IsNullOrWhiteSpace(specs.Socket) ? specs.Socket : "N/A", ""));
                SecondaryMetrics.Add(new MetricItem("Form", !string.IsNullOrWhiteSpace(specs.FormFactor) ? specs.FormFactor : "N/A", ""));
                SecondaryMetrics.Add(new MetricItem("Memory", !string.IsNullOrWhiteSpace(specs.MemoryType) ? specs.MemoryType : "N/A", ""));

                if (specs.MaxMemoryGb is { } maxGb && maxGb > 0)
                {
                    SecondaryMetrics.Add(new MetricItem("Max RAM", maxGb.ToString(), "GB"));
                }

                if (specs.MaxMemorySpeedMhz is { } speed && speed > 0)
                {
                    SecondaryMetrics.Add(new MetricItem("Max Speed", speed.ToString(), "MHz"));
                }

                IsLoading = false;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MotherboardCardViewModel] Failed to load specs: {ex.Message}");
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Subtitle = "Motherboard Info Unavailable";
                IsLoading = false;
            });
        }
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}
