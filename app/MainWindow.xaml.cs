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

    public MainWindowHostController HostController => _hostController;

    public MainWindow(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _hostController = new MainWindowHostController(this);
        InitializeComponent();
        DataContext = viewModel;
        _hostController.Initialize(NotificationHost);
    }
}
