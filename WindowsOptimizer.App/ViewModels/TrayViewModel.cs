using System;
using System.Windows;
using System.Windows.Input;

namespace WindowsOptimizer.App.ViewModels;

public class TrayViewModel : ViewModelBase, IDisposable
{
    private string _toolTipText = "Windows Optimizer\nReady";

    public ICommand ShowWindowCommand { get; }
    public ICommand ExitApplicationCommand { get; }

    public TrayViewModel()
    {
        ShowWindowCommand = new RelayCommand(_ => ShowMainWindow());
        ExitApplicationCommand = new RelayCommand(_ => Application.Current.Shutdown());
    }

    public string ToolTipText
    {
        get => _toolTipText;
        set => SetProperty(ref _toolTipText, value);
    }

    private void ShowMainWindow()
    {
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow != null)
        {
            if (mainWindow.WindowState == WindowState.Minimized)
                mainWindow.WindowState = WindowState.Normal;

            mainWindow.Show();
            mainWindow.Activate();
        }
    }

    public void Dispose()
    {
    }
}
