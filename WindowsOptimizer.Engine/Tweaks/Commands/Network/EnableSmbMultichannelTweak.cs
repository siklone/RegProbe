using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Commands;
using WindowsOptimizer.Engine.Tweaks.Commands;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Network;

public sealed class EnableSmbMultichannelTweak : CommandTweak
{
    private const string PowerShellExe = "powershell.exe";

    public EnableSmbMultichannelTweak(ICommandRunner commandRunner)
        : base(
            id: "network.smb-enable-multichannel",
            name: "SMB: Enable Multichannel",
            description: "Enables SMB Multichannel on the documented SMB client and SMB server configuration surfaces.",
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
                BuildSetScript(enableClient: true, enableServer: true)
            }),
            TimeoutSeconds: 90);
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        if (!TryParseSnapshot(detectedState, out var snapshot))
        {
            return null;
        }

        return new CommandRequest(
            GetPowerShellPath(),
            new ReadOnlyCollection<string>(new[]
            {
                "-NoProfile",
                "-NonInteractive",
                "-Command",
                BuildSetScript(snapshot.ClientEnableMultiChannel, snapshot.ServerEnableMultiChannel)
            }),
            TimeoutSeconds: 90);
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        var output = result.StandardOutput.Trim();
        if (!TryParseSnapshot(output, out var snapshot))
        {
            state = string.Empty;
            return false;
        }

        state = JsonSerializer.Serialize(snapshot);
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        return TryParseSnapshot(result.StandardOutput.Trim(), out var snapshot)
               && snapshot.ClientEnableMultiChannel
               && snapshot.ServerEnableMultiChannel;
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
            "$client = (Get-SmbClientConfiguration).EnableMultiChannel; " +
            "$server = (Get-SmbServerConfiguration).EnableMultiChannel; " +
            "[pscustomobject]@{ ClientEnableMultiChannel = [bool]$client; ServerEnableMultiChannel = [bool]$server } | ConvertTo-Json -Compress";
    }

    private static string BuildSetScript(bool enableClient, bool enableServer)
    {
        var clientLiteral = enableClient ? "$true" : "$false";
        var serverLiteral = enableServer ? "$true" : "$false";
        var clientLabel = enableClient ? "True" : "False";
        var serverLabel = enableServer ? "True" : "False";

        return
            "Set-SmbClientConfiguration -EnableMultiChannel " + clientLiteral + " -Force | Out-Null; " +
            "Set-SmbServerConfiguration -EnableMultiChannel " + serverLiteral + " -Force | Out-Null; " +
            "Write-Output '{\"ClientEnableMultiChannel\":" + clientLabel.ToLowerInvariant() + ",\"ServerEnableMultiChannel\":" + serverLabel.ToLowerInvariant() + "}'";
    }

    private static bool TryParseSnapshot(string output, out Snapshot snapshot)
    {
        snapshot = new Snapshot(false, false);
        if (string.IsNullOrWhiteSpace(output))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Snapshot>(output);
            if (parsed is null)
            {
                return false;
            }

            snapshot = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record Snapshot(bool ClientEnableMultiChannel, bool ServerEnableMultiChannel);
}
