using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WindowsOptimizer.App.Utilities;

namespace WindowsOptimizer.App.ViewModels;

public interface IAsyncCommand : ICommand
{
    Task ExecuteAsync();
    bool CanExecute();
}

public class AsyncRelayCommand : IAsyncCommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private readonly Action<Exception>? _onException;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null, Action<Exception>? onException = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _onException = onException;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke() ?? true);
    }

    public async void Execute(object? parameter)
    {
        await ExecuteAsync();
    }

    public async Task ExecuteAsync()
    {
        if (!CanExecute(null))
            return;

        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();
            await _execute();
        }
        catch (Exception ex)
        {
            _onException?.Invoke(ex);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }

    public bool CanExecute()
    {
        return CanExecute(null);
    }
}

public class AsyncRelayCommand<T> : IAsyncCommand
{
    private readonly Func<T?, Task> _execute;
    private readonly Func<T?, bool>? _canExecute;
    private readonly Action<Exception>? _onException;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null, Action<Exception>? onException = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _onException = onException;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke((T?)parameter) ?? true);
    }

    public async void Execute(object? parameter)
    {
        await ExecuteAsync((T?)parameter);
    }

    public async Task ExecuteAsync(T? parameter)
    {
        if (!CanExecute(parameter))
            return;

        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();
            await _execute(parameter);
        }
        catch (Exception ex)
        {
            _onException?.Invoke(ex);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public async Task ExecuteAsync()
    {
        await ExecuteAsync(default);
    }

    public bool CanExecute()
    {
        return CanExecute(default);
    }

    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}
