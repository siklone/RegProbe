using System.Collections.Generic;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;

namespace RegProbe.Engine.Tweaks.Misc;

public static class DisableOfficeTelemetryTweak
{
    public static RegistryValueBatchTweak CreateDisableOfficeTelemetryTweak(IRegistryAccessor registryAccessor)
    {
        var entries = new List<RegistryValueBatchEntry>();

        // Office telemetry agent
        var basePath = @"Software\Policies\Microsoft\Office\16.0\OSM";

        entries.Add(new RegistryValueBatchEntry(RegistryHive.CurrentUser, basePath, "EnableLogging", RegistryValueKind.DWord, 0, RegistryView.Default));

        entries.Add(new RegistryValueBatchEntry(RegistryHive.CurrentUser, basePath, "EnableUpload", RegistryValueKind.DWord, 0, RegistryView.Default));

        entries.Add(new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Office\16.0\Common", "QMEnable", RegistryValueKind.DWord, 0, RegistryView.Default));

        entries.Add(new RegistryValueBatchEntry(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Office\16.0\Common\Feedback", "Enabled", RegistryValueKind.DWord, 0, RegistryView.Default));

        // Prevent telemetry for specific Office applications
        var applications = new Dictionary<string, string>
        {
            { "accesssolution", "Access" },
            { "olksolution", "Outlook" },
            { "onenotesolution", "OneNote" },
            { "pptsolution", "PowerPoint" },
            { "projectsolution", "Project" },
            { "publishersolution", "Publisher" },
            { "visiosolution", "Visio" },
            { "wdsolution", "Word" },
            { "xlsolution", "Excel" }
        };

        foreach (var app in applications)
        {
            entries.Add(new RegistryValueBatchEntry(RegistryHive.CurrentUser, $@"{basePath}\preventedapplications", app.Key, RegistryValueKind.DWord, 1, RegistryView.Default)); // 1 = Prevent reporting
        }

        // Prevent telemetry for solution types
        var solutionTypes = new[] { "agave", "appaddins", "comaddins", "documentfiles", "templatefiles" };
        foreach (var solutionType in solutionTypes)
        {
            entries.Add(new RegistryValueBatchEntry(RegistryHive.CurrentUser, $@"{basePath}\preventedsolutiontypes", solutionType, RegistryValueKind.DWord, 1, RegistryView.Default)); // 1 = Prevent reporting
        }

        return new RegistryValueBatchTweak(
            id: "misc.disable-office-telemetry",
            name: "Disable Microsoft Office Telemetry",
            description: "Disables Office telemetry logging, data collection, CEIP opt-in, feedback collection, and telemetry agent tasks for all Office applications.",
            risk: TweakRiskLevel.Safe,
            entries: entries,
            registryAccessor: registryAccessor,
            requiresElevation: false);
    }
}
