using System;

namespace WindowsOptimizer.Infrastructure;

/// <summary>
/// Composite logger that writes to multiple log destinations.
/// Combines text-based FileAppLogger with StructuredJsonLogger.
/// </summary>
public sealed class CompositeLogger : IAppLogger
{
    private readonly IAppLogger[] _loggers;

    public CompositeLogger(params IAppLogger[] loggers)
    {
        _loggers = loggers ?? Array.Empty<IAppLogger>();
    }

    /// <summary>
    /// Creates a composite logger with both text and JSON logging.
    /// </summary>
    public static CompositeLogger CreateDefault(AppPaths paths)
    {
        return new CompositeLogger(
            new FileAppLogger(paths),
            new StructuredJsonLogger(paths));
    }

    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        foreach (var logger in _loggers)
        {
            try
            {
                logger.Log(level, message, exception);
            }
            catch
            {
                // Ignore individual logger failures to prevent cascading issues
            }
        }
    }
}
