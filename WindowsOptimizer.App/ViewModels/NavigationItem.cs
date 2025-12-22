namespace WindowsOptimizer.App.ViewModels;

public sealed class NavigationItem
{
    public NavigationItem(string id, string title, string icon, ViewModelBase viewModel)
    {
        Id = id;
        Title = title;
        Icon = icon;
        ViewModel = viewModel;
    }

    public string Id { get; }
    public string Title { get; }
    public string Icon { get; }
    public ViewModelBase ViewModel { get; }
}
