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

        Icon = "\uEC7A"; // MDL2: DeveloperTools
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
                void AddMetricText(string label, string? value, string unit = "")
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return;
                    }

                    var trimmed = value.Trim();
                    if (string.Equals(trimmed, "N/A", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    SecondaryMetrics.Add(new MetricItem(label, trimmed, unit));
                }

                void AddMetricNumber(string label, int? value, string unit = "")
                {
                    if (!value.HasValue || value.Value <= 0)
                    {
                        return;
                    }

                    SecondaryMetrics.Add(new MetricItem(label, value.Value.ToString(), unit));
                }

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

                var manufacturer = !string.IsNullOrWhiteSpace(specs.Manufacturer)
                    ? specs.Manufacturer
                    : _identity.Manufacturer;
                if (string.IsNullOrWhiteSpace(manufacturer))
                {
                    manufacturer = "Unknown";
                }

                AddMetricText("Vendor", manufacturer);
                AddMetricText("Chipset", specs.Chipset);
                AddMetricText("Socket", specs.Socket);
                AddMetricText("Form", specs.FormFactor);
                AddMetricText("Memory", specs.MemoryType);
                AddMetricNumber("Max RAM", specs.MaxMemoryGb, "GB");
                AddMetricNumber("Max Speed", specs.MaxMemorySpeedMhz, "MHz");
                AddMetricText("PCIe", specs.PcieSlots);
                AddMetricNumber("M.2", specs.M2Slots, "slots");
                AddMetricNumber("SATA", specs.SataPorts, "ports");
                AddMetricText("USB", specs.UsbPorts);
                AddMetricText("LAN", specs.NetworkChip);
                AddMetricText("WiFi", specs.WifiChip);

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
