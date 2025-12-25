using System;
using System.Diagnostics;
using System.Windows.Controls;

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
            LogToFile($"CRITICAL: MonitorView constructor failed: {ex.Message}");
            LogToFile($"Stack trace: {ex.StackTrace}");
            LogToFile($"Inner exception: {ex.InnerException?.Message}");
            throw;
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
