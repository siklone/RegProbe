namespace OpenTraceProject.App.ViewModels;

public sealed class NavigationItem
{
    public NavigationItem(string id, string title, ViewModelBase viewModel)
    {
        Id = id;
        Title = title;
        ViewModel = viewModel;
    }

    public string Id { get; }
    public string Title { get; }
    public ViewModelBase ViewModel { get; }
}
