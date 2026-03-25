using System;
using System.Threading.Tasks;

namespace OpenTraceProject.App.Utilities;

public static class TaskExtensions
{
    /// <summary>
    /// Safely fires and forgets a task, logging any exceptions.
    /// usage: myAsyncMethod().SafeFireAndForget();
    /// </summary>
    public static async void SafeFireAndForget(this Task task, Action<Exception>? onException = null)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            onException?.Invoke(ex);
            // In a real app, integrate with global logging here
            System.Diagnostics.Debug.WriteLine($"SafeFireAndForget caught: {ex}");
        }
    }
}
