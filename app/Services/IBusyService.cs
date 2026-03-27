using System;
using System.ComponentModel;

namespace RegProbe.App.Services;

public interface IBusyService : INotifyPropertyChanged
{
    bool IsBusy { get; }
    string Message { get; }
    IDisposable Busy(string? message = null);
}
