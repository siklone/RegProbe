namespace RegProbe.App.ViewModels;

public sealed class ConfigurationShellViewModel : ViewModelBase
{
    public ConfigurationShellViewModel(TweaksViewModel workspace)
    {
        Workspace = workspace;
    }

    public TweaksViewModel Workspace { get; }
}
