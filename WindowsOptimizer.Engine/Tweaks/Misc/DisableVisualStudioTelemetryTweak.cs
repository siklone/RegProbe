using System.Collections.Generic;
using Microsoft.Win32;
using WindowsOptimizer.Core;
using WindowsOptimizer.Infrastructure.Registry;

namespace WindowsOptimizer.Engine.Tweaks.Misc;

public static class DisableVisualStudioTelemetryTweak
{
    public static RegistryValueBatchTweak CreateDisableVisualStudioTelemetryTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>();

        // VS Telemetry
        entries.Add(new RegistryValueBatchEntry(
            RegistryHive.LocalMachine,
            RegistryView.Default,
            @"SOFTWARE\Policies\Microsoft\VisualStudio\SQM",
            "OptIn",
            RegistryValueKind.DWord,
            0));

        // Feedback settings
        var feedbackPolicies = new Dictionary<string, int>
        {
            { "DisableEmailInput", 1 },
            { "DisableFeedbackDialog", 1 },
            { "DisableScreenshotCapture", 1 }
        };

        foreach (var policy in feedbackPolicies)
        {
            entries.Add(new RegistryValueBatchEntry(
                RegistryHive.LocalMachine,
                RegistryView.Default,
                @"SOFTWARE\Policies\Microsoft\VisualStudio\Feedback",
                policy.Key,
                RegistryValueKind.DWord,
                policy.Value));
        }

        // SQM opt-in for different VS versions
        var versions = new[] { "14.0", "15.0", "16.0", "17.0" }; // VS 2015, 2017, 2019, 2022
        foreach (var version in versions)
        {
            entries.Add(new RegistryValueBatchEntry(
                RegistryHive.LocalMachine,
                RegistryView.Default,
                $@"SOFTWARE\Microsoft\VSCommon\{version}\SQM",
                "OptIn",
                RegistryValueKind.DWord,
                0));
        }

        return new RegistryValueBatchTweak(
            id: "misc-disable-visual-studio-telemetry",
            name: "Disable Visual Studio Telemetry",
            description: "Disables Visual Studio telemetry, SQM data collection, IntelliCode remote analysis, and feedback features for VS 2015-2022.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: true);
    }
}
