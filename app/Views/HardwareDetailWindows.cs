using OpenTraceProject.App.ViewModels.Hardware;

namespace OpenTraceProject.App.Views;

public sealed class OsDetailWindow : HardwareDetailWindow
{
    public OsDetailWindow()
    {
        DataContext = new OsDetailVM();
    }
}

public sealed class CpuDetailWindow : HardwareDetailWindow
{
    public CpuDetailWindow()
    {
        DataContext = new CpuDetailVM();
    }
}

public sealed class GpuDetailWindow : HardwareDetailWindow
{
    public GpuDetailWindow()
    {
        DataContext = new GpuDetailVM();
    }
}

public sealed class MemoryDetailWindow : HardwareDetailWindow
{
    public MemoryDetailWindow()
    {
        DataContext = new MemoryDetailVM();
    }
}

public sealed class StorageDetailWindow : HardwareDetailWindow
{
    public StorageDetailWindow()
    {
        DataContext = new StorageDetailVM();
    }
}

public sealed class NetworkDetailWindow : HardwareDetailWindow
{
    public NetworkDetailWindow()
    {
        DataContext = new NetworkDetailVM();
    }
}

public sealed class UsbDetailWindow : HardwareDetailWindow
{
    public UsbDetailWindow()
    {
        DataContext = new UsbDetailVM();
    }
}

public sealed class AudioDetailWindow : HardwareDetailWindow
{
    public AudioDetailWindow()
    {
        DataContext = new AudioDetailVM();
    }
}

public sealed class MotherboardDetailWindow : HardwareDetailWindow
{
    public MotherboardDetailWindow()
    {
        DataContext = new MotherboardDetailVM();
    }
}

public sealed class DisplaysDetailWindow : HardwareDetailWindow
{
    public DisplaysDetailWindow()
    {
        DataContext = new DisplaysDetailVM();
    }
}
