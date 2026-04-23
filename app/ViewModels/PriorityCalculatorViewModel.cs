namespace RegProbe.App.ViewModels;

public sealed class PriorityCalculatorViewModel : ViewModelBase
{
    private int _intervalLength = 2;
    private int _intervalType = 1;
    private int _boost = 3;

    public int IntervalLength
    {
        get => _intervalLength;
        set { if (SetProperty(ref _intervalLength, value)) OnPropertyChanged(nameof(Bitmask)); }
    }

    public int IntervalType
    {
        get => _intervalType;
        set { if (SetProperty(ref _intervalType, value)) OnPropertyChanged(nameof(Bitmask)); }
    }

    public int Boost
    {
        get => _boost;
        set { if (SetProperty(ref _boost, value)) OnPropertyChanged(nameof(Bitmask)); }
    }

    public int Bitmask
    {
        get
        {
            // Windows stores these options as a packed 6-bit mask, so the view model mirrors that encoding.
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
