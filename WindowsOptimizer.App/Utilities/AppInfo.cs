using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WindowsOptimizer.App.Utilities;

public static class AppInfo
{
    public const string ProductName = "Windows Optimizer";
    public const string SuiteName = "Windows Optimizer Suite";
    public const string RepositoryUrl = "https://github.com/siklone/WPF-Windows-optimizer-with-safe-reversible-tweaks";

    public static int CurrentYear => DateTime.Now.Year;

    public static string Version => GetVersionString();

    public static string VersionLabel
    {
        get
        {
            var version = Version;
            return version.StartsWith("0.", StringComparison.Ordinal) ? $"v{version} (Preview)" : $"v{version}";
        }
    }

    public static string BuildConfiguration =>
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    public static string FrameworkLabel => GetFrameworkLabel();

    public static string ArchitectureLabel => RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.X64 => "x64",
        Architecture.X86 => "x86",
        Architecture.Arm64 => "ARM64",
        Architecture.Arm => "ARM",
        _ => RuntimeInformation.ProcessArchitecture.ToString()
    };

    public static string CopyrightLabel => $"© {CurrentYear} {ProductName}";

    private static string GetVersionString()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var plusIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
            return plusIndex > 0 ? informationalVersion[..plusIndex] : informationalVersion;
        }

        var version = assembly.GetName().Version;
        if (version is null)
        {
            return "0.0.0";
        }

        return version.Revision > 0
            ? $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}"
            : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static string GetFrameworkLabel()
    {
        // Typically: ".NET 8.0.12" -> ".NET 8.0"
        var framework = RuntimeInformation.FrameworkDescription;
        var parts = framework.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && string.Equals(parts[0], ".NET", StringComparison.Ordinal))
        {
            var versionPart = parts[1];
            var segments = versionPart.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2)
            {
                versionPart = $"{segments[0]}.{segments[1]}";
            }

            return $".NET {versionPart}";
        }

        return framework;
    }
}

