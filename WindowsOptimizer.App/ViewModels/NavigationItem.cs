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

    /// <summary>MDL2 glyph string (preferred) or emoji fallback.</summary>
    public string IconGlyph => Id switch
    {
        "dashboard" => "\uE80F",      // Home
        "tweaks" => "\uE770",         // Configuration / Repair
        "monitor" => "\uE9D9",        // Monitor
        "settings" => "\uE713",       // Settings gear
        "about" => "\uE946",          // Info
        _ => "\uE70F"
    };

    public string Id { get; }
    public string Title { get; }
    public string Icon { get; }
    public ViewModelBase ViewModel { get; }
}
