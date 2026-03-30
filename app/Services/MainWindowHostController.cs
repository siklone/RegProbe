using System;
using System.Windows;
using System.Windows.Input;
using RegProbe.App.ViewModels;

namespace RegProbe.App.Services;

public sealed class MainWindowHostController
{
    private readonly Window _window;

    public MainWindowHostController(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        MinimizeCommand = new RelayCommand(_ => Minimize(_window));
        ToggleMaximizeRestoreCommand = new RelayCommand(_ => ToggleMaximizeRestore(_window));
        CloseCommand = new RelayCommand(_ => Close(_window));
    }

    public ICommand MinimizeCommand { get; }

    public ICommand ToggleMaximizeRestoreCommand { get; }

    public ICommand CloseCommand { get; }

    public void ToggleMaximizeRestore(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    public void Minimize(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.WindowState = WindowState.Minimized;
    }

    public void Close(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.Close();
    }
}
