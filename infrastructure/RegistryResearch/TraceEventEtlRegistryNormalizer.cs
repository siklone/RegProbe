using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Diagnostics.Tracing;

namespace RegProbe.Infrastructure.RegistryResearch;

public sealed class TraceEventEtlRegistryNormalizer : IRegistryTraceNormalizer
{
    private readonly Func<string, IEnumerable<RegistryTraceEventRecord>> _recordLoader;

    public string Name => nameof(TraceEventEtlRegistryNormalizer);

    public TraceEventEtlRegistryNormalizer()
        : this(LoadRecords)
    {
    }

    internal TraceEventEtlRegistryNormalizer(Func<string, IEnumerable<RegistryTraceEventRecord>> recordLoader)
    {
        _recordLoader = recordLoader ?? throw new ArgumentNullException(nameof(recordLoader));
    }

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
            foreach (var record in _recordLoader(inputPath))
            {
                if (!IsRegistryEvent(record))
                {
                    continue;
                }

                var operation = FirstNonEmpty(record.EventName, record.OpcodeName, record.TaskName) ?? "RegistryEvent";
                var rawPath = FirstNonEmpty(
                    GetPayload(record, "KeyName"),
                    GetPayload(record, "Key"),
                    GetPayload(record, "KeyPath"),
                    GetPayload(record, "Path"),
                    GetPayload(record, "BaseName"));
                var valueName = FirstNonEmpty(
                    GetPayload(record, "ValueName"),
                    GetPayload(record, "Value"),
                    GetPayload(record, "Name"));
                var splitValue = string.IsNullOrWhiteSpace(valueName) && operation.Contains("Value", StringComparison.OrdinalIgnoreCase);
                var parts = RegistryPathParser.Parse(rawPath, splitValue);

                events.Add(new NormalizedRegistryEvent
                {
                    RunId = request.RunId,
                    SourceTool = request.SourceTool,
                    CapturePhase = request.CapturePhase,
                    ProcessName = record.ProcessName,
                    Pid = record.ProcessId > 0 ? record.ProcessId : null,
                    Operation = operation,
                    TimestampUtc = record.TimestampUtc?.ToUniversalTime().ToString("O"),
                    Hive = parts.Hive,
                    KeyPath = parts.KeyPath,
                    ValueName = valueName ?? parts.ValueName,
                    ValueType = FirstNonEmpty(
                        GetPayload(record, "Type"),
                        GetPayload(record, "ValueType")),
                    DataText = FirstNonEmpty(
                        GetPayload(record, "Data"),
                        GetPayload(record, "ValueData"),
                        record.FormattedMessage),
                    Result = FirstNonEmpty(
                        GetPayload(record, "Status"),
                        GetPayload(record, "Result")),
                    EvidenceRefs = request.EvidenceRefs ?? []
                });
            }

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

    private static IEnumerable<RegistryTraceEventRecord> LoadRecords(string inputPath)
    {
        var records = new List<RegistryTraceEventRecord>();
        using var source = new ETWTraceEventSource(inputPath);
        source.Dynamic.All += traceEvent =>
        {
            var payloads = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var payloadName in traceEvent.PayloadNames)
            {
                payloads[payloadName] = traceEvent.PayloadByName(payloadName)?.ToString();
            }

            records.Add(new RegistryTraceEventRecord
            {
                ProviderName = traceEvent.ProviderName,
                TaskName = traceEvent.TaskName,
                EventName = traceEvent.EventName,
                OpcodeName = traceEvent.OpcodeName,
                ProcessName = traceEvent.ProcessName,
                ProcessId = traceEvent.ProcessID > 0 ? traceEvent.ProcessID : null,
                TimestampUtc = new DateTimeOffset(traceEvent.TimeStamp.ToUniversalTime()),
                FormattedMessage = traceEvent.FormattedMessage,
                Payloads = payloads
            });
        };
        source.Process();
        return records;
    }

    private static bool IsRegistryEvent(RegistryTraceEventRecord record)
    {
        var providerName = record.ProviderName ?? string.Empty;
        var taskName = record.TaskName ?? string.Empty;
        var eventName = record.EventName ?? string.Empty;
        return providerName.Contains("Registry", StringComparison.OrdinalIgnoreCase)
            || taskName.Contains("Registry", StringComparison.OrdinalIgnoreCase)
            || eventName.Contains("Registry", StringComparison.OrdinalIgnoreCase)
            || providerName.Equals("Microsoft-Windows-Kernel-Registry", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetPayload(RegistryTraceEventRecord record, string name)
    {
        foreach (var payloadName in record.Payloads.Keys)
        {
            if (!string.Equals(payloadName, name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return record.Payloads[payloadName];
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
