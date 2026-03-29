using System.Windows.Controls;
using System.Windows;

namespace RegProbe.App.Views;

public partial class WorkspaceShellChrome : UserControl
{
    public static readonly DependencyProperty ShellContentProperty =
        DependencyProperty.Register(
            nameof(ShellContent),
            typeof(object),
            typeof(WorkspaceShellChrome),
            new PropertyMetadata(null));

    public WorkspaceShellChrome()
    {
        InitializeComponent();
    }

    public object? ShellContent
    {
        get => GetValue(ShellContentProperty);
        set => SetValue(ShellContentProperty, value);
    }
}
