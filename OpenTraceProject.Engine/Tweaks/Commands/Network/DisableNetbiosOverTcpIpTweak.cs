using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using OpenTraceProject.Core;
using OpenTraceProject.Core.Commands;

namespace OpenTraceProject.Engine.Tweaks.Commands.Network;

public sealed class DisableNetbiosOverTcpIpTweak : CommandTweak
{
    private const string PowerShellExe = "powershell.exe";
    private const int DisabledOption = 2;

    public DisableNetbiosOverTcpIpTweak(ICommandRunner commandRunner)
        : base(
            id: "network.disable-netbios",
            name: "Disable NetBIOS over TCP/IP",
            description: "Disables NetBIOS over TCP/IP on all IP-enabled adapters using the documented per-interface Windows control surface.",
            risk: TweakRiskLevel.Advanced,
            commandRunner: commandRunner)
    {
    }

    protected override CommandRequest GetDetectCommand()
    {
        return new CommandRequest(
            GetPowerShellPath(),
            new ReadOnlyCollection<string>(new[]
            {
                "-NoProfile",
                "-NonInteractive",
                "-Command",
                BuildDetectScript()
            }),
            TimeoutSeconds: 60);
    }

    protected override CommandRequest GetApplyCommand()
    {
        return new CommandRequest(
            GetPowerShellPath(),
            new ReadOnlyCollection<string>(new[]
            {
                "-NoProfile",
                "-NonInteractive",
                "-Command",
                BuildApplyScript(DisabledOption)
            }),
            TimeoutSeconds: 90);
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        if (!TryParseAdapterStates(detectedState, out var adapterStates) || adapterStates.Count == 0)
        {
            return null;
        }

        var encodedState = Convert.ToBase64String(Encoding.UTF8.GetBytes(detectedState));
        var script =
            "$json = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + encodedState + "')); " +
            "$items = @($json | ConvertFrom-Json); " +
            "foreach ($item in $items) { " +
            "  $cfg = Get-CimInstance Win32_NetworkAdapterConfiguration | Where-Object { $_.SettingID -eq $item.SettingID }; " +
            "  if ($null -eq $cfg) { continue } " +
            "  $result = Invoke-CimMethod -InputObject $cfg -MethodName SetTcpipNetbios -Arguments @{ TcpipNetbiosOptions = [uint32]$item.TcpipNetbiosOptions }; " +
            "  if ($result.ReturnValue -ne 0 -and $result.ReturnValue -ne 1) { throw \"SetTcpipNetbios restore failed for adapter $($item.Index) with return code $($result.ReturnValue).\" } " +
            "} " +
            "Write-Output (\"Restored NetBIOS over TCP/IP state on {0} adapters.\" -f $items.Count)";

        return new CommandRequest(
            GetPowerShellPath(),
            new ReadOnlyCollection<string>(new[]
            {
                "-NoProfile",
                "-NonInteractive",
                "-Command",
                script
            }),
            TimeoutSeconds: 90);
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        var output = result.StandardOutput.Trim();
        if (!TryParseAdapterStates(output, out var adapterStates))
        {
            state = string.Empty;
            return false;
        }

        state = JsonSerializer.Serialize(adapterStates);
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        return TryParseAdapterStates(result.StandardOutput.Trim(), out var adapterStates)
               && adapterStates.Count > 0
               && adapterStates.All(adapter => adapter.TcpipNetbiosOptions == DisabledOption);
    }

    private static string GetPowerShellPath()
    {
        return Path.Combine(
            Environment.SystemDirectory,
            "WindowsPowerShell",
            "v1.0",
            PowerShellExe);
    }

    private static string BuildDetectScript()
    {
        return
            "$items = @(Get-CimInstance Win32_NetworkAdapterConfiguration | " +
            "Where-Object { $_.IPEnabled -eq $true } | Sort-Object Index | ForEach-Object { " +
            "  [pscustomobject]@{ " +
            "    Index = [int]$_.Index; " +
            "    SettingID = [string]$_.SettingID; " +
            "    Description = [string]$_.Description; " +
            "    TcpipNetbiosOptions = if ($null -eq $_.TcpipNetbiosOptions) { -1 } else { [int]$_.TcpipNetbiosOptions } " +
            "  } " +
            "}); " +
            "$items | ConvertTo-Json -Compress -Depth 3";
    }

    private static string BuildApplyScript(int targetOption)
    {
        return
            "$configs = @(Get-CimInstance Win32_NetworkAdapterConfiguration | Where-Object { $_.IPEnabled -eq $true }); " +
            "foreach ($cfg in $configs) { " +
            "  $result = Invoke-CimMethod -InputObject $cfg -MethodName SetTcpipNetbios -Arguments @{ TcpipNetbiosOptions = [uint32]" + targetOption + " }; " +
            "  if ($result.ReturnValue -ne 0 -and $result.ReturnValue -ne 1) { throw \"SetTcpipNetbios failed for adapter $($cfg.Index) with return code $($result.ReturnValue).\" } " +
            "} " +
            "Write-Output (\"Updated NetBIOS over TCP/IP on {0} adapters.\" -f $configs.Count)";
    }

    private static bool TryParseAdapterStates(string output, out List<AdapterState> adapterStates)
    {
        adapterStates = new List<AdapterState>();
        if (string.IsNullOrWhiteSpace(output))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(output);
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in document.RootElement.EnumerateArray())
                {
                    adapterStates.Add(ParseAdapterState(element));
                }
            }
            else if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                adapterStates.Add(ParseAdapterState(document.RootElement));
            }
            else
            {
                return false;
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static AdapterState ParseAdapterState(JsonElement element)
    {
        var index = element.TryGetProperty("Index", out var indexElement) && indexElement.TryGetInt32(out var parsedIndex)
            ? parsedIndex
            : -1;
        var settingId = element.TryGetProperty("SettingID", out var settingIdElement)
            ? settingIdElement.GetString() ?? string.Empty
            : string.Empty;
        var description = element.TryGetProperty("Description", out var descriptionElement)
            ? descriptionElement.GetString() ?? string.Empty
            : string.Empty;
        var tcpipNetbiosOptions = element.TryGetProperty("TcpipNetbiosOptions", out var optionsElement) && optionsElement.TryGetInt32(out var parsedOption)
            ? parsedOption
            : -1;

        return new AdapterState(index, settingId, description, tcpipNetbiosOptions);
    }

    private sealed record AdapterState(int Index, string SettingID, string Description, int TcpipNetbiosOptions);
}
