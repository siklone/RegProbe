using System;

namespace OpenTraceProject.Infrastructure;

public interface IAppLogger
{
    void Log(LogLevel level, string message, Exception? exception = null);
}
