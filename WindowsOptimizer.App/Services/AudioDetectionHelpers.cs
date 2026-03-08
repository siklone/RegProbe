using System;

namespace WindowsOptimizer.App.Services;

internal static class AudioDetectionHelpers
{
    public static bool IsLikelyVirtualDevice(string? name, string? manufacturer = null, string? deviceId = null)
    {
        var value = $"{name} {manufacturer} {deviceId}".Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("virtual audio", StringComparison.Ordinal) ||
               value.Contains("wave extensible", StringComparison.Ordinal) ||
               value.Contains("streaming", StringComparison.Ordinal) ||
               value.Contains("remote audio", StringComparison.Ordinal) ||
               value.Contains("vb-audio", StringComparison.Ordinal) ||
               value.Contains("virtual cable", StringComparison.Ordinal) ||
               value.Contains("voicemod", StringComparison.Ordinal) ||
               value.Contains(@"root\unnamed_device", StringComparison.Ordinal);
    }

    public static int ScoreDevice(string? name, string? manufacturer, string? status, string? deviceId)
    {
        var score = 0;

        if (string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
        {
            score += 20;
        }

        if (!string.IsNullOrWhiteSpace(deviceId) &&
            deviceId.StartsWith("HDAUDIO\\", StringComparison.OrdinalIgnoreCase))
        {
            score += 25;
        }

        if (!string.IsNullOrWhiteSpace(manufacturer) &&
            !manufacturer.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
        {
            score += 10;
        }

        if (!IsLikelyVirtualDevice(name, manufacturer, deviceId))
        {
            score += 50;
        }
        else
        {
            score -= 60;
        }

        if (!string.IsNullOrWhiteSpace(name) &&
            name.Contains("High Definition Audio Device", StringComparison.OrdinalIgnoreCase))
        {
            score -= 10;
        }

        return score;
    }
}
