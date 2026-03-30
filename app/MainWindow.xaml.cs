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

    #region Custom Title Bar Handlers
    
    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _hostController.HandleTitleBarMouseLeftButtonDown(this, e);
    }

    #endregion
}
