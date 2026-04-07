using System;
using System.Text.RegularExpressions;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace RegProbe.Infrastructure.Elevation;

public static class ElevatedHostSessionSecurity
{
    public static string CreateSessionToken()
    {
        Span<byte> buffer = stackalloc byte[16];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToHexString(buffer);
    }

    public static bool IsSessionTokenAccepted(string expectedToken, string? actualToken)
    {
        return !string.IsNullOrWhiteSpace(expectedToken)
            && string.Equals(expectedToken, actualToken, StringComparison.Ordinal);
    }

    public static string BuildPipeNonceSuffix(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return "session";
        }

        var normalized = sessionToken.Trim();
        return normalized.Length <= 12
            ? normalized
            : normalized[..12];
    }

    public static bool TryGetClientProcessId(NamedPipeServerStream stream, out int clientProcessId)
    {
        clientProcessId = 0;
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        if (stream is null)
        {
            return false;
        }

        if (!GetNamedPipeClientProcessId(stream.SafePipeHandle, out var nativeClientProcessId))
        {
            return false;
        }

        if (nativeClientProcessId > int.MaxValue)
        {
            return false;
        }

        clientProcessId = (int)nativeClientProcessId;
        return true;
    }

    public static bool IsClientProcessAccepted(int expectedParentProcessId, int actualClientProcessId)
    {
        return expectedParentProcessId <= 0 || actualClientProcessId == expectedParentProcessId;
    }

    public static string RedactSensitiveText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var redacted = Regex.Replace(
            text,
            "(--session-token\\s+\")([^\"]+)(\")",
            "$1<redacted>$3",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        redacted = Regex.Replace(
            redacted,
            "(sessionToken=)([^\\s]+)",
            "$1<redacted>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        redacted = Regex.Replace(
            redacted,
            "(token=)([^\\s]+)",
            "$1<redacted>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return redacted;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeClientProcessId(SafePipeHandle pipe, out uint clientProcessId);
}
