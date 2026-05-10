# Operator96 Enriched Value Matrix

- Generated UTC: `2026-05-10T16:47:56Z`
- Campaign: `operator96-enriched-values-20260510`
- Records: `96`
- Candidate values: `209`
- App-card eligible records: `85`

## Reference Catalog

- `microsoft-powercfg`: Powercfg command-line options (https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options)
- `microsoft-cfg`: Control Flow Guard for platform security (https://learn.microsoft.com/en-us/windows/win32/secbp/control-flow-guard)
- `microsoft-fast-startup`: Distinguishing fast startup from wake-from-hibernation (https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/distinguishing-fast-startup-from-wake-from-hibernation)
- `sysinternals-procmon`: Process Monitor (https://learn.microsoft.com/en-us/sysinternals/downloads/procmon)
- `local-vm-defaults`: Win11 25H2 VM observed defaults
- `runtime-evidence`: ETW/Procmon/Ghidra/operator96 VM artifacts
- `reactos-static`: ReactOS/static string hints (https://github.com/reactos/reactos)

## Records

| # | Value | Default | Rules | Candidates | App gate | Notes |
|---:|---|---|---|---|---|---|
| 1 | `EnableLocalLogonSid` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 2 | `EnableVirtualization` | `known-present` | `boolean-toggle` | `0`:vm-validated, `1`:local-default | `eligible=True` none | evidence-lanes-open-or-covered |
| 3 | `AdditionalCriticalWorkerThreads` | `known-present` | `count-boundary` | `1`:vm-validated, `5`:vm-validated, `0`:local-default | `eligible=False` `kernel-worker-thread-override` | evidence-lanes-open-or-covered |
| 4 | `AdditionalDelayedWorkerThreads` | `known-present` | `count-boundary`, `threshold-boundary` | `1`:vm-validated, `5`:vm-validated, `0`:local-default | `eligible=False` `kernel-worker-thread-override` | evidence-lanes-open-or-covered |
| 5 | `UuidSequenceNumber` | `known-present` | - | `0`:vm-validated, `3322358`:vm-validated, `2636877`:local-default | `eligible=False` `safety-finding-present` | evidence-lanes-open-or-covered |
| 6 | `TickcountRolloverDelay` | `known-absent` | `count-boundary`, `threshold-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 7 | `KernelWorkerTestFlags` | `known-absent` | - | `0`:vm-validated, `1`:vm-validated | `eligible=False` `kernel-worker-thread-override` | evidence-lanes-open-or-covered |
| 8 | `MaximumKernelWorkerThreads` | `known-absent` | `count-boundary` | `0`:vm-validated, `25000`:vm-validated, `1`:name-rule | `eligible=False` `kernel-worker-thread-override` | evidence-lanes-open-or-covered |
| 9 | `ForceEnableMutantAutoboost` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 10 | `AllowRemoteDASD` | `known-present` | - | `1`:vm-validated, `0`:local-default | `eligible=True` none | evidence-lanes-open-or-covered |
| 11 | `DisableDiskCounters` | `known-absent` | `boolean-toggle`, `count-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 12 | `IoAllowLoadCrashDumpDriver` | `known-absent` | - | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 13 | `IoEnableSessionZeroAccessCheck` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 14 | `GlobalTimerResolutionRequests` | `known-absent` | - | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 15 | `ForceParkingRequested` | `known-absent` | - | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 16 | `EnableWerUserReporting` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 17 | `HyperStartDisabled` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 18 | `DisableLightWeightSuspend` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 19 | `TimerCheckFlags` | `known-absent` | - | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 20 | `ForceIdleGracePeriod` | `known-absent` | `threshold-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 21 | `DisableExceptionChainValidation` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=False` `security-mitigation-override` | evidence-lanes-open-or-covered |
| 22 | `MaxDynamicTickDuration` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 23 | `EnableTickAccumulationFromAccountingPeriods` | `known-absent` | `boolean-toggle`, `count-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 24 | `EnablePerCpuClockTickScheduling` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 25 | `SerializeTimerExpiration` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 26 | `XStateContextLookasidePerProcMaxDepth` | `known-absent` | `threshold-boundary` | `0`:vm-validated, `1024`:vm-validated, `1`:name-rule | `eligible=True` none | evidence-lanes-open-or-covered |
| 27 | `LongDpcQueueThreshold` | `known-absent` | `threshold-boundary` | `0`:vm-validated, `2`:vm-validated, `1`:name-rule | `eligible=True` none | evidence-lanes-open-or-covered |
| 28 | `LongDpcRuntimeThreshold` | `known-absent` | `threshold-boundary` | `0`:vm-validated, `50`:vm-validated, `1`:name-rule | `eligible=True` none | evidence-lanes-open-or-covered |
| 29 | `ForceBugcheckForDpcWatchdog` | `known-absent` | `timeout-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 30 | `ForceForegroundBoostDecay` | `known-absent` | - | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 31 | `RebalanceMinPriority` | `known-absent` | - | `0`:vm-validated, `16`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 32 | `InterruptSteeringFlags` | `known-absent` | - | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 33 | `AlwaysTrackIoBoosting` | `known-absent` | - | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 34 | `DisableControlFlowGuardExportSuppression` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=False` `security-mitigation-override` | evidence-lanes-open-or-covered |
| 35 | `MaximumCooperativeIdleSearchWidth` | `known-absent` | `threshold-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 36 | `HiberbootEnabled` | `known-present` | `boolean-toggle` | `0`:vm-validated, `1`:local-default | `eligible=True` none | evidence-lanes-open-or-covered |
| 37 | `PowerSettingProfile` | `known-present` | - | `1`:vm-validated, `0`:local-default | `eligible=True` none | evidence-lanes-open-or-covered |
| 38 | `WatchdogResumeTimeout` | `known-present` | `timeout-boundary` | `0`:vm-validated, `1`:vm-validated, `120`:local-default | `eligible=True` none | evidence-lanes-open-or-covered |
| 39 | `WatchdogSleepTimeout` | `known-present` | `timeout-boundary` | `0`:vm-validated, `1`:vm-validated, `300`:local-default | `eligible=True` none | evidence-lanes-open-or-covered |
| 40 | `SkipTickOverride` | `known-absent` | - | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 41 | `Win32CalloutWatchdogBugcheckEnabled` | `known-absent` | `boolean-toggle`, `timeout-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 42 | `IdleScanInterval` | `known-absent` | `threshold-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 43 | `SleepStudyDisabled` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 44 | `Class1InitialUnparkCount` | `known-present` | `count-boundary` | `0`:vm-validated, `1`:vm-validated, `64`:local-default | `eligible=True` none | evidence-lanes-open-or-covered |
| 45 | `CustomizeDuringSetup` | `known-present` | - | `0`:vm-validated, `1`:local-default | `eligible=True` none | evidence-lanes-open-or-covered |
| 46 | `EnergyEstimationEnabled` | `known-present` | `boolean-toggle` | `0`:vm-validated, `1`:local-default | `eligible=True` none | evidence-lanes-open-or-covered |
| 47 | `HiberFileSizePercent` | `known-present` | `percent-range` | `1`:vm-validated, `0`:local-default, `50`:name-rule, `100`:name-rule | `eligible=True` none | evidence-lanes-open-or-covered |
| 48 | `MfBufferingThreshold` | `known-present` | `threshold-boundary` | `1`:vm-validated, `0`:local-default | `eligible=True` none | evidence-lanes-open-or-covered |
| 49 | `PerfCalculateActualUtilization` | `known-present` | `boolean-toggle` | `0`:vm-validated, `1`:local-default | `eligible=True` none | evidence-lanes-open-or-covered |
| 50 | `SourceSettingsVersion` | `known-present` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated, `4`:local-default | `eligible=True` none | evidence-lanes-open-or-covered |
| 51 | `TimerRebaseThresholdOnDripsExit` | `known-present` | `threshold-boundary` | `0`:vm-validated, `1`:vm-validated, `60`:local-default | `eligible=True` none | evidence-lanes-open-or-covered |
| 52 | `HibernateEnabledDefault` | `known-present` | `boolean-toggle` | `0`:vm-validated, `1`:local-default | `eligible=True` none | evidence-lanes-open-or-covered |
| 53 | `EventProcessorEnabled` | `known-present` | `boolean-toggle` | `0`:vm-validated, `1`:local-default | `eligible=True` none | evidence-lanes-open-or-covered |
| 54 | `LidReliabilityState` | `known-present` | - | `0`:vm-validated, `1`:local-default | `eligible=True` none | evidence-lanes-open-or-covered |
| 55 | `HibernateEnabled` | `known-present` | `boolean-toggle` | `1`:vm-validated, `0`:local-default | `eligible=True` none | evidence-lanes-open-or-covered |
| 56 | `DisableInboxPepGeneratedConstraints` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 57 | `DisableDisplayBurstOnPowerSourceChange` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 58 | `IdleProcessorsRequireQosManagement` | `known-absent` | - | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 59 | `TtmEnabled` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=False` `safety-finding-present` | evidence-lanes-open-or-covered |
| 60 | `AllowAudioToEnableExecutionRequiredPowerRequests` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 61 | `DeepIoCoalescingEnabled` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 62 | `IgnoreCsComplianceCheck` | `known-absent` | - | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 63 | `DripsSwHwDivergenceEnableLiveDump` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 64 | `DisableVsyncLatencyUpdate` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 65 | `SleepstudyAccountingEnabled` | `known-absent` | `boolean-toggle`, `count-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=False` `safety-finding-present` | evidence-lanes-open-or-covered |
| 66 | `EnableInputSuppression` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 67 | `PerfCheckTimerImplementation` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 68 | `StandbyConnectivityGracePeriod` | `known-absent` | `threshold-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 69 | `EnforceAusterityMode` | `known-absent` | - | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 70 | `AlwaysComputeQosHints` | `known-absent` | - | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 71 | `HeteroMultiCoreClassesEnabled` | `known-absent` | `boolean-toggle`, `count-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 72 | `HeteroMultiClassParkingEnabled` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 73 | `DisableIdleStatesAtBoot` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `2`:vm-validated, `1`:name-rule | `eligible=True` none | evidence-lanes-open-or-covered |
| 74 | `PerfBoostAtGuaranteed` | `known-absent` | - | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 75 | `MSDisabled` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 76 | `FxAccountingTelemetryDisabled` | `known-absent` | `boolean-toggle`, `count-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 77 | `Win32kCalloutWatchdogTimeoutSeconds` | `known-absent` | `timeout-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 78 | `EnableMinimalHiberFile` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 79 | `HiberbootEnabled` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 80 | `MaximumFrequencyOverride` | `known-absent` | `percent-range` | `0`:vm-validated, `100`:vm-validated, `1`:name-rule, `50`:name-rule | `eligible=True` none | evidence-lanes-open-or-covered |
| 81 | `PoFxSystemIrpWaitForReportDevicePowered` | `known-absent` | - | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 82 | `AllowSystemRequiredPowerRequests` | `known-absent` | - | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 83 | `CoalescingFlushInterval` | `known-absent` | `threshold-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 84 | `CoalescingTimerInterval` | `known-absent` | `threshold-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 85 | `HeteroHgsEePerfHintsIndependentEnabled` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 86 | `HeteroHgsPlusDisabled` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 87 | `IpiLastClockOwnerDisable` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 88 | `PowerWatchdogRequestQueueTimeoutMsec` | `known-absent` | `timeout-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 89 | `PowerWatchdogPoCalloutTimeoutMsec` | `known-absent` | `timeout-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 90 | `PowerWatchdogPowerOnGdiTimeoutMsec` | `known-absent` | `timeout-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 91 | `PowerWatchdogDwmSyncFlushTimeoutMsec` | `known-absent` | `timeout-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 92 | `PowerWatchdogDrvSetMonitorTimeoutMsec` | `known-absent` | `timeout-boundary` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 93 | `Policy` | `known-absent` | - | `0`:vm-validated, `1`:vm-validated | `eligible=False` `key-missing-in-target-vm` | no-evidence-found-on-win11-25h2 |
| 94 | `EnableDsNetRefresh` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 95 | `EnabledActions` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=True` none | evidence-lanes-open-or-covered |
| 96 | `PowerThrottlingOff` | `known-absent` | `boolean-toggle` | `0`:vm-validated, `1`:vm-validated | `eligible=False` `key-missing-in-target-vm` | evidence-lanes-open-or-covered |
