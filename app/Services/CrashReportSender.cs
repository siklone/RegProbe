using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RegProbe.App.Services;

internal static class CrashReportSender
{
    public static async Task SendAsync(CrashReport report, string remoteEndpoint)
    {
        if (string.IsNullOrEmpty(remoteEndpoint)) return;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var json = JsonSerializer.Serialize(report);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await client.PostAsync(remoteEndpoint, content);
            Debug.WriteLine("Crash report sent to remote");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to send crash report: {ex.Message}");
        }
    }
}
