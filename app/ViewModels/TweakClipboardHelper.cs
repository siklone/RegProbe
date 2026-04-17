using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace RegProbe.App.ViewModels;

internal static class TweakClipboardHelper
{
    private const int ClipboardBusy = unchecked((int)0x800401D0);
    private const int ClipboardCantEmpty = unchecked((int)0x800401D1);

    public static bool TrySetText(string text, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            errorMessage = "Nothing to copy.";
            return false;
        }

        for (var attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == true)
                {
                    Clipboard.SetText(text);
                }
                else if (System.Windows.Application.Current?.Dispatcher != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => Clipboard.SetText(text));
                }
                else
                {
                    Clipboard.SetText(text);
                }

                return true;
            }
            catch (COMException ex) when (ex.HResult == ClipboardBusy || ex.HResult == ClipboardCantEmpty)
            {
                Thread.Sleep(30 * (attempt + 1));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        errorMessage = "Clipboard is busy. Try again.";
        return false;
    }
}
