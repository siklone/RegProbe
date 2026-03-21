using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Commands;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Power;

public sealed class DisableUsbSelectiveSuspendTweak : ITweak
{
    private const string System32PowerCfgExe = "powercfg.exe";
    private const string UsbSubgroupGuid = "2a737441-1930-4402-8d77-b2bebba308a3";
    private const string UsbSelectiveSuspendGuid = "48e6b7a6-50f5-4782-a5d4-53bb8f07e226";
    private const int DisabledValue = 0;

    private static readonly Regex CurrentAcRegex = new(@"Current AC Power Setting Index:\s*0x(?<value>[0-9A-Fa-f]+)", RegexOptions.Compiled);
    private static readonly Regex CurrentDcRegex = new(@"Current DC Power Setting Index:\s*0x(?<value>[0-9A-Fa-f]+)", RegexOptions.Compiled);

    private readonly ICommandRunner _commandRunner;
    private UsbSelectiveSuspendSnapshot? _snapshot;

    public DisableUsbSelectiveSuspendTweak(ICommandRunner commandRunner)
    {
        _commandRunner = commandRunner ?? throw new ArgumentNullException(nameof(commandRunner));
    }

    public string Id => "power.disable-usb-selective-suspend";
    public string Name => "Disable USB Selective Suspend";
    public string Description => "Disables USB Selective Suspend to prevent USB devices from powering down unexpectedly. This can resolve issues with USB devices disconnecting or becoming unresponsive.";
    public TweakRiskLevel Risk => TweakRiskLevel.Safe;
    public bool RequiresElevation => true;

    public async Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var snapshot = await ReadSnapshotAsync(ct);
            _snapshot = snapshot;

            var isApplied = snapshot.Ac == DisabledValue && snapshot.Dc == DisabledValue;
            var state = isApplied ? "Disabled" : "Enabled";
            var message = string.Create(
                CultureInfo.InvariantCulture,
                $"USB Selective Suspend: {state} (AC/DC: {snapshot.Ac}/{snapshot.Dc}).");

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
            await RunPowerCfgAsync(new[] { "/setacvalueindex", "SCHEME_CURRENT", UsbSubgroupGuid, UsbSelectiveSuspendGuid, DisabledValue.ToString(CultureInfo.InvariantCulture) }, ct);
            await RunPowerCfgAsync(new[] { "/setdcvalueindex", "SCHEME_CURRENT", UsbSubgroupGuid, UsbSelectiveSuspendGuid, DisabledValue.ToString(CultureInfo.InvariantCulture) }, ct);
            await RunPowerCfgAsync(new[] { "/setactive", "SCHEME_CURRENT" }, ct);

            return new TweakResult(
                TweakStatus.Applied,
                "Disabled USB Selective Suspend for the active power plan.",
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
            var isApplied = snapshot.Ac == DisabledValue && snapshot.Dc == DisabledValue;

            return isApplied
                ? new TweakResult(TweakStatus.Verified, "USB Selective Suspend is disabled for AC and DC power.", DateTimeOffset.UtcNow)
                : new TweakResult(TweakStatus.Failed, $"Verification failed. Current AC/DC values: {snapshot.Ac}/{snapshot.Dc}.", DateTimeOffset.UtcNow);
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
            await RunPowerCfgAsync(new[] { "/setacvalueindex", "SCHEME_CURRENT", UsbSubgroupGuid, UsbSelectiveSuspendGuid, _snapshot.Ac.ToString(CultureInfo.InvariantCulture) }, ct);
            await RunPowerCfgAsync(new[] { "/setdcvalueindex", "SCHEME_CURRENT", UsbSubgroupGuid, UsbSelectiveSuspendGuid, _snapshot.Dc.ToString(CultureInfo.InvariantCulture) }, ct);
            await RunPowerCfgAsync(new[] { "/setactive", "SCHEME_CURRENT" }, ct);

            return new TweakResult(
                TweakStatus.RolledBack,
                "Restored previous USB Selective Suspend values for the active power plan.",
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

    private async Task<UsbSelectiveSuspendSnapshot> ReadSnapshotAsync(CancellationToken ct)
    {
        var result = await RunPowerCfgAsync(new[] { "/query", "SCHEME_CURRENT", UsbSubgroupGuid, UsbSelectiveSuspendGuid }, ct);

        return new UsbSelectiveSuspendSnapshot(
            ParseIndexedValue(result.StandardOutput, CurrentAcRegex, "AC USB selective suspend"),
            ParseIndexedValue(result.StandardOutput, CurrentDcRegex, "DC USB selective suspend"));
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

    private sealed record UsbSelectiveSuspendSnapshot(int Ac, int Dc);
}
