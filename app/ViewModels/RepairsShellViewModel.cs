namespace RegProbe.App.ViewModels;

public sealed class RepairsShellViewModel : ViewModelBase
{
    public RepairsShellViewModel(TweaksViewModel workspace)
    {
        Workspace = workspace;
    }

    public TweaksViewModel Workspace { get; }
}
