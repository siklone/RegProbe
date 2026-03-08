using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsOptimizer.App.Services;

public class BusyService : IBusyService
{
    private int _busyCount;
    private bool _isBusy;
    private string _message = string.Empty;
    private readonly object _lock = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy != value)
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }
    }

    public string Message
    {
        get => _message;
        private set
        {
            if (_message != value)
            {
                _message = value;
                OnPropertyChanged();
            }
        }
    }

    public IDisposable Busy(string? message = null)
    {
        lock (_lock)
        {
            _busyCount++;
            IsBusy = true;
            if (!string.IsNullOrEmpty(message))
            {
                Message = message;
            }
        }

        return new BusyToken(this);
    }

    private void Release()
    {
        lock (_lock)
        {
            _busyCount--;
            if (_busyCount <= 0)
            {
                _busyCount = 0;
                IsBusy = false;
                Message = string.Empty;
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private class BusyToken : IDisposable
    {
        private readonly BusyService _service;
        private bool _disposed;

        public BusyToken(BusyService service)
        {
            _service = service;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _service.Release();
                _disposed = true;
            }
        }
    }
}
