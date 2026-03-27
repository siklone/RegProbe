using System;
using System.Collections.ObjectModel;
using System.IO;
using RegProbe.Core;
using RegProbe.Core.Commands;
using RegProbe.Engine.Tweaks.Commands;

namespace RegProbe.Engine.Tweaks.Commands.Network;

public sealed class DisableSmbLeasingTweak : CommandTweak
{
    private const string PowerShellExe = "powershell.exe";

    public DisableSmbLeasingTweak(ICommandRunner commandRunner)
        : base(
            id: "network.smb-disable-leasing",
            name: "SMB: Disable Leasing",
            description: "Disables SMB server leasing using the documented SMB server configuration surface.",
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
                BuildSetScript(enableLeasing: false)
            }),
            TimeoutSeconds: 90);
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        if (!bool.TryParse(detectedState, out var enableLeasing))
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
                BuildSetScript(enableLeasing)
            }),
            TimeoutSeconds: 90);
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        var output = result.StandardOutput.Trim();
        if (!bool.TryParse(output, out var enableLeasing))
        {
            state = string.Empty;
            return false;
        }

        state = enableLeasing ? bool.TrueString : bool.FalseString;
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        return bool.TryParse(result.StandardOutput.Trim(), out var enableLeasing) && !enableLeasing;
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
            "$value = (Get-SmbServerConfiguration).EnableLeasing; " +
            "if ($value) { Write-Output 'True' } else { Write-Output 'False' }";
    }

    private static string BuildSetScript(bool enableLeasing)
    {
        var boolLiteral = enableLeasing ? "$true" : "$false";
        var label = enableLeasing ? "True" : "False";

        return
            "Set-SmbServerConfiguration -EnableLeasing " + boolLiteral + " -Force | Out-Null; " +
            "Write-Output 'EnableLeasing=" + label + "'";
    }
}
