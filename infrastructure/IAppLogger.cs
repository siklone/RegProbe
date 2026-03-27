using System;

namespace RegProbe.Infrastructure;

public interface IAppLogger
{
    void Log(LogLevel level, string message, Exception? exception = null);
}
