# Operator Reg Add Inventory - 2026-05-08

- Status: **ok**
- Generated UTC: `2026-05-08T19:52:27Z`
- Mode: non-mutating repo + VM read inventory; no registry writes were applied.
- Parsed commands: `96/96`
- Repo exact target matches: `40`
- Repo text-only hits: `5`
- Repo no-hit values: `51`
- Live VM values present: `26/96`
- Live VM requested values already matching: `10`
- Live VM value missing under existing key: `69`
- Live VM key missing: `1`

## Important Guardrail

A live value being present only proves existence on the current VM. It does not prove that applying the pasted value is safe, documented, or product-eligible. Kernel, watchdog, timer, worker-thread, hibernation, and mitigation-bypass values must stay behind the research/evidence decision gates.

## Live Values Present

| # | Value | Current | Requested | Match | Repo exact records | Flags |
|---|---|---:|---:|---|---|---|
| 2 | `EnableVirtualization` | `1` | `0` | `false` | policy.system.enable-virtualization |  |
| 3 | `AdditionalCriticalWorkerThreads` | `0` | `5` | `false` | system.executive-additional-worker-threads | executive-worker-thread low-level profiling-required |
| 4 | `AdditionalDelayedWorkerThreads` | `0` | `5` | `false` | system.executive-additional-worker-threads | executive-worker-thread low-level profiling-required |
| 5 | `UuidSequenceNumber` | `2928467` | `3322358` | `false` | system.executive-uuid-sequence-number |  |
| 10 | `AllowRemoteDASD` | `0` | `0` | `true` | system.io-allow-remote-dasd | deprecated-path-collision removable-storage-policy-is-active-surface |
| 36 | `HiberbootEnabled` | `1` | `0` | `false` | power.disable-fast-startup |  |
| 37 | `PowerSettingProfile` | `0` | `0` | `true` | text-only |  |
| 38 | `WatchdogResumeTimeout` | `120` | `0` | `false` | power.session-watchdog-timeouts | watchdog/bugcheck-sensitive |
| 39 | `WatchdogSleepTimeout` | `300` | `0` | `false` | power.session-watchdog-timeouts | watchdog/bugcheck-sensitive |
| 44 | `Class1InitialUnparkCount` | `64` | `64` | `true` | power.control.class1-initial-unpark-count |  |
| 45 | `CustomizeDuringSetup` | `1` | `1` | `true` | no repo hit |  |
| 46 | `EnergyEstimationEnabled` | `1` | `0` | `false` | power.optimize-performance |  |
| 47 | `HiberFileSizePercent` | `0` | `0` | `true` | power.control.hiber-file-size-percent |  |
| 48 | `MfBufferingThreshold` | `0` | `0` | `true` | power.control.mf-buffering-threshold |  |
| 49 | `PerfCalculateActualUtilization` | `1` | `0` | `false` | power.control.perf-calculate-actual-utilization |  |
| 50 | `SourceSettingsVersion` | `4` | `4` | `true` | no repo hit |  |
| 51 | `TimerRebaseThresholdOnDripsExit` | `60` | `60` | `true` | power.control.timer-rebase-threshold-on-drips-exit | kernel-timing-sensitive |
| 52 | `HibernateEnabledDefault` | `1` | `0` | `false` | power.control.hibernate-enabled-default |  |
| 53 | `EventProcessorEnabled` | `1` | `0` | `false` | power.optimize-performance |  |
| 54 | `LidReliabilityState` | `1` | `1` | `true` | power.control.lid-reliability-state |  |
| 55 | `HibernateEnabled` | `0` | `0` | `true` | power.control.hibernate-enabled-default, power.control.hibernate-enabled |  |
| 58 | `IdleProcessorsRequireQosManagement` | `1` | `0` | `false` | no repo hit |  |
| 60 | `AllowAudioToEnableExecutionRequiredPowerRequests` | `1` | `0` | `false` | power.control.allow-audio-to-enable-execution-required-power-requests |  |
| 70 | `AlwaysComputeQosHints` | `1` | `0` | `false` | no repo hit |  |
| 82 | `AllowSystemRequiredPowerRequests` | `1` | `0` | `false` | power.control.allow-system-required-power-requests |  |
| 83 | `CoalescingFlushInterval` | `1` | `0` | `false` | no repo hit |  |

## Repo Exact Target Matches

| # | Value | Requested | Live status | Record ids | Flags |
|---|---|---:|---|---|---|
| 2 | `EnableVirtualization` | `0` | `value-present` | policy.system.enable-virtualization |  |
| 3 | `AdditionalCriticalWorkerThreads` | `5` | `value-present` | system.executive-additional-worker-threads | executive-worker-thread low-level profiling-required |
| 4 | `AdditionalDelayedWorkerThreads` | `5` | `value-present` | system.executive-additional-worker-threads | executive-worker-thread low-level profiling-required |
| 5 | `UuidSequenceNumber` | `3322358` | `value-present` | system.executive-uuid-sequence-number |  |
| 10 | `AllowRemoteDASD` | `0` | `value-present` | system.io-allow-remote-dasd | deprecated-path-collision removable-storage-policy-is-active-surface |
| 14 | `GlobalTimerResolutionRequests` | `1` | `value-missing` | system.kernel.global-timer-resolution-requests | kernel-timing-sensitive |
| 19 | `TimerCheckFlags` | `0` | `value-missing` | system.kernel.timer-check-flags | kernel-timing-sensitive |
| 21 | `DisableExceptionChainValidation` | `1` | `value-missing` | system.kernel.disable-exception-chain-validation | security-hold mitigation-bypass never-default-apply |
| 25 | `SerializeTimerExpiration` | `0` | `value-missing` | system.kernel-serialize-timer-expiration | kernel-timing-sensitive |
| 27 | `LongDpcQueueThreshold` | `2` | `value-missing` | system.kernel-long-dpc-threshold-cluster | kernel-timing-sensitive |
| 28 | `LongDpcRuntimeThreshold` | `50` | `value-missing` | system.kernel-long-dpc-threshold-cluster | kernel-timing-sensitive |
| 29 | `ForceBugcheckForDpcWatchdog` | `0` | `value-missing` | system.kernel.force-bugcheck-for-dpc-watchdog | watchdog/bugcheck-sensitive, kernel-timing-sensitive |
| 36 | `HiberbootEnabled` | `0` | `value-present` | power.disable-fast-startup |  |
| 38 | `WatchdogResumeTimeout` | `0` | `value-present` | power.session-watchdog-timeouts | watchdog/bugcheck-sensitive |
| 39 | `WatchdogSleepTimeout` | `0` | `value-present` | power.session-watchdog-timeouts | watchdog/bugcheck-sensitive |
| 41 | `Win32CalloutWatchdogBugcheckEnabled` | `0` | `value-missing` | power.session-win32-callout-watchdog-bugcheck-enabled | watchdog/bugcheck-sensitive |
| 44 | `Class1InitialUnparkCount` | `64` | `value-present` | power.control.class1-initial-unpark-count |  |
| 46 | `EnergyEstimationEnabled` | `0` | `value-present` | power.optimize-performance |  |
| 47 | `HiberFileSizePercent` | `0` | `value-present` | power.control.hiber-file-size-percent |  |
| 48 | `MfBufferingThreshold` | `0` | `value-present` | power.control.mf-buffering-threshold |  |
| 49 | `PerfCalculateActualUtilization` | `0` | `value-present` | power.control.perf-calculate-actual-utilization |  |
| 51 | `TimerRebaseThresholdOnDripsExit` | `60` | `value-present` | power.control.timer-rebase-threshold-on-drips-exit | kernel-timing-sensitive |
| 52 | `HibernateEnabledDefault` | `0` | `value-present` | power.control.hibernate-enabled-default |  |
| 53 | `EventProcessorEnabled` | `0` | `value-present` | power.optimize-performance |  |
| 54 | `LidReliabilityState` | `1` | `value-present` | power.control.lid-reliability-state |  |
| 55 | `HibernateEnabled` | `0` | `value-present` | power.control.hibernate-enabled-default, power.control.hibernate-enabled |  |
| 59 | `TtmEnabled` | `0` | `value-missing` | power.control.ttm-enabled |  |
| 60 | `AllowAudioToEnableExecutionRequiredPowerRequests` | `0` | `value-present` | power.control.allow-audio-to-enable-execution-required-power-requests |  |
| 61 | `DeepIoCoalescingEnabled` | `0` | `value-missing` | power.optimize-performance |  |
| 73 | `DisableIdleStatesAtBoot` | `2` | `value-missing` | power.disable-cpu-idle-states |  |
| 75 | `MSDisabled` | `1` | `value-missing` | power.disable-modern-standby |  |
| 77 | `Win32kCalloutWatchdogTimeoutSeconds` | `0` | `value-missing` | power.control.win32k-callout-watchdog-timeout-seconds | watchdog/bugcheck-sensitive |
| 82 | `AllowSystemRequiredPowerRequests` | `0` | `value-present` | power.control.allow-system-required-power-requests |  |
| 84 | `CoalescingTimerInterval` | `0` | `value-missing` | power.optimize-performance | kernel-timing-sensitive |
| 88 | `PowerWatchdogRequestQueueTimeoutMsec` | `0` | `value-missing` | power.control.power-watchdog-timeout-cluster | watchdog/bugcheck-sensitive |
| 89 | `PowerWatchdogPoCalloutTimeoutMsec` | `0` | `value-missing` | power.control.power-watchdog-timeout-cluster | watchdog/bugcheck-sensitive |
| 90 | `PowerWatchdogPowerOnGdiTimeoutMsec` | `0` | `value-missing` | power.control.power-watchdog-timeout-cluster | watchdog/bugcheck-sensitive |
| 91 | `PowerWatchdogDwmSyncFlushTimeoutMsec` | `0` | `value-missing` | power.control.power-watchdog-timeout-cluster | watchdog/bugcheck-sensitive |
| 92 | `PowerWatchdogDrvSetMonitorTimeoutMsec` | `0` | `value-missing` | power.control.power-watchdog-timeout-cluster | watchdog/bugcheck-sensitive |
| 96 | `PowerThrottlingOff` | `1` | `value-missing` | power.disable-power-throttling |  |

## Repo Text-Only Hits

| # | Value | Live status | Text hit record ids |
|---|---|---|---|
| 37 | `PowerSettingProfile` | `value-present` | power.session-watchdog-timeouts |
| 43 | `SleepStudyDisabled` | `value-missing` | privacy.disable-sleep-study-diagnostics |
| 79 | `HiberbootEnabled` | `value-missing` | power.control.ttm-enabled, power.disable-fast-startup |
| 93 | `Policy` | `key-missing` | audio.show-disconnected-devices, audio.show-hidden-devices, cleanup.memory-dumps, cleanup.prefetch-files, cleanup.shadow-copies, cleanup.temp-files, cleanup.wer-files, cleanup.windows-old |
| 95 | `EnabledActions` | `value-missing` | power.disable-modern-standby |

## Repo No-Hit Values

- `EnableLocalLogonSid` at `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM`: value-missing, requested `0`
- `TickcountRolloverDelay` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive`: value-missing, requested `0`
- `KernelWorkerTestFlags` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive`: value-missing, requested `0`
- `MaximumKernelWorkerThreads` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive`: value-missing, requested `25000`
- `ForceEnableMutantAutoboost` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive`: value-missing, requested `1`
- `DisableDiskCounters` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System`: value-missing, requested `1`
- `IoAllowLoadCrashDumpDriver` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System`: value-missing, requested `0`
- `IoEnableSessionZeroAccessCheck` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System`: value-missing, requested `1`
- `ForceParkingRequested` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel`: value-missing, requested `0`
- `EnableWerUserReporting` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel`: value-missing, requested `0`
- `HyperStartDisabled` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel`: value-missing, requested `1`
- `DisableLightWeightSuspend` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel`: value-missing, requested `0`
- `ForceIdleGracePeriod` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel`: value-missing, requested `0`
- `MaxDynamicTickDuration` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel`: value-missing, requested `1`
- `EnableTickAccumulationFromAccountingPeriods` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel`: value-missing, requested `1`
- `EnablePerCpuClockTickScheduling` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel`: value-missing, requested `1`
- `XStateContextLookasidePerProcMaxDepth` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel`: value-missing, requested `1024`
- `ForceForegroundBoostDecay` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel`: value-missing, requested `0`
- `RebalanceMinPriority` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel`: value-missing, requested `16`
- `InterruptSteeringFlags` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel`: value-missing, requested `1`
- `AlwaysTrackIoBoosting` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel`: value-missing, requested `0`
- `DisableControlFlowGuardExportSuppression` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel`: value-missing, requested `1`
- `MaximumCooperativeIdleSearchWidth` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel`: value-missing, requested `0`
- `SkipTickOverride` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power`: value-missing, requested `0`
- `IdleScanInterval` at `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power`: value-missing, requested `0`
- `CustomizeDuringSetup` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-present, current `1`, requested `1`
- `SourceSettingsVersion` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-present, current `4`, requested `4`
- `DisableInboxPepGeneratedConstraints` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `1`
- `DisableDisplayBurstOnPowerSourceChange` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `1`
- `IdleProcessorsRequireQosManagement` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-present, current `1`, requested `0`
- `IgnoreCsComplianceCheck` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `1`
- `DripsSwHwDivergenceEnableLiveDump` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `0`
- `DisableVsyncLatencyUpdate` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `1`
- `SleepstudyAccountingEnabled` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `0`
- `EnableInputSuppression` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `1`
- `PerfCheckTimerImplementation` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `0`
- `StandbyConnectivityGracePeriod` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `0`
- `EnforceAusterityMode` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `0`
- `AlwaysComputeQosHints` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-present, current `1`, requested `0`
- `HeteroMultiCoreClassesEnabled` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `0`
- `HeteroMultiClassParkingEnabled` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `0`
- `PerfBoostAtGuaranteed` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `1`
- `FxAccountingTelemetryDisabled` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `1`
- `EnableMinimalHiberFile` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `0`
- `MaximumFrequencyOverride` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `100`
- `PoFxSystemIrpWaitForReportDevicePowered` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `0`
- `CoalescingFlushInterval` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-present, current `1`, requested `0`
- `HeteroHgsEePerfHintsIndependentEnabled` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `1`
- `HeteroHgsPlusDisabled` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `1`
- `IpiLastClockOwnerDisable` at `HKLM\SYSTEM\CurrentControlSet\Control\Power`: value-missing, requested `1`
- `EnableDsNetRefresh` at `HKLM\SYSTEM\CurrentControlSet\Control\Power\ModernSleep`: value-missing, requested `0`
