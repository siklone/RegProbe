using System;
using System.Windows;

namespace WindowsOptimizer.App.Views;

public partial class HardwareDetailWindow : Window
{
    public HardwareDetailWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnClosed(e);
    }
}
