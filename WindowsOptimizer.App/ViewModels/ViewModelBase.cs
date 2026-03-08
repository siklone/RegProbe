using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsOptimizer.App.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() ?? true)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        else
        {
            System.Windows.Application.Current?.Dispatcher?.BeginInvoke(() => 
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
