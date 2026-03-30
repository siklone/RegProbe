using System;
using RegProbe.App.ViewModels;

namespace RegProbe.App.Services;

public sealed class MainWindowFactory
{
    public MainWindow CreateMainWindow()
    {
        return new MainWindow(CreateMainViewModel());
    }

    public MainWindow CreateRecoveryWindow()
    {
        return new MainWindow(CreateMainViewModel())
        {
            Opacity = 1
        };
    }

    private static MainViewModel CreateMainViewModel()
    {
        return new MainViewModel();
    }
}
