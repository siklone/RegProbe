using System;

namespace RegProbe.App.ViewModels;

public sealed class PriorityCalculatorViewModel : ViewModelBase
{
    private int _intervalLength = 2; // 10 = Short
    private int _intervalType = 1;   // 01 = Variable
    private int _boost = 3;         // 11 = Maximum (Optimized for gamers)

    // Bits 0-1
    public int IntervalLength
    {
        get => _intervalLength;
        set { if (SetProperty(ref _intervalLength, value)) OnPropertyChanged(nameof(Bitmask)); }
    }

    // Bits 2-3
    public int IntervalType
    {
        get => _intervalType;
        set { if (SetProperty(ref _intervalType, value)) OnPropertyChanged(nameof(Bitmask)); }
    }

    // Bits 4-5
    public int Boost
    {
        get => _boost;
        set { if (SetProperty(ref _boost, value)) OnPropertyChanged(nameof(Bitmask)); }
    }

    public int Bitmask
    {
        get
        {
            // Calculate mask based on bits: Boost (4-5), Type (2-3), Length (0-1)
            return (Boost << 4) | (IntervalType << 2) | IntervalLength;
        }
        set
        {
            IntervalLength = value & 0x03;
            IntervalType = (value >> 2) & 0x03;
            Boost = (value >> 4) & 0x03;
        }
    }

    public string Description => Bitmask switch
    {
        0x26 => "Standard Windows (Short, Variable, High Boost)",
        0x14 => "Server Optimized (Long, Fixed, No Boost)",
        0x28 => "Gamer Optimized (Short, Fixed, High Boost)",
        _ => "Custom Configuration"
    };
}
