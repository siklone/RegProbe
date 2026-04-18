using System.Windows;

namespace RegProbe.App.Services;

internal static class SingleInstanceUserNotifier
{
    public static void ShowInstanceWarning()
    {
        MessageBox.Show(
            "RegProbe is already running but not responding.\n\n" +
            "Please close the existing instance using Task Manager,\n" +
            "or restart your computer if the problem persists.",
            "RegProbe",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}
