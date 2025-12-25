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
            Debug.WriteLine("MonitorView: Constructor started");
            InitializeComponent();
            Debug.WriteLine("MonitorView: InitializeComponent completed successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CRITICAL: MonitorView constructor failed: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            Debug.WriteLine($"Inner exception: {ex.InnerException?.Message}");
            throw;
        }
    }
}
