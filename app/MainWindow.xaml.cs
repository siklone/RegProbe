using System;
using System.Windows;
using RegProbe.App.Services;
using RegProbe.App.ViewModels;

namespace RegProbe.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainWindowHostController _hostController;
    private readonly MainWindowLifecycleCoordinator _lifecycleCoordinator;

    public MainWindowHostController HostController => _hostController;

    public MainWindow(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _hostController = new MainWindowHostController(this);
        _lifecycleCoordinator = new MainWindowLifecycleCoordinator(this);
        InitializeComponent();
        DataContext = viewModel;
        _lifecycleCoordinator.Initialize(NotificationHost);
    }
}
