using System;
using System.Collections.Generic;
using System.Linq;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Services;
using WindowsOptimizer.Engine.Tweaks;
using Microsoft.Win32;

namespace WindowsOptimizer.App.Services.TweakProviders;

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
    public abstract string CategoryName { get; }

    public abstract IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated);

    protected RegistryValueTweak CreateRegistryTweak(
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
        var effectiveRequiresElevation = requiresElevation ?? hive != RegistryHive.CurrentUser;
        var accessor = effectiveRequiresElevation ? context.ElevatedRegistry : context.LocalRegistry;

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
            requiresElevation);
    }

    protected RegistryValueSetTweak CreateRegistryValueSetTweak(
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
        var effectiveRequiresElevation = requiresElevation ?? hive != RegistryHive.CurrentUser;
        var accessor = effectiveRequiresElevation ? context.ElevatedRegistry : context.LocalRegistry;

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
            requiresElevation);
    }

    protected RegistryValueBatchTweak CreateRegistryValueBatchTweak(
        TweakContext context,
        string id,
        string name,
        string description,
        TweakRiskLevel risk,
        IReadOnlyList<RegistryValueBatchEntry> entries,
        bool? requiresElevation = null)
    {
        if (entries is null) throw new ArgumentNullException(nameof(entries));

        var effectiveRequiresElevation = requiresElevation ?? entries.Any(entry => entry.Hive != RegistryHive.CurrentUser);
        var accessor = effectiveRequiresElevation ? context.ElevatedRegistry : context.LocalRegistry;

        return new RegistryValueBatchTweak(
            id,
            name,
            description,
            risk,
            entries,
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
}
