using System;

namespace RegProbe.Infrastructure.Elevation;

public class ElevatedHostException : Exception
{
    public int? ErrorHResult { get; }

    public ElevatedHostException(string message)
        : base(message)
    {
    }

    public ElevatedHostException(string message, int? errorHResult)
        : base(message)
    {
        ErrorHResult = errorHResult;
    }

    public ElevatedHostException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorHResult = innerException.HResult;
    }

    public ElevatedHostException(string message, Exception innerException, int? errorHResult)
        : base(message, innerException)
    {
        ErrorHResult = errorHResult;
    }
}

public sealed class ElevatedHostLaunchException : ElevatedHostException
{
    public ElevatedHostLaunchException(string message)
        : base(message)
    {
    }

    public ElevatedHostLaunchException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
