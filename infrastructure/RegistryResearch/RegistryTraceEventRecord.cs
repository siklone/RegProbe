using System;
using System.Collections.Generic;

namespace RegProbe.Infrastructure.RegistryResearch;

internal sealed record RegistryTraceEventRecord
{
    public string? ProviderName { get; init; }

    public string? TaskName { get; init; }

    public string? EventName { get; init; }

    public string? OpcodeName { get; init; }

    public string? ProcessName { get; init; }

    public int? ProcessId { get; init; }

    public DateTimeOffset? TimestampUtc { get; init; }

    public string? FormattedMessage { get; init; }

    public IReadOnlyDictionary<string, string?> Payloads { get; init; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}
