using System.Diagnostics;
using System.Windows;

namespace RegProbe.App.Services;

internal static class SingleInstanceWindowActivator
{
    public static void BringToForeground()
    {
        var mainWindow = Application.Current?.MainWindow;
        if (mainWindow == null) return;

        try
        {
            if (mainWindow.WindowState == WindowState.Minimized)
            {
                mainWindow.WindowState = WindowState.Normal;
            }

            mainWindow.Activate();

            // Workaround: briefly set Topmost to force focus.
            mainWindow.Topmost = true;
            mainWindow.Topmost = false;

            mainWindow.Focus();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SingleInstance] BringToForeground failed: {ex.Message}");
        }
    }
}
