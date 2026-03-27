# Kernel/Power 96-Key Routing Note

Date: 2026-03-27

Scope: user-supplied `reg add` batch for kernel/power/system values.

Parsed count: `96` commands. The pasted batch does not contain `101` `reg add` lines.

## Why this note exists

We should run this lane together, but only one agent should own the live VM, reboot, ETW, and snapshot workflow.

- `research/README.md:108` starts the repo's current validation model from a single exact path/value/baseline.
- `scripts/vm/guest-validation-agent.ps1:240` through `scripts/vm/guest-validation-agent.ps1:241` and `scripts/vm/guest-validation-agent.ps1:502` through `scripts/vm/guest-validation-agent.ps1:504` show the current guest agent still assumes one `registry_path`, one `value_name`, and one `candidate_value`.
- `research/vm-incidents.json:3` through `research/vm-incidents.json:7` currently report `18` incidents with `17` snapshot reverts.
- `research/records/power.disable-cpu-idle-states.json:7` and `research/records/power.disable-cpu-idle-states.json:15` show the raw power bundle is still destabilizing runtime/benchmark lanes on the current baseline.

Recommended ownership split:

- Main agent: VM owner. Snapshot restore, boot tests, ETW/WPR/Procmon, runtime incident handling, and any guest-side scripts.
- Secondary agent: host-side routing, existence manifest prep, docs/static triage, and special-case filtering before values enter the heavy VM queue.

## Route summary

| Route | Count | Meaning |
| --- | ---: | --- |
| `existing-covered` | 8 | Already tied to an app surface or current research record. |
| `research-only` | 5 | Repo already has a lead or historical record, but not a clean current app surface. |
| `docs-first-new-candidate` | 63 | No current app mapping, but repo docs already mention the path/value family. |
| `net-new` | 20 | No trustworthy app/record/docs hit in the current source pass. |

## Existing-covered

These should not go into the first heavy VM queue unless we are explicitly re-auditing them.

- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\HiberbootEnabled`
  Existing tweak: `power.disable-fast-startup` in `app/Services/TweakProviders/PowerTweakProvider.cs:26`.
  Current record: `research/records/power.disable-fast-startup.review.json:7`.

- `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling\PowerThrottlingOff`
  Existing tweak: `power.disable-power-throttling` in `app/Services/TweakProviders/PowerTweakProvider.cs:38`.
  Current record: `research/records/power.disable-power-throttling.json:16`.

- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel\SerializeTimerExpiration`
  Existing tweak: `system.kernel-serialize-timer-expiration` in `app/Services/TweakProviders/SystemRegistryTweakProvider.cs:93`.
  Current record: `research/records/system.kernel-serialize-timer-expiration.review.json:7`.
  Caveat: the repo still flags missing primary Microsoft proof in `research/records/system.kernel-serialize-timer-expiration.review.json:29`.

- `HKLM\SYSTEM\CurrentControlSet\Control\Power\CoalescingTimerInterval`
- `HKLM\SYSTEM\CurrentControlSet\Control\Power\DeepIoCoalescingEnabled`
- `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnergyEstimationEnabled`
- `HKLM\SYSTEM\CurrentControlSet\Control\Power\EventProcessorEnabled`
  Existing bundle: `power.optimize-performance` in `app/Services/TweakProviders/PowerTweakProvider.cs:50`.
  Current record: `research/records/power.optimize-performance.review.json:7`.
  Caveat: that record already treats the bundle as a weak/non-canonical control surface.

- `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableIdleStatesAtBoot`
  Existing bundle: `power.disable-cpu-idle-states` in `app/Services/TweakProviders/PowerTweakProvider.cs:68`.
  Current record: `research/records/power.disable-cpu-idle-states.json:7`.

## Research-only

These already have some repo context, so they should go through a lighter pre-pass before any deep VM work.

- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM\EnableVirtualization`
  Treat this as a boot/UAC lead only.
  Use `research/records/security.uac-never-notify.json:324` as the current meaningful lead.
  Do not confuse it with `EnableVirtualizationBasedSecurity` in `app/Services/TweakProviders/SecurityTweakProvider.cs:181`.

- `HKLM\SYSTEM\CurrentControlSet\Control\Power\HiberFileSizePercent`
  Historical repo lead exists in `research/records/power.hide-hibernate-option.json:16`.

- `HKLM\SYSTEM\CurrentControlSet\Control\Power\MSDisabled`
- `HKLM\SYSTEM\CurrentControlSet\Control\Power\ModernSleep\EnabledActions`
  Historical repo lead exists in `research/records/power.disable-modern-standby.json:16`.

- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\SleepStudyDisabled`
  Current privacy lead exists in `research/records/privacy.disable-sleep-study-diagnostics.review.json:168`.

## Docs-first new candidates

These have repo-doc leads and should start with `existence-first` plus the lightest proof that matches the doc quality.

- `HKLM\SYSTEM\CurrentControlSet\Control\Power`
  First-hit doc cluster: `Docs/power/power.md:96` through `Docs/power/power.md:221`.
  Values:
  `AllowAudioToEnableExecutionRequiredPowerRequests`, `AllowSystemRequiredPowerRequests`, `AlwaysComputeQosHints`, `Class1InitialUnparkCount`, `CoalescingFlushInterval`, `DisableDisplayBurstOnPowerSourceChange`, `DisableInboxPepGeneratedConstraints`, `DisableVsyncLatencyUpdate`, `DripsSwHwDivergenceEnableLiveDump`, `EnableInputSuppression`, `EnableMinimalHiberFile`, `EnforceAusterityMode`, `FxAccountingTelemetryDisabled`, `HeteroHgsEePerfHintsIndependentEnabled`, `HeteroHgsPlusDisabled`, `HeteroMultiClassParkingEnabled`, `HeteroMultiCoreClassesEnabled`, `HiberbootEnabled`, `HibernateEnabled`, `HibernateEnabledDefault`, `IdleProcessorsRequireQosManagement`, `IgnoreCsComplianceCheck`, `IpiLastClockOwnerDisable`, `LidReliabilityState`, `MaximumFrequencyOverride`, `MfBufferingThreshold`, `PerfBoostAtGuaranteed`, `PerfCalculateActualUtilization`, `PerfCheckTimerImplementation`, `PoFxSystemIrpWaitForReportDevicePowered`, `PowerWatchdogDrvSetMonitorTimeoutMsec`, `PowerWatchdogDwmSyncFlushTimeoutMsec`, `PowerWatchdogPoCalloutTimeoutMsec`, `PowerWatchdogPowerOnGdiTimeoutMsec`, `PowerWatchdogRequestQueueTimeoutMsec`, `SleepstudyAccountingEnabled`, `StandbyConnectivityGracePeriod`, `TimerRebaseThresholdOnDripsExit`, `TtmEnabled`, `Win32kCalloutWatchdogTimeoutSeconds`.

- `HKLM\SYSTEM\CurrentControlSet\Control\Power\ModernSleep\EnableDsNetRefresh`
  First-hit doc: `Docs/power/power.md:245`.

- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel`
  First-hit doc cluster: `Docs/system/system.md:75` through `Docs/system/system.md:179`.
  Values:
  `AlwaysTrackIoBoosting`, `DisableControlFlowGuardExportSuppression`, `DisableExceptionChainValidation`, `DisableLightWeightSuspend`, `EnablePerCpuClockTickScheduling`, `EnableTickAccumulationFromAccountingPeriods`, `ForceBugcheckForDpcWatchdog`, `ForceForegroundBoostDecay`, `ForceIdleGracePeriod`, `ForceParkingRequested`, `GlobalTimerResolutionRequests`, `HyperStartDisabled`, `InterruptSteeringFlags`, `LongDpcQueueThreshold`, `LongDpcRuntimeThreshold`, `MaxDynamicTickDuration`, `MaximumCooperativeIdleSearchWidth`, `RebalanceMinPriority`, `TimerCheckFlags`, `XStateContextLookasidePerProcMaxDepth`.

- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Kernel\EnableWerUserReporting`
  First-hit doc is privacy-side, not kernel-side: `Docs/privacy/privacy.md:2777`.

- `HKLM\SYSTEM\CurrentControlSet\Control\Power\ForceHibernateDisabled\Policy`
  Keep this in docs-first, but do not trust token-only search because `Policy` is too generic.

## Net-new

These had no trustworthy provider, record, or doc hit in the current source pass.

- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM`
  `EnableLocalLogonSid`

- `HKLM\SYSTEM\CurrentControlSet\Control\Power`
  `CustomizeDuringSetup`, `SourceSettingsVersion`

- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive`
  `AdditionalCriticalWorkerThreads`, `AdditionalDelayedWorkerThreads`, `ForceEnableMutantAutoboost`, `KernelWorkerTestFlags`, `MaximumKernelWorkerThreads`, `TickcountRolloverDelay`, `UuidSequenceNumber`

- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System`
  `AllowRemoteDASD`, `DisableDiskCounters`, `IoAllowLoadCrashDumpDriver`, `IoEnableSessionZeroAccessCheck`

- `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power`
  `IdleScanInterval`, `PowerSettingProfile`, `SkipTickOverride`, `WatchdogResumeTimeout`, `WatchdogSleepTimeout`, `Win32CalloutWatchdogBugcheckEnabled`

## Special cases that need manual handling

- `HKLM\SYSTEM\CurrentControlSet\Control\Power\HiberbootEnabled`
  Do not route this as existing Fast Startup coverage.
  The authoritative current local Fast Startup path is `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\HiberbootEnabled`.
  The stale `Control\Power` path divergence is explicitly called out in `research/records/power.disable-fast-startup.review.json:7`, `:16`, and `:62`.

- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM\EnableVirtualization`
  Do not route this as VBS coverage.
  String matches against `EnableVirtualizationBasedSecurity` are false positives for this batch.

- `HKLM\SYSTEM\CurrentControlSet\Control\Power\ForceHibernateDisabled\Policy`
  Do not route generic `Policy` hits as coverage.
  This value needs exact-path existence proof before any stronger conclusion.

## Practical execution order

1. Main agent builds an `existence-first` manifest for all `96` values from a clean VM baseline.
2. Exclude `existing-covered` and `research-only` from the first heavy VM queue unless a re-audit is explicitly wanted.
3. Run `net-new` first, grouped by subsystem.
4. Run `docs-first-new-candidate` second, also grouped by subsystem, and only escalate to deep static work when the lighter proof is not enough.
5. Keep one VM owner throughout. The current guest agent is still single-value oriented, so batch execution needs a wrapper rather than direct reuse of the current config shape.
