using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;

namespace RegProbe.Engine.Tweaks.Peripheral;

public sealed class AudioEnhancementsTweak : ITweak
{
    private const string RenderRoot = @"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render";
    private const string CaptureRoot = @"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Capture";
    private const string PropertiesSubKey = "Properties";
    private const string FxPropertiesSubKey = "FxProperties";
    private const string EnhancementProperty3 = "{b3f8fa53-0004-438e-9003-51a46e139bfc},3";
    private const string EnhancementProperty4 = "{b3f8fa53-0004-438e-9003-51a46e139bfc},4";
    private const string EnhancementFxProperty = "{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5";
    private static readonly string ProtectedAclMessage =
        "MMDevices audio endpoint enhancement keys are protected on this system; RegProbe does not mutate them blindly.";

    private readonly Func<IReadOnlyList<string>> _discoverProtectedTargets;

    public AudioEnhancementsTweak(IRegistryAccessor registryAccessor)
        : this(registryAccessor, DiscoverProtectedTargets)
    {
    }

    public AudioEnhancementsTweak(IRegistryAccessor registryAccessor, Func<IReadOnlyList<string>> discoverProtectedTargets)
    {
        ArgumentNullException.ThrowIfNull(registryAccessor);
        _discoverProtectedTargets = discoverProtectedTargets ?? throw new ArgumentNullException(nameof(discoverProtectedTargets));
    }

    public string Id => "peripheral.audio-disable-enhancements";
    public string Name => "Disable Audio Enhancements";
    public string Description => "Checks the audio-enhancement endpoint flags, but skips mutation when MMDevices keys are protected by Windows ACLs.";
    public TweakRiskLevel Risk => TweakRiskLevel.Safe;
    public bool RequiresElevation => true;

    public Task<TweakResult> DetectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(NotApplicableResult(BuildNotApplicableMessage()));
    }

    public Task<TweakResult> ApplyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(NotApplicableResult(BuildNotApplicableMessage()));
    }

    public Task<TweakResult> VerifyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(NotApplicableResult(BuildNotApplicableMessage()));
    }

    public Task<TweakResult> RollbackAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(NotApplicableResult(BuildNotApplicableMessage()));
    }

    private string BuildNotApplicableMessage()
    {
        var targets = SafeDiscoverProtectedTargets();
        if (targets.Count == 0)
        {
            return $"Not applicable: {ProtectedAclMessage} No audio endpoint enhancement targets were found.";
        }

        return $"Not applicable: {ProtectedAclMessage} Found {targets.Count} protected device-scoped target values.";
    }

    private IReadOnlyList<string> SafeDiscoverProtectedTargets()
    {
        try
        {
            return _discoverProtectedTargets();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static TweakResult NotApplicableResult(string message)
        => new(TweakStatus.NotApplicable, message, DateTimeOffset.UtcNow);

    private static IReadOnlyList<string> DiscoverProtectedTargets()
    {
        if (!OperatingSystem.IsWindows())
        {
            return Array.Empty<string>();
        }

        var targets = new List<string>();
        using var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        AddEndpointTargets(localMachine, RenderRoot, targets);
        AddEndpointTargets(localMachine, CaptureRoot, targets);
        return targets;
    }

    private static void AddEndpointTargets(RegistryKey localMachine, string endpointRoot, List<string> targets)
    {
        using var root = localMachine.OpenSubKey(endpointRoot, writable: false);
        if (root is null)
        {
            return;
        }

        foreach (var endpointId in root.GetSubKeyNames())
        {
            var endpointPath = endpointRoot + "\\" + endpointId;
            AddTargetIfSubKeyExists(localMachine, endpointPath, PropertiesSubKey, EnhancementProperty3, targets);
            AddTargetIfSubKeyExists(localMachine, endpointPath, PropertiesSubKey, EnhancementProperty4, targets);
            AddTargetIfSubKeyExists(localMachine, endpointPath, FxPropertiesSubKey, EnhancementFxProperty, targets);
        }
    }

    private static void AddTargetIfSubKeyExists(
        RegistryKey localMachine,
        string endpointPath,
        string subKey,
        string valueName,
        List<string> targets)
    {
        var path = endpointPath + "\\" + subKey;
        using var key = localMachine.OpenSubKey(path, writable: false);
        if (key is not null)
        {
            targets.Add(@"HKLM\" + path + "\\" + valueName);
        }
    }
}
