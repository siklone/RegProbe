using System;
using System.ComponentModel;
using System.Windows;
using RegProbe.App.Services;
using RegProbe.App.ViewModels;

namespace RegProbe.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainWindowHostController _hostController = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        Loaded += MainWindow_Loaded;
        _hostController.Initialize(this, NotificationHost);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _hostController.ClampToWorkArea(this);
    }

    #region Custom Title Bar Handlers
    
    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _hostController.HandleTitleBarMouseLeftButtonDown(this, e);
    }
    
    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        _hostController.ToggleMaximizeRestore(this);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        _hostController.Minimize(this);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _hostController.Close(this);
    }
    
    #endregion

    protected override void OnClosing(CancelEventArgs e)
    {
        _hostController.DisposeDataContext(DataContext);
        base.OnClosing(e);
    }
}
