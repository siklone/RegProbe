using System;
using System.Collections.Generic;
using System.IO;
using RegProbe.Infrastructure.RegistryResearch;

namespace RegProbe.Tests;

public sealed class ProcmonCsvRegistryNormalizerTests : IDisposable
{
    private readonly string _tempDirectory;

    public ProcmonCsvRegistryNormalizerTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"RegistryTraceNormalizerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in test temp roots.
        }
    }

    [Fact]
    public void ProcmonCsvNormalizer_ParsesRegistryRowsIntoNormalizedBundle()
    {
        var inputPath = Path.Combine(_tempDirectory, "procmon.csv");
        File.WriteAllText(
            inputPath,
            string.Join(
                Environment.NewLine,
                [
                    "Time of Day,Process Name,PID,Operation,Path,Result,Detail",
                    "\"4/7/2026 2:15:30 PM\",powershell.exe,4242,RegSetValue,HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer\\HideRecommendedSection,SUCCESS,\"Type: REG_DWORD, Data: 1\"",
                    "\"4/7/2026 2:15:31 PM\",powershell.exe,4242,CreateFile,C:\\temp.txt,SUCCESS,\"Desired Access: Generic Read\""
                ]),
            encoding: System.Text.Encoding.UTF8);

        var normalizer = new ProcmonCsvRegistryNormalizer();
        var bundle = normalizer.Normalize(new RegistryNormalizationRequest(
            inputPath,
            "run-procmon",
            "procmon",
            "runtime",
            ["evidence/files/test/procmon.csv"]));

        Assert.Equal("ok", bundle.Status);
        Assert.Equal(nameof(ProcmonCsvRegistryNormalizer), bundle.NormalizerName);
        Assert.Equal(1, bundle.EventCount);
        var ev = Assert.Single(bundle.Events);
        Assert.Equal("run-procmon", ev.RunId);
        Assert.Equal("procmon", ev.SourceTool);
        Assert.Equal("runtime", ev.CapturePhase);
        Assert.Equal("powershell.exe", ev.ProcessName);
        Assert.Equal(4242, ev.Pid);
        Assert.Equal("RegSetValue", ev.Operation);
        Assert.Equal("HKLM", ev.Hive);
        Assert.Equal(@"SOFTWARE\Policies\Microsoft\Windows\Explorer", ev.KeyPath);
        Assert.Equal("HideRecommendedSection", ev.ValueName);
        Assert.Equal("REG_DWORD", ev.ValueType);
        Assert.Equal("1", ev.DataText);
        Assert.Equal("SUCCESS", ev.Result);
        Assert.Equal(["evidence/files/test/procmon.csv"], ev.EvidenceRefs);
        Assert.False(string.IsNullOrWhiteSpace(ev.TimestampUtc));
    }

    [Fact]
    public void ProcmonCsvNormalizer_EmptyCsvReturnsDeterministicError()
    {
        var inputPath = Path.Combine(_tempDirectory, "empty.csv");
        File.WriteAllText(inputPath, string.Empty);

        var normalizer = new ProcmonCsvRegistryNormalizer();
        var bundle = normalizer.Normalize(new RegistryNormalizationRequest(inputPath, "run-empty", "procmon", "runtime"));

        Assert.Equal("error", bundle.Status);
        Assert.Equal("empty-csv", bundle.ErrorKind);
        Assert.Empty(bundle.Events);
    }

    [Fact]
    public void ProcmonCsvNormalizer_MissingInputReturnsDeterministicError()
    {
        var inputPath = Path.Combine(_tempDirectory, "missing.csv");

        var normalizer = new ProcmonCsvRegistryNormalizer();
        var bundle = normalizer.Normalize(new RegistryNormalizationRequest(inputPath, "run-missing", "procmon", "runtime"));

        Assert.Equal("error", bundle.Status);
        Assert.Equal("missing-input", bundle.ErrorKind);
        Assert.Contains("Input trace was not found", Assert.Single(bundle.Errors));
    }
}

public sealed class TraceEventEtlRegistryNormalizerTests
{
    [Fact]
    public void TraceEventEtlNormalizer_MapsInjectedRegistryEvents()
    {
        var records = new[]
        {
            new RegistryTraceEventRecord
            {
                ProviderName = "Microsoft-Windows-Kernel-Registry",
                EventName = "RegQueryValue",
                ProcessName = "svchost.exe",
                ProcessId = 188,
                TimestampUtc = DateTimeOffset.Parse("2026-04-07T12:15:30Z"),
                Payloads = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["KeyName"] = @"HKLM\System\CurrentControlSet\Control\Session Manager\Executive\UuidSequenceNumber",
                    ["Type"] = "REG_DWORD",
                    ["Data"] = "0x002caf1b",
                    ["Status"] = "SUCCESS"
                }
            },
            new RegistryTraceEventRecord
            {
                ProviderName = "Microsoft-Windows-Kernel-Registry",
                EventName = "RegSetValue",
                ProcessName = "explorer.exe",
                ProcessId = 3440,
                TimestampUtc = DateTimeOffset.Parse("2026-04-07T12:15:31Z"),
                Payloads = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Key"] = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                    ["ValueName"] = "HideFileExt",
                    ["ValueType"] = "REG_DWORD",
                    ["ValueData"] = "0",
                    ["Result"] = "SUCCESS"
                }
            }
        };

        var normalizer = new TraceEventEtlRegistryNormalizer(_ => records);
        var tempPath = Path.Combine(Path.GetTempPath(), $"trace-{Guid.NewGuid():N}.etl");
        File.WriteAllText(tempPath, "fixture");
        try
        {
            var bundle = normalizer.Normalize(new RegistryNormalizationRequest(
                tempPath,
                "run-etl",
                "etw",
                "boot",
                ["evidence/files/test/trace.etl"]));

            Assert.Equal("ok", bundle.Status);
            Assert.Equal(2, bundle.EventCount);

            var query = bundle.Events[0];
            Assert.Equal("HKLM", query.Hive);
            Assert.Equal(@"System\CurrentControlSet\Control\Session Manager\Executive", query.KeyPath);
            Assert.Equal("UuidSequenceNumber", query.ValueName);
            Assert.Equal("REG_DWORD", query.ValueType);
            Assert.Equal("0x002caf1b", query.DataText);
            Assert.Equal("SUCCESS", query.Result);

            var set = bundle.Events[1];
            Assert.Equal("HKCU", set.Hive);
            Assert.Equal(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", set.KeyPath);
            Assert.Equal("HideFileExt", set.ValueName);
            Assert.Equal("REG_DWORD", set.ValueType);
            Assert.Equal("0", set.DataText);
            Assert.Equal("SUCCESS", set.Result);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void TraceEventEtlNormalizer_NoInjectedRegistryEventsReturnsDeterministicError()
    {
        var normalizer = new TraceEventEtlRegistryNormalizer(_ => Array.Empty<RegistryTraceEventRecord>());
        var tempPath = Path.GetTempFileName();
        try
        {
            var bundle = normalizer.Normalize(new RegistryNormalizationRequest(tempPath, "run-empty-etl", "etw", "boot"));
            Assert.Equal("error", bundle.Status);
            Assert.Equal("no-registry-events", bundle.ErrorKind);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void TraceEventEtlNormalizer_MissingInputReturnsDeterministicError()
    {
        var normalizer = new TraceEventEtlRegistryNormalizer(_ => Array.Empty<RegistryTraceEventRecord>());
        var bundle = normalizer.Normalize(new RegistryNormalizationRequest("C:\\missing\\trace.etl", "run-missing-etl", "etw", "boot"));

        Assert.Equal("error", bundle.Status);
        Assert.Equal("missing-input", bundle.ErrorKind);
        Assert.Contains("Input trace was not found", Assert.Single(bundle.Errors));
    }
}
