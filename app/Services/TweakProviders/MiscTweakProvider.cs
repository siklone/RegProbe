using System.Collections.Generic;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Engine;
using RegProbe.Engine.Tweaks;
using RegProbe.Engine.Tweaks.Commands.Cleanup;
using RegProbe.Engine.Tweaks.Misc;

namespace RegProbe.App.Services.TweakProviders;

public sealed class MiscTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "Tools & Cleanup";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        // Maintenance & Tools
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

    }
}
