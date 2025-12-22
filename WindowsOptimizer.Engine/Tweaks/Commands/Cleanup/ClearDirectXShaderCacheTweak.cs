using System;
using System.Collections.Generic;
using System.IO;
using WindowsOptimizer.Core;

namespace WindowsOptimizer.Engine.Tweaks.Commands.Cleanup;

public sealed class ClearDirectXShaderCacheTweak : FileCleanupTweak
{
    public ClearDirectXShaderCacheTweak()
        : base(
            id: "cleanup-directx-shader-cache",
            name: "Clear DirectX Shader Cache",
            description: "Clears DirectX and vendor shader caches (NVIDIA, AMD, Intel). Shaders will be recompiled on next app launch.",
            risk: TweakRiskLevel.Safe,
            requiresElevation: false)
    {
    }

    protected override IEnumerable<string> GetPathsToClean()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // DirectX shader cache
        yield return Path.Combine(localAppData, "D3DSCache");

        // NVIDIA caches
        yield return Path.Combine(localAppData, "NVIDIA", "DXCache");
        yield return Path.Combine(localAppData, "NVIDIA", "GLCache");
        yield return Path.Combine(localAppData, "NVIDIA Corporation", "NV_Cache");

        // AMD cache
        yield return Path.Combine(localAppData, "AMD", "DXCache");

        // Intel cache
        yield return Path.Combine(localAppData, "Intel", "DXCache");
    }
}
