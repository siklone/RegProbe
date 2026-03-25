using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using OpenTraceProject.Core;
using OpenTraceProject.Core.Commands;

namespace OpenTraceProject.Engine.Tweaks.Commands.Security;

public sealed class DisableUacFullTweak : CommandTweak
{
    private const string System32RegExe = "reg.exe";
    private const string TargetKey = @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
    private const string TargetValueName = "EnableLUA";
    private const string MissingState = "__missing__";
    private static readonly Regex ValueRegex = new(@"\bEnableLUA\s+REG_DWORD\s+0x(?<value>[0-9a-fA-F]+)\b", RegexOptions.Compiled);

    public DisableUacFullTweak(ICommandRunner commandRunner)
        : base(
            id: "security.disable-uac",
            name: "Disable UAC (Full)",
            description: "Disables User Account Control entirely. Requires a reboot and severely lowers system security.",
            risk: TweakRiskLevel.Risky,
            commandRunner: commandRunner)
    {
    }

    protected override CommandRequest GetDetectCommand()
    {
        var executable = global::System.IO.Path.Combine(Environment.SystemDirectory, System32RegExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "query", TargetKey, "/v", TargetValueName }));
    }

    protected override CommandRequest GetApplyCommand()
    {
        var executable = global::System.IO.Path.Combine(Environment.SystemDirectory, System32RegExe);
        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "add", TargetKey, "/v", TargetValueName, "/t", "REG_DWORD", "/d", "0", "/f" }));
    }

    protected override CommandRequest? GetRollbackCommand(string detectedState)
    {
        var executable = global::System.IO.Path.Combine(Environment.SystemDirectory, System32RegExe);

        if (string.Equals(detectedState, MissingState, StringComparison.OrdinalIgnoreCase))
        {
            return new CommandRequest(
                executable,
                new ReadOnlyCollection<string>(new[] { "delete", TargetKey, "/v", TargetValueName, "/f" }));
        }

        return new CommandRequest(
            executable,
            new ReadOnlyCollection<string>(new[] { "add", TargetKey, "/v", TargetValueName, "/t", "REG_DWORD", "/d", detectedState, "/f" }));
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        if (result.ExitCode != 0)
        {
            state = MissingState;
            return false;
        }

        var match = ValueRegex.Match(result.StandardOutput);
        if (!match.Success)
        {
            state = MissingState;
            return false;
        }

        state = Convert.ToInt32(match.Groups["value"].Value, 16).ToString();
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        return ValueRegex.Match(result.StandardOutput) is { Success: true } match
            && string.Equals(match.Groups["value"].Value, "0", StringComparison.OrdinalIgnoreCase);
    }
}
