using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Infrastructure.Discord;

public sealed class DiscordWebhookClient
{
    private static readonly HttpClient SharedClient = new();
    private readonly HttpClient _httpClient;

    public DiscordWebhookClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? SharedClient;
    }

    public async Task<DiscordWebhookResult> SendMessageAsync(string webhookUrl, string content, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            throw new ArgumentException("Webhook URL is required.", nameof(webhookUrl));
        }

        var payload = new
        {
            content = content ?? string.Empty
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        return new DiscordWebhookResult(response.IsSuccessStatusCode, (int)response.StatusCode, responseBody);
    }
}
