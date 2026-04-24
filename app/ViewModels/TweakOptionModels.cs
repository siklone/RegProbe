namespace RegProbe.App.ViewModels;

public sealed class TweakSubOption : ViewModelBase
{
    private bool _isEnabled;
    private string _value = string.Empty;

    public TweakSubOption(string label, TweakSubOptionType type)
    {
        Label = label;
        Type = type;
    }

    public string Label { get; }

    public TweakSubOptionType Type { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}

public sealed class TweakChoiceOption : ViewModelBase
{
    public TweakChoiceOption(string key, string label, string description)
    {
        Key = key;
        Label = label;
        Description = description ?? string.Empty;
    }

    public string Key { get; }

    public string Label { get; }

    public string Description { get; }
}

public enum TweakSubOptionType
{
    Toggle,
    Numeric,
    Dropdown
}
