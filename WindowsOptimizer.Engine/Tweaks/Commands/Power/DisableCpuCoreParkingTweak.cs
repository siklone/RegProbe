using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Commands;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Power;

public sealed class DisableCpuCoreParkingTweak : ITweak
{
    private const string System32PowerCfgExe = "powercfg.exe";
    private const string ProcessorSubgroup = "SUB_PROCESSOR";
    private const string CoreParkingMinCores = "CPMINCORES";
    private const string CoreParkingMaxCores = "CPMAXCORES";
    private const int DisabledCoreParkingValue = 100;
    private static readonly Regex CurrentAcRegex = new(@"Current AC Power Setting Index:\s*0x(?<value>[0-9A-Fa-f]+)", RegexOptions.Compiled);
    private static readonly Regex CurrentDcRegex = new(@"Current DC Power Setting Index:\s*0x(?<value>[0-9A-Fa-f]+)", RegexOptions.Compiled);

    private readonly ICommandRunner _commandRunner;
    private CpuCoreParkingSnapshot? _snapshot;

    public DisableCpuCoreParkingTweak(ICommandRunner commandRunner)
    {
        _commandRunner = commandRunner ?? throw new ArgumentNullException(nameof(commandRunner));
    }

    public string Id => "power.disable-cpu-parking";
    public string Name => "Disable CPU Core Parking";
    public string Description => "Prevents Windows from parking CPU cores by setting the active power plan core parking limits to 100% for AC and DC power.";
    public TweakRiskLevel Risk => TweakRiskLevel.Safe;
    public bool RequiresElevation => true;

    public async Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var snapshot = await ReadSnapshotAsync(ct);
            _snapshot = snapshot;

            var isApplied = snapshot.MinAc == DisabledCoreParkingValue
                && snapshot.MinDc == DisabledCoreParkingValue
                && snapshot.MaxAc == DisabledCoreParkingValue
                && snapshot.MaxDc == DisabledCoreParkingValue;

            var message = string.Create(
                CultureInfo.InvariantCulture,
                $"Current plan values - Min AC/DC: {snapshot.MinAc}/{snapshot.MinDc}, Max AC/DC: {snapshot.MaxAc}/{snapshot.MaxDc}.");

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
            await RunPowerCfgAsync(new[] { "/setacvalueindex", "SCHEME_CURRENT", ProcessorSubgroup, CoreParkingMinCores, DisabledCoreParkingValue.ToString(CultureInfo.InvariantCulture) }, ct);
            await RunPowerCfgAsync(new[] { "/setdcvalueindex", "SCHEME_CURRENT", ProcessorSubgroup, CoreParkingMinCores, DisabledCoreParkingValue.ToString(CultureInfo.InvariantCulture) }, ct);
            await RunPowerCfgAsync(new[] { "/setacvalueindex", "SCHEME_CURRENT", ProcessorSubgroup, CoreParkingMaxCores, DisabledCoreParkingValue.ToString(CultureInfo.InvariantCulture) }, ct);
            await RunPowerCfgAsync(new[] { "/setdcvalueindex", "SCHEME_CURRENT", ProcessorSubgroup, CoreParkingMaxCores, DisabledCoreParkingValue.ToString(CultureInfo.InvariantCulture) }, ct);
            await RunPowerCfgAsync(new[] { "/setactive", "SCHEME_CURRENT" }, ct);

            return new TweakResult(TweakStatus.Applied, "Disabled CPU core parking for the active power plan.", DateTimeOffset.UtcNow);
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
            var isApplied = snapshot.MinAc == DisabledCoreParkingValue
                && snapshot.MinDc == DisabledCoreParkingValue
                && snapshot.MaxAc == DisabledCoreParkingValue
                && snapshot.MaxDc == DisabledCoreParkingValue;

            return isApplied
                ? new TweakResult(TweakStatus.Verified, "Core parking is disabled for the active power plan.", DateTimeOffset.UtcNow)
                : new TweakResult(TweakStatus.Failed, $"Verification failed. Current values: Min {snapshot.MinAc}/{snapshot.MinDc}, Max {snapshot.MaxAc}/{snapshot.MaxDc}.", DateTimeOffset.UtcNow);
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
            await RunPowerCfgAsync(new[] { "/setacvalueindex", "SCHEME_CURRENT", ProcessorSubgroup, CoreParkingMinCores, _snapshot.MinAc.ToString(CultureInfo.InvariantCulture) }, ct);
            await RunPowerCfgAsync(new[] { "/setdcvalueindex", "SCHEME_CURRENT", ProcessorSubgroup, CoreParkingMinCores, _snapshot.MinDc.ToString(CultureInfo.InvariantCulture) }, ct);
            await RunPowerCfgAsync(new[] { "/setacvalueindex", "SCHEME_CURRENT", ProcessorSubgroup, CoreParkingMaxCores, _snapshot.MaxAc.ToString(CultureInfo.InvariantCulture) }, ct);
            await RunPowerCfgAsync(new[] { "/setdcvalueindex", "SCHEME_CURRENT", ProcessorSubgroup, CoreParkingMaxCores, _snapshot.MaxDc.ToString(CultureInfo.InvariantCulture) }, ct);
            await RunPowerCfgAsync(new[] { "/setactive", "SCHEME_CURRENT" }, ct);

            return new TweakResult(TweakStatus.RolledBack, "Restored previous CPU core parking values for the active power plan.", DateTimeOffset.UtcNow);
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

    private async Task<CpuCoreParkingSnapshot> ReadSnapshotAsync(CancellationToken ct)
    {
        var minOutput = await RunPowerCfgAsync(new[] { "/qh", "SCHEME_CURRENT", ProcessorSubgroup, CoreParkingMinCores }, ct);
        var maxOutput = await RunPowerCfgAsync(new[] { "/qh", "SCHEME_CURRENT", ProcessorSubgroup, CoreParkingMaxCores }, ct);

        return new CpuCoreParkingSnapshot(
            ParseIndexedValue(minOutput.StandardOutput, CurrentAcRegex, "AC minimum core parking"),
            ParseIndexedValue(minOutput.StandardOutput, CurrentDcRegex, "DC minimum core parking"),
            ParseIndexedValue(maxOutput.StandardOutput, CurrentAcRegex, "AC maximum core parking"),
            ParseIndexedValue(maxOutput.StandardOutput, CurrentDcRegex, "DC maximum core parking"));
    }

    private async Task<CommandResult> RunPowerCfgAsync(string[] args, CancellationToken ct)
    {
        var executable = global::System.IO.Path.Combine(Environment.SystemDirectory, System32PowerCfgExe);
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

    private sealed record CpuCoreParkingSnapshot(int MinAc, int MinDc, int MaxAc, int MaxDc);
}
