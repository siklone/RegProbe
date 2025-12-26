using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsOptimizer.App.Diagnostics;

namespace WindowsOptimizer.App.Views;

public partial class MonitorView : UserControl
{
    public MonitorView()
    {
        try
        {
            LogToFile("MonitorView: Constructor started");
            InitializeComponent();
            LogToFile("MonitorView: InitializeComponent completed successfully");
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("CRITICAL: MonitorView constructor failed", ex);

            Content = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x2E, 0x34, 0x40)),
                Padding = new System.Windows.Thickness(16),
                Child = new TextBox
                {
                    IsReadOnly = true,
                    TextWrapping = System.Windows.TextWrapping.Wrap,
                    Background = new SolidColorBrush(Color.FromRgb(0x3B, 0x42, 0x52)),
                    Foreground = new SolidColorBrush(Color.FromRgb(0xEC, 0xEF, 0xF4)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x56, 0x6A)),
                    Text =
                        "Monitor view failed to load.\n\n" +
                        $"{ex.GetType().Name}: {ex.Message}\n\n" +
                        "Details were written to %TEMP%\\WindowsOptimizer_Debug.log"
                }
            };
        }
    }

    private static void LogToFile(string message)
    {
        try
        {
            var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WindowsOptimizer_Debug.log");
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            System.IO.File.AppendAllText(logPath, $"[{timestamp}] {message}\n");
        }
        catch
        {
            // Ignore logging errors
        }
    }
}
