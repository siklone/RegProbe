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

        var redacted = ReplaceSensitiveValue(
            text,
            "(--pipe\\s+)(?:\"[^\"]*\"|'[^']*'|\\S+)");
        redacted = ReplaceSensitiveValue(
            redacted,
            "(--session-token\\s+)(?:\"[^\"]*\"|'[^']*'|\\S+)");
        redacted = ReplaceSensitiveValue(
            redacted,
            "(\\bpipeName\\b\\s*=\\s*)(?:\"[^\"]*\"|'[^']*'|\\S+)");
        redacted = ReplaceSensitiveValue(
            redacted,
            "(\\bsessionToken\\b\\s*=\\s*)(?:\"[^\"]*\"|'[^']*'|\\S+)");
        redacted = ReplaceSensitiveValue(
            redacted,
            "(\\btoken\\b\\s*=\\s*)(?:\"[^\"]*\"|'[^']*'|\\S+)");
        return redacted;
    }

    private static string ReplaceSensitiveValue(string text, string pattern)
    {
        return Regex.Replace(
            text,
            pattern,
            static match =>
            {
                var prefix = match.Groups[1].Value;
                var suffix = match.Value[prefix.Length..];
                return prefix + RedactArgumentValuePreservingQuotes(suffix);
            },
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string RedactArgumentValuePreservingQuotes(string value)
    {
        if (value.Length >= 2)
        {
            if (value[0] == '"' && value[^1] == '"')
            {
                return "\"<redacted>\"";
            }

            if (value[0] == '\'' && value[^1] == '\'')
            {
                return "'<redacted>'";
            }
        }

        return "<redacted>";
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeClientProcessId(SafePipeHandle pipe, out uint clientProcessId);
}
