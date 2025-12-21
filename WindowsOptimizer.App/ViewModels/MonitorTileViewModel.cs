using System.Globalization;

namespace WindowsOptimizer.App.ViewModels;

public sealed class MonitorTileViewModel : ViewModelBase
{
    private string _valueText = "--";
    private string _unitText = string.Empty;
    private string _subtitle = "Waiting for data";
    private string _statusText = "Idle";
    private bool _isWarning;

    public MonitorTileViewModel(string key, string title, string accent)
    {
        Key = key;
        Title = title;
        Accent = accent;
    }

    public string Key { get; }

    public string Title { get; }

    public string Accent { get; }

    public string ValueText
    {
        get => _valueText;
        private set => SetProperty(ref _valueText, value);
    }

    public string UnitText
    {
        get => _unitText;
        private set => SetProperty(ref _unitText, value);
    }

    public string Subtitle
    {
        get => _subtitle;
        private set => SetProperty(ref _subtitle, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsWarning
    {
        get => _isWarning;
        private set => SetProperty(ref _isWarning, value);
    }

    public void Update(double? value, string unit, string subtitle, string status, bool isWarning)
    {
        ValueText = value.HasValue
            ? value.Value.ToString("0.#", CultureInfo.InvariantCulture)
            : "--";
        UnitText = unit;
        Subtitle = subtitle;
        StatusText = status;
        IsWarning = isWarning;
    }
}
