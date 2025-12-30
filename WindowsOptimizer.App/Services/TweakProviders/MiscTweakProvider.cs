using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Engine.Tweaks.Misc;

namespace WindowsOptimizer.App.Services.TweakProviders;

public sealed class MiscTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Tools & Cleanup";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // Maintenance & Tools
        yield return SetNotepadPlusPlusDefaultEditorTweak.CreateSetNotepadPlusPlusDefaultEditorTweak(context.ElevatedRegistry);
        yield return SevenZipSettingsTweak.CreateOptimize7ZipSettingsTweak(context.LocalRegistry);

        // Command-based Cleanup
        yield return new CleanupComponentStoreTweak(context.ElevatedCommandRunner);
        yield return new DisableReservedStorageTweak(context.ElevatedCommandRunner);
        yield return new ClearRecycleBinTweak(context.ElevatedCommandRunner);
        yield return new ClearShadowCopiesTweak(context.ElevatedCommandRunner);

        // File-based Cleanup
        yield return new ClearTemporaryFilesTweak();
        yield return new ClearDirectXShaderCacheTweak();
        yield return new ClearThumbnailCacheTweak();
        yield return new ClearWindowsUpdateCacheTweak();
        yield return new ClearWERFilesTweak();
        yield return new ClearPrefetchFilesTweak();
        yield return new ClearFontCacheTweak();
        yield return new ClearWindowsOldTweak();
        yield return new ClearMemoryDumpFilesTweak();

        // Misc specialized
        yield return new RemoveProductKeyTweak(context.ElevatedCommandRunner);

        // Search
        yield return CreateRegistryTweak(
            context,
            "misc.disable-search-web-results",
            "Disable Search Web Results",
            "Prevents Windows Search from showing Bing web results in the Start menu.",
            TweakRiskLevel.Advanced,
            RegistryHive.CurrentUser,
            @"Software\Policies\Microsoft\Windows\Explorer",
            "DisableSearchBoxSuggestions",
            RegistryValueKind.DWord,
            1,
            requiresElevation: false);
    }
}
