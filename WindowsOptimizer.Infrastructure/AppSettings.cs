namespace WindowsOptimizer.Infrastructure;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public bool DemoTweakAlphaEnabled { get; set; }
    public bool DemoTweakBetaEnabled { get; set; }
    public string? DiscordWebhookUrl { get; set; }
    public bool DiscordNotificationsEnabled { get; set; }
    public bool DiscordAutoPatchEnabled { get; set; }
    public string Theme { get; set; } = "Dark";
}
