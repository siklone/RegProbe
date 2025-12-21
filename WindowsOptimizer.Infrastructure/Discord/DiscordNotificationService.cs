using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;

namespace WindowsOptimizer.Infrastructure.Discord;

public sealed class DiscordNotificationService : IDisposable
{
    private readonly DiscordWebhookClient _client;
    private readonly ISettingsStore _settingsStore;
    private bool _disposed;

    public DiscordNotificationService(
        DiscordWebhookClient client,
        ISettingsStore settingsStore)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
    }

    public async Task<bool> SendTweakExecutionLogAsync(
        string tweakName,
        TweakStatus status,
        string message,
        CancellationToken ct)
    {
        var settings = await _settingsStore.LoadAsync(ct);
        if (!settings.DiscordNotificationsEnabled ||
            string.IsNullOrWhiteSpace(settings.DiscordWebhookUrl))
        {
            return false;
        }

        var color = status switch
        {
            TweakStatus.Applied => 0xA3BE8C,     // Nord14 green (success)
            TweakStatus.Failed => 0xBF616A,      // Nord11 red (error)
            TweakStatus.Verified => 0x88C0D0,    // Nord8 cyan (verified)
            TweakStatus.RolledBack => 0xD08770,  // Nord12 orange (rollback)
            TweakStatus.Detected => 0x81A1C1,    // Nord9 blue (detected)
            _ => 0x5E81AC                        // Nord10 dark blue (default)
        };

        var fields = new List<DiscordEmbedField>
        {
            new("Status", status.ToString(), true),
            new("Time", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"), true)
        };

        var embed = new DiscordEmbed(
            Title: $"Tweak: {tweakName}",
            Description: message,
            Color: color,
            Fields: fields.AsReadOnly(),
            Footer: new DiscordEmbedFooter("Windows Optimizer Suite"),
            Timestamp: DateTimeOffset.UtcNow);

        try
        {
            var result = await _client.SendEmbedAsync(
                settings.DiscordWebhookUrl,
                embed,
                ct);

            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SendBulkExecutionSummaryAsync(
        int totalTweaks,
        int successCount,
        int failureCount,
        TimeSpan duration,
        CancellationToken ct)
    {
        var settings = await _settingsStore.LoadAsync(ct);
        if (!settings.DiscordNotificationsEnabled ||
            string.IsNullOrWhiteSpace(settings.DiscordWebhookUrl))
        {
            return false;
        }

        var color = failureCount == 0 ? 0xA3BE8C : (successCount > 0 ? 0xEBCB8B : 0xBF616A);

        var fields = new List<DiscordEmbedField>
        {
            new("Total Tweaks", totalTweaks.ToString(), true),
            new("Successful", successCount.ToString(), true),
            new("Failed", failureCount.ToString(), true),
            new("Duration", $"{duration.TotalSeconds:F1}s", true)
        };

        var embed = new DiscordEmbed(
            Title: "Bulk Tweak Execution Summary",
            Description: $"Executed {totalTweaks} tweaks with {successCount} successes and {failureCount} failures.",
            Color: color,
            Fields: fields.AsReadOnly(),
            Footer: new DiscordEmbedFooter("Windows Optimizer Suite"),
            Timestamp: DateTimeOffset.UtcNow);

        try
        {
            var result = await _client.SendEmbedAsync(
                settings.DiscordWebhookUrl,
                embed,
                ct);

            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UploadPatchFileAsync(
        string filePath,
        string description,
        CancellationToken ct)
    {
        var settings = await _settingsStore.LoadAsync(ct);
        if (!settings.DiscordAutoPatchEnabled ||
            string.IsNullOrWhiteSpace(settings.DiscordWebhookUrl))
        {
            return false;
        }

        try
        {
            var result = await _client.SendFileAsync(
                settings.DiscordWebhookUrl,
                filePath,
                $"📦 **Patch Upload**: {description}",
                ct);

            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _client?.Dispose();
        _disposed = true;
    }
}
