using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Principal;

namespace RegProbe.App.Services;

internal static class CrashReportFactory
{
    public static CrashReport Create(Exception exception, string source, bool isTerminating)
    {
        return new CrashReport
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = DateTime.UtcNow,
            Source = source,
            IsTerminating = isTerminating,
            ExceptionType = exception.GetType().FullName ?? "Unknown",
            Message = exception.Message,
            StackTrace = exception.StackTrace ?? "",
            InnerException = exception.InnerException?.Message,
            AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
            OsVersion = Environment.OSVersion.ToString(),
            MachineName = Environment.MachineName,
            UserName = Environment.UserName,
            ProcessorCount = Environment.ProcessorCount,
            WorkingSet = Environment.WorkingSet / 1024 / 1024,
            AdditionalData = new Dictionary<string, string>
            {
                ["IsAdmin"] = IsRunningAsAdmin().ToString(),
                ["Culture"] = CultureInfo.CurrentCulture.Name
            }
        };
    }

    private static bool IsRunningAsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
