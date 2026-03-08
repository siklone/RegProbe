using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WindowsOptimizer.Infrastructure.Discord;

public sealed record DiscordEmbed(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("color")] int? Color,
    [property: JsonPropertyName("fields")] IReadOnlyList<DiscordEmbedField>? Fields,
    [property: JsonPropertyName("footer")] DiscordEmbedFooter? Footer,
    [property: JsonPropertyName("timestamp")] DateTimeOffset? Timestamp);

public sealed record DiscordEmbedField(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("inline")] bool Inline = false);

public sealed record DiscordEmbedFooter(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("icon_url")] string? IconUrl = null);

public sealed record DiscordWebhookResult(
    bool Success,
    int StatusCode,
    string ResponseBody);
