using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Infrastructure.Discord;

public sealed class DiscordWebhookClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public DiscordWebhookClient()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WindowsOptimizerSuite/1.0");
    }

    public async Task<DiscordWebhookResult> SendMessageAsync(
        string webhookUrl,
        string message,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            throw new ArgumentException("Webhook URL is required.", nameof(webhookUrl));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message is required.", nameof(message));
        }

        var payload = new { content = message };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(webhookUrl, content, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            return new DiscordWebhookResult(
                response.IsSuccessStatusCode,
                (int)response.StatusCode,
                responseBody);
        }
        catch (Exception ex)
        {
            return new DiscordWebhookResult(
                false,
                0,
                $"Error: {ex.Message}");
        }
    }

    public async Task<DiscordWebhookResult> SendEmbedAsync(
        string webhookUrl,
        DiscordEmbed embed,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            throw new ArgumentException("Webhook URL is required.", nameof(webhookUrl));
        }

        if (embed is null)
        {
            throw new ArgumentNullException(nameof(embed));
        }

        var payload = new { embeds = new[] { embed } };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(webhookUrl, content, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            return new DiscordWebhookResult(
                response.IsSuccessStatusCode,
                (int)response.StatusCode,
                responseBody);
        }
        catch (Exception ex)
        {
            return new DiscordWebhookResult(
                false,
                0,
                $"Error: {ex.Message}");
        }
    }

    public async Task<DiscordWebhookResult> SendFileAsync(
        string webhookUrl,
        string filePath,
        string? message,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            throw new ArgumentException("Webhook URL is required.", nameof(webhookUrl));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            return new DiscordWebhookResult(
                false,
                0,
                "File not found.");
        }

        using var form = new MultipartFormDataContent();

        if (!string.IsNullOrWhiteSpace(message))
        {
            form.Add(new StringContent(message), "content");
        }

        var fileBytes = await File.ReadAllBytesAsync(filePath, ct);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "file", Path.GetFileName(filePath));

        try
        {
            var response = await _httpClient.PostAsync(webhookUrl, form, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            return new DiscordWebhookResult(
                response.IsSuccessStatusCode,
                (int)response.StatusCode,
                responseBody);
        }
        catch (Exception ex)
        {
            return new DiscordWebhookResult(
                false,
                0,
                $"Error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _httpClient?.Dispose();
        _disposed = true;
    }
}
