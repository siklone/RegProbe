using System;

namespace RegProbe.App.Services;

internal sealed record StartupNavigationRequest(
    string? OpenTweakId,
    bool ExpandPlanDrawer)
{
    public static StartupNavigationRequest? TryParse(string[] args)
    {
        if (args is null || args.Length == 0)
        {
            return null;
        }

        string? openTweakId = null;
        var expandPlanDrawer = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals("--open-tweak", StringComparison.OrdinalIgnoreCase)
                || arg.Equals("--qa-open-tweak", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                {
                    openTweakId = args[++i];
                }

                continue;
            }

            if (arg.Equals("--expand-plan", StringComparison.OrdinalIgnoreCase)
                || arg.Equals("--qa-expand-plan", StringComparison.OrdinalIgnoreCase))
            {
                expandPlanDrawer = true;
            }
        }

        if (string.IsNullOrWhiteSpace(openTweakId) && !expandPlanDrawer)
        {
            return null;
        }

        return new StartupNavigationRequest(
            string.IsNullOrWhiteSpace(openTweakId) ? null : openTweakId.Trim(),
            expandPlanDrawer);
    }
}
