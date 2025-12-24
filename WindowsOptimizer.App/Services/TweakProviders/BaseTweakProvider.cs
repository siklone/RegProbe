using System;
using System.Collections.Generic;
using System.Linq;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using Microsoft.Win32;

namespace WindowsOptimizer.App.Services.TweakProviders;

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
}
