using System;

namespace WindowsOptimizer.Infrastructure.Elevation;

public class ElevatedHostException : Exception
{
    public ElevatedHostException(string message)
        : base(message)
    {
    }

    public ElevatedHostException(string message, Exception innerException)
        : base(message, innerException)
    {
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
