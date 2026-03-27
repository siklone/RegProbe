using System;
using System.Collections.Generic;
using System.Linq;
using RegProbe.Core;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Engine;
using RegProbe.Engine.Services;
using RegProbe.Engine.Tweaks;
using RegProbe.Engine.Tweaks.Commands.RegistryOps;
using Microsoft.Win32;

namespace RegProbe.App.Services.TweakProviders;

/// <summary>
/// Wrapper that adds documentation metadata to any ITweak.
/// </summary>
public sealed class DocumentedTweak : ITweak, ITweakWithDocumentation
{
    private readonly ITweak _innerTweak;
    
    public DocumentedTweak(ITweak innerTweak, TweakDocumentation documentation)
    {
        _innerTweak = innerTweak ?? throw new ArgumentNullException(nameof(innerTweak));
        Documentation = documentation ?? throw new ArgumentNullException(nameof(documentation));
    }
    
    public string Id => _innerTweak.Id;
    public string Name => _innerTweak.Name;
    public string Description => _innerTweak.Description;
    public TweakRiskLevel Risk => _innerTweak.Risk;
    public bool RequiresElevation => _innerTweak.RequiresElevation;
    public TweakDocumentation Documentation { get; }
    
    public System.Threading.Tasks.Task<TweakResult> DetectAsync(System.Threading.CancellationToken ct) => _innerTweak.DetectAsync(ct);
    public System.Threading.Tasks.Task<TweakResult> ApplyAsync(System.Threading.CancellationToken ct) => _innerTweak.ApplyAsync(ct);
    public System.Threading.Tasks.Task<TweakResult> VerifyAsync(System.Threading.CancellationToken ct) => _innerTweak.VerifyAsync(ct);
    public System.Threading.Tasks.Task<TweakResult> RollbackAsync(System.Threading.CancellationToken ct) => _innerTweak.RollbackAsync(ct);
}

public abstract class BaseTweakProvider : ITweakProvider
{
    private const string PoliciesPrefix = @"Software\Policies\";
    private const string LegacyPoliciesPrefix = @"Software\Microsoft\Windows\CurrentVersion\Policies\";

    public abstract string CategoryName { get; }

    public abstract IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated);

    protected ITweak CreateRegistryTweak(
        TweakContext context,
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        RegistryHive hive,
        string keyPath,
        string valueName,
        RegistryValueKind valueKind,
        object targetValue,
        RegistryView view = RegistryView.Default,
        bool? requiresElevation = null)
    {
        var effectiveRequiresElevation = ResolveRequiresElevation(hive, keyPath, requiresElevation);
        if (ShouldUseCommandBackedRegistryExecution(hive, keyPath, effectiveRequiresElevation))
        {
            return CreateCommandBackedRegistryTweak(
                context,
                id,
                name,
                description,
                risk,
                hive,
                keyPath,
                valueName,
                valueKind,
                targetValue,
                view,
                effectiveRequiresElevation);
        }

        var accessor = context.LocalRegistry;

        return new RegistryValueTweak(
            id,
            name,
            description,
            risk,
            hive,
            keyPath,
            valueName,
            valueKind,
            targetValue,
            accessor,
            view,
            effectiveRequiresElevation);
    }

    protected ITweak CreateRegistryValueSetTweak(
        TweakContext context,
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        RegistryHive hive,
        string keyPath,
        IReadOnlyList<RegistryValueSetEntry> entries,
        RegistryView view = RegistryView.Default,
        bool? requiresElevation = null)
    {
        var effectiveRequiresElevation = ResolveRequiresElevation(hive, keyPath, requiresElevation);
        if (ShouldUseCommandBackedRegistryExecution(hive, keyPath, effectiveRequiresElevation))
        {
            var batchEntries = entries
                .Select(entry => new RegistryValueBatchEntry(hive, keyPath, entry.Name, entry.Kind, entry.TargetValue, view))
                .ToList();

            return new RegistryCommandBatchTweak(
                id,
                name,
                description,
                risk,
                batchEntries,
                context.LocalRegistry,
                context.ElevatedRegistry,
                effectiveRequiresElevation);
        }

        var accessor = context.LocalRegistry;

        return new RegistryValueSetTweak(
            id,
            name,
            description,
            risk,
            hive,
            keyPath,
            entries,
            accessor,
            view,
            effectiveRequiresElevation);
    }

    protected ITweak CreateRegistryValueBatchTweak(
        TweakContext context,
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        IReadOnlyList<RegistryValueBatchEntry> entries,
        bool? requiresElevation = null)
    {
        if (entries is null) throw new ArgumentNullException(nameof(entries));

        var effectiveRequiresElevation = ResolveRequiresElevation(entries, requiresElevation);
        if (entries.Any(entry => ShouldUseCommandBackedRegistryExecution(entry.Hive, entry.KeyPath, effectiveRequiresElevation)))
        {
            return new RegistryCommandBatchTweak(
                id,
                name,
                description,
                risk,
                entries,
                context.LocalRegistry,
                context.ElevatedRegistry,
                effectiveRequiresElevation);
        }

        var accessor = context.LocalRegistry;

        return new RegistryValueBatchTweak(
            id,
            name,
            description,
            risk,
            entries,
            accessor,
            effectiveRequiresElevation);
    }

    protected RegistryCommandBatchTweak CreateCommandBackedRegistryTweak(
        TweakContext context,
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        RegistryHive hive,
        string keyPath,
        string valueName,
        RegistryValueKind valueKind,
        object targetValue,
        RegistryView view = RegistryView.Default,
        bool? requiresElevation = null)
    {
        return new RegistryCommandBatchTweak(
            id,
            name,
            description,
            risk,
            new[]
            {
                new RegistryValueBatchEntry(hive, keyPath, valueName, valueKind, targetValue, view)
            },
            context.LocalRegistry,
            context.ElevatedRegistry,
            requiresElevation);
    }

    protected RegistryCommandBatchTweak CreateCommandBackedRegistryValueBatchTweak(
        TweakContext context,
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        IReadOnlyList<RegistryValueBatchEntry> entries,
        bool? requiresElevation = null)
    {
        if (entries is null) throw new ArgumentNullException(nameof(entries));

        return new RegistryCommandBatchTweak(
            id,
            name,
            description,
            risk,
            entries,
            context.LocalRegistry,
            context.ElevatedRegistry,
            requiresElevation);
    }

    protected RegistryValuePresetBatchTweak CreateRegistryValuePresetBatchTweak(
        TweakContext context,
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        IReadOnlyList<RegistryValuePresetBatchOption> presets,
        string defaultPresetKey,
        bool? requiresElevation = null)
    {
        if (presets is null) throw new ArgumentNullException(nameof(presets));

        var accessor = context.LocalRegistry;

        return new RegistryValuePresetBatchTweak(
            id,
            name,
            description,
            risk,
            presets,
            defaultPresetKey,
            accessor,
            requiresElevation);
    }

    protected CompositeTweak CreateCompositeTweak(
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        IReadOnlyList<ITweak> tweaks)
    {
        return new CompositeTweak(id, name, description, risk, tweaks);
    }

    protected ServiceStartModeBatchTweak CreateServiceStartModeBatchTweak(
        TweakContext context,
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        IReadOnlyList<string> serviceNames,
        ServiceStartMode targetStartMode,
        bool stopRunning = true,
        bool? requiresElevation = null)
    {
        if (serviceNames is null) throw new ArgumentNullException(nameof(serviceNames));

        var entries = serviceNames.Select(serviceName => new ServiceStartModeEntry(serviceName, targetStartMode)).ToList();
        return new ServiceStartModeBatchTweak(
            id,
            name,
            description,
            risk,
            entries,
            context.ElevatedServiceManager,
            stopRunning,
            requiresElevation);
    }

    protected ScheduledTaskBatchTweak CreateScheduledTaskBatchTweak(
        TweakContext context,
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        IReadOnlyList<string> taskPaths,
        bool? requiresElevation = null)
    {
        if (taskPaths is null) throw new ArgumentNullException(nameof(taskPaths));

        return new ScheduledTaskBatchTweak(
            id,
            name,
            description,
            risk,
            taskPaths,
            context.ElevatedTaskManager,
            requiresElevation);
    }

    protected FileRenameTweak CreateFileRenameTweak(
        TweakContext context,
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        string sourcePath,
        string disabledPath,
        bool? requiresElevation = null)
    {
        return new FileRenameTweak(
            id,
            name,
            description,
            risk,
            sourcePath,
            disabledPath,
            context.ElevatedFileSystem,
            requiresElevation);
    }
    
    /// <summary>
    /// Wraps a tweak with documentation metadata.
    /// </summary>
    protected DocumentedTweak WithDocumentation(ITweak tweak, TweakDocumentation documentation)
        => new DocumentedTweak(tweak, documentation);
    
    /// <summary>
    /// Wraps a tweak with nohuto documentation.
    /// </summary>
    /// <param name="tweak">The tweak to wrap</param>
    /// <param name="category">Category folder name (e.g., "kernel", "graphics")</param>
    /// <param name="anchor">Optional anchor in the markdown file</param>
    protected DocumentedTweak WithNohutoDoc(ITweak tweak, string category, string anchor = "")
        => new DocumentedTweak(tweak, TweakDocumentation.FromNohuto(category, anchor));
    
    /// <summary>
    /// Wraps a tweak with Microsoft documentation.
    /// </summary>
    /// <param name="tweak">The tweak to wrap</param>
    /// <param name="url">Full URL to Microsoft Learn documentation</param>
    protected DocumentedTweak WithMicrosoftDoc(ITweak tweak, string url)
        => new DocumentedTweak(tweak, TweakDocumentation.FromMicrosoft(url));

    private static bool IsPolicyLikeRegistryPath(string keyPath)
    {
        if (string.IsNullOrWhiteSpace(keyPath))
        {
            return false;
        }

        var normalized = keyPath.TrimStart('\\');
        return normalized.StartsWith(PoliciesPrefix, StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith(LegacyPoliciesPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ResolveRequiresElevation(RegistryHive hive, string keyPath, bool? requiresElevation)
    {
        return requiresElevation ?? hive != RegistryHive.CurrentUser;
    }

    private static bool ResolveRequiresElevation(IReadOnlyList<RegistryValueBatchEntry> entries, bool? requiresElevation)
    {
        return requiresElevation ?? entries.Any(entry => entry.Hive != RegistryHive.CurrentUser);
    }

    private static bool ShouldUseCommandBackedRegistryExecution(RegistryHive hive, string keyPath, bool requiresElevation)
    {
        return requiresElevation || hive != RegistryHive.CurrentUser;
    }
}
