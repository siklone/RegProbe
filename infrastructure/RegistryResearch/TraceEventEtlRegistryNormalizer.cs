using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Diagnostics.Tracing;

namespace RegProbe.Infrastructure.RegistryResearch;

public sealed class TraceEventEtlRegistryNormalizer : IRegistryTraceNormalizer
{
    public string Name => nameof(TraceEventEtlRegistryNormalizer);

    public bool CanNormalize(string inputPath)
        => string.Equals(Path.GetExtension(inputPath), ".etl", StringComparison.OrdinalIgnoreCase);

    public NormalizedRegistryBundle Normalize(RegistryNormalizationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var inputPath = Path.GetFullPath(request.InputPath);
        if (!File.Exists(inputPath))
        {
            return BuildErrorBundle(request, inputPath, "missing-input", $"Input trace was not found: {inputPath}");
        }

        try
        {
            var events = new List<NormalizedRegistryEvent>();
            using var source = new ETWTraceEventSource(inputPath);
            source.Dynamic.All += traceEvent =>
            {
                if (!IsRegistryEvent(traceEvent))
                {
                    return;
                }

                var operation = FirstNonEmpty(traceEvent.EventName, traceEvent.OpcodeName, traceEvent.TaskName) ?? "RegistryEvent";
                var rawPath = FirstNonEmpty(
                    GetPayload(traceEvent, "KeyName"),
                    GetPayload(traceEvent, "Key"),
                    GetPayload(traceEvent, "KeyPath"),
                    GetPayload(traceEvent, "Path"),
                    GetPayload(traceEvent, "BaseName"));
                var valueName = FirstNonEmpty(
                    GetPayload(traceEvent, "ValueName"),
                    GetPayload(traceEvent, "Value"),
                    GetPayload(traceEvent, "Name"));
                var splitValue = string.IsNullOrWhiteSpace(valueName) && operation.Contains("Value", StringComparison.OrdinalIgnoreCase);
                var parts = RegistryPathParser.Parse(rawPath, splitValue);

                events.Add(new NormalizedRegistryEvent
                {
                    RunId = request.RunId,
                    SourceTool = request.SourceTool,
                    CapturePhase = request.CapturePhase,
                    ProcessName = traceEvent.ProcessName,
                    Pid = traceEvent.ProcessID > 0 ? traceEvent.ProcessID : null,
                    Operation = operation,
                    TimestampUtc = traceEvent.TimeStamp.ToUniversalTime().ToString("O"),
                    Hive = parts.Hive,
                    KeyPath = parts.KeyPath,
                    ValueName = valueName ?? parts.ValueName,
                    ValueType = FirstNonEmpty(
                        GetPayload(traceEvent, "Type"),
                        GetPayload(traceEvent, "ValueType")),
                    DataText = FirstNonEmpty(
                        GetPayload(traceEvent, "Data"),
                        GetPayload(traceEvent, "ValueData"),
                        traceEvent.FormattedMessage),
                    Result = FirstNonEmpty(
                        GetPayload(traceEvent, "Status"),
                        GetPayload(traceEvent, "Result")),
                    EvidenceRefs = request.EvidenceRefs ?? []
                });
            };
            source.Process();

            if (events.Count == 0)
            {
                return BuildErrorBundle(request, inputPath, "no-registry-events", "TraceEvent did not surface any registry events in the ETL.");
            }

            return new NormalizedRegistryBundle
            {
                RunId = request.RunId,
                SourceTool = request.SourceTool,
                CapturePhase = request.CapturePhase,
                GeneratedUtc = DateTimeOffset.UtcNow.ToString("O"),
                NormalizerName = Name,
                InputPath = inputPath,
                Status = "ok",
                EventCount = events.Count,
                FilteredEventCount = events.Count,
                EvidenceRefs = request.EvidenceRefs ?? [],
                Events = events
            };
        }
        catch (Exception ex)
        {
            return BuildErrorBundle(request, inputPath, "etl-normalization-failed", ex.Message);
        }
    }

    private static bool IsRegistryEvent(TraceEvent traceEvent)
    {
        var providerName = traceEvent.ProviderName ?? string.Empty;
        var taskName = traceEvent.TaskName ?? string.Empty;
        var eventName = traceEvent.EventName ?? string.Empty;
        return providerName.Contains("Registry", StringComparison.OrdinalIgnoreCase)
            || taskName.Contains("Registry", StringComparison.OrdinalIgnoreCase)
            || eventName.Contains("Registry", StringComparison.OrdinalIgnoreCase)
            || providerName.Equals("Microsoft-Windows-Kernel-Registry", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetPayload(TraceEvent traceEvent, string name)
    {
        foreach (var payloadName in traceEvent.PayloadNames)
        {
            if (!string.Equals(payloadName, name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var payload = traceEvent.PayloadByName(payloadName);
            return payload?.ToString();
        }

        return null;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static NormalizedRegistryBundle BuildErrorBundle(
        RegistryNormalizationRequest request,
        string inputPath,
        string errorKind,
        string error)
    {
        return new NormalizedRegistryBundle
        {
            RunId = request.RunId,
            SourceTool = request.SourceTool,
            CapturePhase = request.CapturePhase,
            GeneratedUtc = DateTimeOffset.UtcNow.ToString("O"),
            NormalizerName = nameof(TraceEventEtlRegistryNormalizer),
            InputPath = inputPath,
            Status = "error",
            ErrorKind = errorKind,
            Errors = [error],
            EventCount = 0,
            FilteredEventCount = 0,
            EvidenceRefs = request.EvidenceRefs ?? [],
            Events = []
        };
    }
}
