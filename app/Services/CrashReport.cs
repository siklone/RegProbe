using System;
using System.Collections.Generic;

namespace RegProbe.App.Services;

/// <summary>
/// Crash report data model.
/// </summary>
public class CrashReport
{
    public string Id { get; init; } = "";
    public DateTime Timestamp { get; init; }
    public string Source { get; init; } = "";
    public bool IsTerminating { get; init; }
    public string ExceptionType { get; init; } = "";
    public string Message { get; init; } = "";
    public string StackTrace { get; init; } = "";
    public string? InnerException { get; init; }
    public string AppVersion { get; init; } = "";
    public string OsVersion { get; init; } = "";
    public string MachineName { get; init; } = "";
    public string UserName { get; init; } = "";
    public int ProcessorCount { get; init; }
    public long WorkingSet { get; init; }
    public Dictionary<string, string> AdditionalData { get; init; } = new();
}
