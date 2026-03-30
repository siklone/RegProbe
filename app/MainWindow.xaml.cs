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

    public MainWindow()
    {
        _hostController = new MainWindowHostController(this);
        InitializeComponent();
        DataContext = new MainViewModel();
        _hostController.Initialize(NotificationHost);
    }
}
