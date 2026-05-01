using System;
using System.Collections.Generic;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Engine;
using RegProbe.Engine.Tweaks;
using RegProbe.Engine.Tweaks.Commands.Cleanup;

namespace RegProbe.Application.Services.TweakProviders;

public sealed class SystemTweakProvider : BaseTweakProvider
{
    public override string CategoryName => "System";

    public override IEnumerable<ITweak> CreateTweaks(TweakExecutionPipeline pipeline, TweakContext context, bool isElevated)
    {
        ITweak CreateDisableServiceTweak(
            string id,
            string name,
            string description,
            params string[] serviceNames)
            => CreateServiceStartModeBatchTweak(
                context,
                id,
                name,
                description,
                TweakRiskLevel.Risky,
                serviceNames,
                ServiceStartMode.Disabled);

        // Core System Behavior
        // Appearance & Explorer
        // Mass Disablements
        yield return CreateScheduledTaskBatchTweak(
            context,
            "system.disable-scheduled-tasks",
            "Disable Telemetry & Maintenance Tasks",
            "Disables dozens of scheduled tasks related to telemetry, data collection, and non-essential maintenance.",
            TweakRiskLevel.Risky,
            new[]
            {
                @"\Microsoft\Windows\Application Experience\MareBackup",
                @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
                @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
                @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
                @"\Microsoft\Windows\DiskCleanup\SilentCleanup",
                @"\Microsoft\Windows\Feedback\Siuf\DmClient",
                @"\Microsoft\Windows\Windows Error Reporting\QueueReporting"
            });

        yield return CreateDisableServiceTweak(
            "system.services.disable-connected-user-experiences",
            "Disable Connected User Experiences Service",
            "Disables the Connected User Experiences and Telemetry service (DiagTrack).",
            "DiagTrack");

        yield return CreateDisableServiceTweak(
            "system.services.disable-wap-push-routing",
            "Disable WAP Push Routing Service",
            "Disables the WAP Push Message Routing service.",
            "dmwappushservice");

        yield return CreateDisableServiceTweak(
            "system.services.disable-sysmain",
            "Disable SysMain Service",
            "Disables the SysMain (Superfetch) service.",
            "SysMain");

        yield return CreateDisableServiceTweak(
            "system.services.disable-windows-search",
            "Disable Windows Search Service",
            "Disables the Windows Search indexing service.",
            "WSearch");

        yield return CreateDisableServiceTweak(
            "system.services.disable-windows-error-reporting",
            "Disable Windows Error Reporting Service",
            "Disables the Windows Error Reporting service.",
            "WerSvc");

        yield return CreateDisableServiceTweak(
            "system.services.disable-print-spooler",
            "Disable Print Spooler Service",
            "Disables the Print Spooler service.",
            "Spooler");

        yield return CreateDisableServiceTweak(
            "system.services.disable-print-notifications",
            "Disable Print Notification Service",
            "Disables the printer notification service.",
            "PrintNotify");

        yield return CreateDisableServiceTweak(
            "system.services.disable-print-workflow-user-service",
            "Disable Print Workflow User Service",
            "Disables per-user print workflow services.",
            "PrintWorkflowUserSvc_*");

        yield return CreateDisableServiceTweak(
            "system.services.disable-print-device-configuration",
            "Disable Print Device Configuration Service",
            "Disables the printer device configuration service.",
            "PrintDeviceConfigurationService");

        yield return CreateDisableServiceTweak(
            "system.services.disable-print-scan-broker",
            "Disable Print Scan Broker Service",
            "Disables the print and scan broker service.",
            "PrintScanBrokerService");

        yield return CreateDisableServiceTweak(
            "system.services.disable-bluetooth-support",
            "Disable Bluetooth Support Service",
            "Disables the main Bluetooth support service.",
            "bthserv");

        yield return CreateDisableServiceTweak(
            "system.services.disable-bluetooth-user-service",
            "Disable Bluetooth User Service",
            "Disables per-user Bluetooth services.",
            "BluetoothUserService_*");

        yield return CreateDisableServiceTweak(
            "system.services.disable-bluetooth-audio-gateway",
            "Disable Bluetooth Audio Gateway Service",
            "Disables the Bluetooth Audio Gateway service.",
            "BTAGService");

        yield return CreateRegistryTweak(
            context,
            "system.disable-storage-sense",
            "Disable Storage Sense",
            "Turns off Storage Sense automatic cleanup.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\StorageSense",
            "AllowStorageSenseGlobal",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "system.disable-storage-sense-temp-cleanup",
            "Disable Storage Sense Temporary Files Cleanup",
            "Prevents Storage Sense from deleting temporary files.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"Software\Policies\Microsoft\Windows\StorageSense",
            "AllowStorageSenseTemporaryFilesCleanup",
            RegistryValueKind.DWord,
            0);

        yield return CreateRegistryTweak(
            context,
            "system.disable-service-splitting",
            "Disable Service Splitting",
            "Prevents services from being split into separate svchost processes.",
            TweakRiskLevel.Advanced,
            RegistryHive.LocalMachine,
            @"System\CurrentControlSet\Control",
            "SvcHostSplitThresholdInKB",
            RegistryValueKind.DWord,
            -1);

        // Command-based System Tweaks
        yield return new ClearEventLogsTweak(context.ElevatedCommandRunner, "System");
    }
}
