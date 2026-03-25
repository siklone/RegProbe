using System.Collections.Generic;
using Microsoft.Win32;
using OpenTraceProject.Core;
using OpenTraceProject.Core.Registry;
using OpenTraceProject.Core.Services;
using OpenTraceProject.Engine;
using OpenTraceProject.Engine.Tweaks;
using OpenTraceProject.Engine.Tweaks.Commands.Cleanup;
using OpenTraceProject.Engine.Tweaks.Misc;

namespace OpenTraceProject.App.Services.TweakProviders;

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
