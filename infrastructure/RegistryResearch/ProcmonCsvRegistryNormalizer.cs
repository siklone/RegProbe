using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic.FileIO;

namespace RegProbe.Infrastructure.RegistryResearch;

public sealed class ProcmonCsvRegistryNormalizer : IRegistryTraceNormalizer
{
    public string Name => nameof(ProcmonCsvRegistryNormalizer);

    public bool CanNormalize(string inputPath)
        => string.Equals(Path.GetExtension(inputPath), ".csv", StringComparison.OrdinalIgnoreCase);

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
            using var parser = new TextFieldParser(inputPath)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true
            };
            parser.SetDelimiters(",");

            if (parser.EndOfData)
            {
                return BuildErrorBundle(request, inputPath, "empty-csv", "Procmon CSV did not contain a header row.");
            }

            var headers = parser.ReadFields() ?? [];
            var headerMap = headers
                .Select((value, index) => new KeyValuePair<string, int>(value, index))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

            while (!parser.EndOfData)
            {
                var fields = parser.ReadFields();
                if (fields is null || fields.Length == 0)
                {
                    continue;
                }

                var operation = GetField(fields, headerMap, "Operation");
                if (string.IsNullOrWhiteSpace(operation) || !operation.StartsWith("Reg", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var rawPath = GetField(fields, headerMap, "Path");
                var splitValue = operation.Contains("Value", StringComparison.OrdinalIgnoreCase);
                var parts = RegistryPathParser.Parse(rawPath, splitValue);
                var detail = GetField(fields, headerMap, "Detail");
                var (valueType, dataText) = ParseDetail(detail);

                events.Add(new NormalizedRegistryEvent
                {
                    RunId = request.RunId,
                    SourceTool = request.SourceTool,
                    CapturePhase = request.CapturePhase,
                    ProcessName = GetField(fields, headerMap, "Process Name"),
                    Pid = ParseInt(GetField(fields, headerMap, "PID")),
                    Operation = operation,
                    TimestampUtc = NormalizeTimestamp(GetField(fields, headerMap, "Time of Day")),
                    Hive = parts.Hive,
                    KeyPath = parts.KeyPath,
                    ValueName = parts.ValueName,
                    ValueType = valueType,
                    DataText = dataText ?? detail,
                    Result = GetField(fields, headerMap, "Result"),
                    EvidenceRefs = request.EvidenceRefs ?? []
                });
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
            return BuildErrorBundle(request, inputPath, "procmon-normalization-failed", ex.Message);
        }
    }

    private static string? GetField(string[] fields, IReadOnlyDictionary<string, int> headerMap, string name)
    {
        return headerMap.TryGetValue(name, out var index) && index >= 0 && index < fields.Length
            ? fields[index]
            : null;
    }

    private static (string? ValueType, string? DataText) ParseDetail(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
        {
            return (null, null);
        }

        string? valueType = null;
        string? data = null;
        foreach (var segment in detail.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.StartsWith("Type:", StringComparison.OrdinalIgnoreCase))
            {
                valueType = segment["Type:".Length..].Trim();
            }
            else if (segment.StartsWith("Data:", StringComparison.OrdinalIgnoreCase))
            {
                data = segment["Data:".Length..].Trim();
            }
        }

        return (valueType, data);
    }

    private static int? ParseInt(string? raw)
        => int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static string? NormalizeTimestamp(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
            ? parsed.ToUniversalTime().ToString("O")
            : raw.Trim();
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
            NormalizerName = nameof(ProcmonCsvRegistryNormalizer),
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
