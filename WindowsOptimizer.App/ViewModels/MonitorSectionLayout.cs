namespace WindowsOptimizer.App.ViewModels;

public sealed class MonitorSectionLayout : ViewModelBase
{
    private bool _isVisible = true;

    public MonitorSectionLayout(string key, string title, string description)
    {
        Key = key;
        Title = title;
        Description = description;
    }

    public string Key { get; }

    public string Title { get; }

    public string Description { get; }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }
}
