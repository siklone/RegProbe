using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenTraceProject.Core;
using OpenTraceProject.Core.Commands;

namespace OpenTraceProject.Engine.Tweaks.Commands.Power;

public sealed class SetCpuBoostPerfModeTweak : CommandTweak
{
    private const string PowerCfgExe = "powercfg.exe";
    private const string ProcessorSubgroup = "SUB_PROCESSOR";
    private const string PerfBoostModeSetting = "PERFBOOSTMODE";
    private const int AggressiveValue = 2;

    private static readonly Regex CurrentAcRegex = new(@"Current AC Power Setting Index:\s*0x(?<value>[0-9A-Fa-f]+)", RegexOptions.Compiled);
    private static readonly Regex CurrentDcRegex = new(@"Current DC Power Setting Index:\s*0x(?<value>[0-9A-Fa-f]+)", RegexOptions.Compiled);

    public SetCpuBoostPerfModeTweak(ICommandRunner commandRunner)
        : base(
            id: "power.optimize-cpu-boost",
            name: "Optimize CPU Performance Boost",
            description: "Sets PERFBOOSTMODE to Aggressive on the active power plan using the documented power setting surface.",
            risk: TweakRiskLevel.Safe,
            commandRunner: commandRunner)
    {
    }

    protected override CommandRequest GetDetectCommand()
    {
        return new CommandRequest(
            GetPowerCfgPath(),
            new ReadOnlyCollection<string>(new[]
            {
                "/qh",
                "SCHEME_CURRENT",
                ProcessorSubgroup,
                PerfBoostModeSetting
            }),
            TimeoutSeconds: 60);
    }

    protected override CommandRequest GetApplyCommand()
    {
        return new CommandRequest(
            GetPowerCfgPath(),
            new ReadOnlyCollection<string>(new[]
            {
                "/setacvalueindex",
                "SCHEME_CURRENT",
                ProcessorSubgroup,
                PerfBoostModeSetting,
                AggressiveValue.ToString(CultureInfo.InvariantCulture),
                "/setdcvalueindex",
                "SCHEME_CURRENT",
                ProcessorSubgroup,
                PerfBoostModeSetting,
                AggressiveValue.ToString(CultureInfo.InvariantCulture),
                "/setactive",
                "SCHEME_CURRENT"
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
            GetPowerCfgPath(),
            new ReadOnlyCollection<string>(new[]
            {
                "/setacvalueindex",
                "SCHEME_CURRENT",
                ProcessorSubgroup,
                PerfBoostModeSetting,
                snapshot.AcValue.ToString(CultureInfo.InvariantCulture),
                "/setdcvalueindex",
                "SCHEME_CURRENT",
                ProcessorSubgroup,
                PerfBoostModeSetting,
                snapshot.DcValue.ToString(CultureInfo.InvariantCulture),
                "/setactive",
                "SCHEME_CURRENT"
            }),
            TimeoutSeconds: 90);
    }

    protected override bool ParseDetectedState(CommandResult result, out string state)
    {
        if (!TryParsePowerCfgOutput(result.StandardOutput, out var snapshot))
        {
            state = string.Empty;
            return false;
        }

        state = JsonSerializer.Serialize(snapshot);
        return true;
    }

    protected override bool VerifyApplied(CommandResult result)
    {
        return TryParsePowerCfgOutput(result.StandardOutput, out var snapshot)
               && snapshot.AcValue == AggressiveValue
               && snapshot.DcValue == AggressiveValue;
    }

    private static string GetPowerCfgPath()
    {
        return global::System.IO.Path.Combine(Environment.SystemDirectory, PowerCfgExe);
    }

    private static bool TryParsePowerCfgOutput(string output, out PerfBoostSnapshot snapshot)
    {
        snapshot = new PerfBoostSnapshot(0, 0);

        if (string.IsNullOrWhiteSpace(output))
        {
            return false;
        }

        var acMatch = CurrentAcRegex.Match(output);
        var dcMatch = CurrentDcRegex.Match(output);
        if (!acMatch.Success || !dcMatch.Success)
        {
            return false;
        }

        if (!int.TryParse(acMatch.Groups["value"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var acValue))
        {
            return false;
        }

        if (!int.TryParse(dcMatch.Groups["value"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var dcValue))
        {
            return false;
        }

        snapshot = new PerfBoostSnapshot(acValue, dcValue);
        return true;
    }

    private static bool TryParseSnapshot(string detectedState, out PerfBoostSnapshot snapshot)
    {
        snapshot = new PerfBoostSnapshot(0, 0);

        if (string.IsNullOrWhiteSpace(detectedState))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<PerfBoostSnapshot>(detectedState);
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

    private sealed record PerfBoostSnapshot(int AcValue, int DcValue);
}
