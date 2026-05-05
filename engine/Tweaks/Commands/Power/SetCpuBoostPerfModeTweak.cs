using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.Core;
using RegProbe.Core.Commands;

namespace RegProbe.Engine.Tweaks.Commands.Power;

public sealed class SetCpuBoostPerfModeTweak : ITweak
{
    private const string PowerCfgExe = "powercfg.exe";
    private const string ProcessorSubgroup = "SUB_PROCESSOR";
    private const string PerfBoostModeSetting = "PERFBOOSTMODE";
    private const int AggressiveValue = 2;

    private static readonly Regex CurrentAcRegex = new(@"Current AC Power Setting Index:\s*0x(?<value>[0-9A-Fa-f]+)", RegexOptions.Compiled);
    private static readonly Regex CurrentDcRegex = new(@"Current DC Power Setting Index:\s*0x(?<value>[0-9A-Fa-f]+)", RegexOptions.Compiled);

    private readonly ICommandRunner _commandRunner;
    private PerfBoostSnapshot? _snapshot;

    public SetCpuBoostPerfModeTweak(
        ICommandRunner commandRunner,
        string? name = null,
        string? description = null)
    {
        _commandRunner = commandRunner ?? throw new ArgumentNullException(nameof(commandRunner));
        Name = name ?? "Set CPU Boost Mode (Aggressive)";
        Description = description ?? "Sets PERFBOOSTMODE to Aggressive on the active power plan using the documented powercfg surface.";
    }

    public string Id => "power.optimize-cpu-boost";
    public string Name { get; }
    public string Description { get; }
    public TweakRiskLevel Risk => TweakRiskLevel.Safe;
    public bool RequiresElevation => true;

    public async Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var snapshot = await ReadSnapshotAsync(ct);
            _snapshot = snapshot;

            var isApplied = snapshot.AcValue == AggressiveValue && snapshot.DcValue == AggressiveValue;
            var message = string.Create(
                CultureInfo.InvariantCulture,
                $"Current state: {JsonSerializer.Serialize(snapshot)}");

            return new TweakResult(
                isApplied ? TweakStatus.Applied : TweakStatus.Detected,
                message,
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(TweakStatus.Failed, $"Detect failed: {ex.Message}", DateTimeOffset.UtcNow, ex);
        }
    }

    public async Task<TweakResult> ApplyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            await RunPowerCfgAsync(
                new[]
                {
                    "/setacvalueindex",
                    "SCHEME_CURRENT",
                    ProcessorSubgroup,
                    PerfBoostModeSetting,
                    AggressiveValue.ToString(CultureInfo.InvariantCulture)
                },
                ct);
            await RunPowerCfgAsync(
                new[]
                {
                    "/setdcvalueindex",
                    "SCHEME_CURRENT",
                    ProcessorSubgroup,
                    PerfBoostModeSetting,
                    AggressiveValue.ToString(CultureInfo.InvariantCulture)
                },
                ct);
            await RunPowerCfgAsync(new[] { "/setactive", "SCHEME_CURRENT" }, ct);

            return new TweakResult(
                TweakStatus.Applied,
                "Set CPU performance boost mode to Aggressive for the active power plan.",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(TweakStatus.Failed, $"Apply failed: {ex.Message}", DateTimeOffset.UtcNow, ex);
        }
    }

    public async Task<TweakResult> VerifyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var snapshot = await ReadSnapshotAsync(ct);
            var isApplied = snapshot.AcValue == AggressiveValue && snapshot.DcValue == AggressiveValue;

            return isApplied
                ? new TweakResult(TweakStatus.Verified, "CPU performance boost mode is Aggressive for AC and DC power.", DateTimeOffset.UtcNow)
                : new TweakResult(TweakStatus.Failed, $"Verification failed. Current AC/DC values: {snapshot.AcValue}/{snapshot.DcValue}.", DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(TweakStatus.Failed, $"Verify failed: {ex.Message}", DateTimeOffset.UtcNow, ex);
        }
    }

    public async Task<TweakResult> RollbackAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (_snapshot is null)
        {
            return new TweakResult(TweakStatus.Skipped, "Rollback skipped because no prior detect state is available.", DateTimeOffset.UtcNow);
        }

        try
        {
            await RunPowerCfgAsync(
                new[]
                {
                    "/setacvalueindex",
                    "SCHEME_CURRENT",
                    ProcessorSubgroup,
                    PerfBoostModeSetting,
                    _snapshot.AcValue.ToString(CultureInfo.InvariantCulture)
                },
                ct);
            await RunPowerCfgAsync(
                new[]
                {
                    "/setdcvalueindex",
                    "SCHEME_CURRENT",
                    ProcessorSubgroup,
                    PerfBoostModeSetting,
                    _snapshot.DcValue.ToString(CultureInfo.InvariantCulture)
                },
                ct);
            await RunPowerCfgAsync(new[] { "/setactive", "SCHEME_CURRENT" }, ct);

            return new TweakResult(
                TweakStatus.RolledBack,
                "Restored previous CPU performance boost mode values for the active power plan.",
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TweakResult(TweakStatus.Failed, $"Rollback failed: {ex.Message}", DateTimeOffset.UtcNow, ex);
        }
    }

    private async Task<PerfBoostSnapshot> ReadSnapshotAsync(CancellationToken ct)
    {
        var result = await RunPowerCfgAsync(
            new[]
            {
                "/qh",
                "SCHEME_CURRENT",
                ProcessorSubgroup,
                PerfBoostModeSetting
            },
            ct);

        return new PerfBoostSnapshot(
            ParseIndexedValue(result.StandardOutput, CurrentAcRegex, "AC PERFBOOSTMODE"),
            ParseIndexedValue(result.StandardOutput, CurrentDcRegex, "DC PERFBOOSTMODE"));
    }

    private async Task<CommandResult> RunPowerCfgAsync(string[] args, CancellationToken ct)
    {
        var executable = global::System.IO.Path.Combine(Environment.SystemDirectory, PowerCfgExe);
        var request = new CommandRequest(executable, new ReadOnlyCollection<string>(args));
        var result = await _commandRunner.RunAsync(request, ct);

        if (result.TimedOut)
        {
            throw new InvalidOperationException($"powercfg timed out: {string.Join(' ', args)}");
        }

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"powercfg failed ({result.ExitCode}): {result.StandardError}".Trim());
        }

        return result;
    }

    private static int ParseIndexedValue(string output, Regex regex, string label)
    {
        var match = regex.Match(output);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not parse {label} from powercfg output.");
        }

        return int.Parse(match.Groups["value"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    private sealed record PerfBoostSnapshot(int AcValue, int DcValue);
}
