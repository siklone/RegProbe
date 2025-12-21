namespace WindowsOptimizer.Infrastructure.Discord;

public sealed record DiscordWebhookResult(
    bool Success,
    int StatusCode,
    string ResponseBody);
