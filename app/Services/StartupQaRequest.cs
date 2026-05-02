using System;
using System.IO;

namespace RegProbe.App.Services;

internal sealed record StartupQaRequest(
    string TweakId,
    string OutputPath,
    bool RollbackAfterApply,
    bool ShutdownWhenDone)
{
    public static StartupQaRequest? TryParse(string[] args)
    {
        if (args is null || args.Length == 0)
        {
            return null;
        }

        string? tweakId = null;
        string? outputPath = null;
        var rollbackAfterApply = true;
        var shutdownWhenDone = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals("--qa-run-tweak", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                {
                    tweakId = args[++i];
                }

                continue;
            }

            if (arg.Equals("--qa-output", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                {
                    outputPath = args[++i];
                }

                continue;
            }

            if (arg.Equals("--qa-skip-rollback", StringComparison.OrdinalIgnoreCase))
            {
                rollbackAfterApply = false;
                continue;
            }

            if (arg.Equals("--qa-shutdown", StringComparison.OrdinalIgnoreCase))
            {
                shutdownWhenDone = true;
            }
        }

        if (string.IsNullOrWhiteSpace(tweakId))
        {
            return null;
        }

        outputPath = string.IsNullOrWhiteSpace(outputPath)
            ? Path.Combine(Path.GetTempPath(), "RegProbe_QaResult.json")
            : outputPath;

        return new StartupQaRequest(
            tweakId.Trim(),
            Path.GetFullPath(outputPath),
            rollbackAfterApply,
            shutdownWhenDone);
    }
}
