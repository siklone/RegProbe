# Operator96 App Surface Review

- Generated UTC: `2026-05-14T00:44:04Z`
- Matrix: `registry-research-framework/audit/operator96-enriched-value-matrix-20260510.json`
- Aggregate: `registry-research-framework/audit/operator96-low-noise-rerun-aggregate-20260512.json`
- Records: `96`
- Ready for bounded app card: `0`
- Needs low-noise rerun: `0`
- Not app-surface ready: `79`
- Blocked by gate: `17`
- Blocked by safety: `0`
- Aggregate surface blocked: `False`

## Policy

- Only ready_for_bounded_app_card may enter the app surface without another VM campaign.
- Low-confidence, harmful, noisy, or host-noise-unknown experiments are observations only.
- needs_low_noise_rerun means host noise was not clean; not_app_surface_ready means the run was clean enough to store but not positive/bounded enough to ship.
- Aggregate non_ok or noisy_result_count greater than zero blocks all Operator96 app surfacing until a clean rerun exists.

## Buckets

- `blocked_by_gate`: `17`
- `not_app_surface_ready`: `79`

## Records

| # | Value | Bucket | Reason | Action |
|---:|---|---|---|---|
| 1 | `EnableLocalLogonSid` | `blocked_by_gate` | rollback-not-tested | do-not-surface-unless-blocker-is-researched-away |
| 2 | `EnableVirtualization` | `blocked_by_gate` | rollback-not-tested | do-not-surface-unless-blocker-is-researched-away |
| 3 | `AdditionalCriticalWorkerThreads` | `blocked_by_gate` | rollback-not-tested, kernel-worker-thread-override | do-not-surface-unless-blocker-is-researched-away |
| 4 | `AdditionalDelayedWorkerThreads` | `blocked_by_gate` | rollback-not-tested, kernel-worker-thread-override | do-not-surface-unless-blocker-is-researched-away |
| 5 | `UuidSequenceNumber` | `blocked_by_gate` | rollback-not-tested | do-not-surface-unless-blocker-is-researched-away |
| 6 | `TickcountRolloverDelay` | `blocked_by_gate` | rollback-not-tested | do-not-surface-unless-blocker-is-researched-away |
| 7 | `KernelWorkerTestFlags` | `blocked_by_gate` | rollback-not-tested, kernel-worker-thread-override | do-not-surface-unless-blocker-is-researched-away |
| 8 | `MaximumKernelWorkerThreads` | `blocked_by_gate` | rollback-not-tested, kernel-worker-thread-override | do-not-surface-unless-blocker-is-researched-away |
| 9 | `ForceEnableMutantAutoboost` | `blocked_by_gate` | rollback-not-tested | do-not-surface-unless-blocker-is-researched-away |
| 10 | `AllowRemoteDASD` | `blocked_by_gate` | rollback-not-tested | do-not-surface-unless-blocker-is-researched-away |
| 11 | `DisableDiskCounters` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 12 | `IoAllowLoadCrashDumpDriver` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 13 | `IoEnableSessionZeroAccessCheck` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 14 | `GlobalTimerResolutionRequests` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 15 | `ForceParkingRequested` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 16 | `EnableWerUserReporting` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 17 | `HyperStartDisabled` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 18 | `DisableLightWeightSuspend` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 19 | `TimerCheckFlags` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 20 | `ForceIdleGracePeriod` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 21 | `DisableExceptionChainValidation` | `blocked_by_gate` | rollback-not-tested, security-mitigation-override | do-not-surface-unless-blocker-is-researched-away |
| 22 | `MaxDynamicTickDuration` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 23 | `EnableTickAccumulationFromAccountingPeriods` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 24 | `EnablePerCpuClockTickScheduling` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 25 | `SerializeTimerExpiration` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 26 | `XStateContextLookasidePerProcMaxDepth` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 27 | `LongDpcQueueThreshold` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 28 | `LongDpcRuntimeThreshold` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 29 | `ForceBugcheckForDpcWatchdog` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 30 | `ForceForegroundBoostDecay` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 31 | `RebalanceMinPriority` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 32 | `InterruptSteeringFlags` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 33 | `AlwaysTrackIoBoosting` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 34 | `DisableControlFlowGuardExportSuppression` | `blocked_by_gate` | rollback-not-tested, security-mitigation-override | do-not-surface-unless-blocker-is-researched-away |
| 35 | `MaximumCooperativeIdleSearchWidth` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 36 | `HiberbootEnabled` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 37 | `PowerSettingProfile` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 38 | `WatchdogResumeTimeout` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 39 | `WatchdogSleepTimeout` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 40 | `SkipTickOverride` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 41 | `Win32CalloutWatchdogBugcheckEnabled` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 42 | `IdleScanInterval` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 43 | `SleepStudyDisabled` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 44 | `Class1InitialUnparkCount` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 45 | `CustomizeDuringSetup` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 46 | `EnergyEstimationEnabled` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 47 | `HiberFileSizePercent` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 48 | `MfBufferingThreshold` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 49 | `PerfCalculateActualUtilization` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 50 | `SourceSettingsVersion` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 51 | `TimerRebaseThresholdOnDripsExit` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 52 | `HibernateEnabledDefault` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 53 | `EventProcessorEnabled` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 54 | `LidReliabilityState` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 55 | `HibernateEnabled` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 56 | `DisableInboxPepGeneratedConstraints` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 57 | `DisableDisplayBurstOnPowerSourceChange` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 58 | `IdleProcessorsRequireQosManagement` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 59 | `TtmEnabled` | `blocked_by_gate` | rollback-not-tested | do-not-surface-unless-blocker-is-researched-away |
| 60 | `AllowAudioToEnableExecutionRequiredPowerRequests` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 61 | `DeepIoCoalescingEnabled` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 62 | `IgnoreCsComplianceCheck` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 63 | `DripsSwHwDivergenceEnableLiveDump` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 64 | `DisableVsyncLatencyUpdate` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 65 | `SleepstudyAccountingEnabled` | `blocked_by_gate` | rollback-not-tested | do-not-surface-unless-blocker-is-researched-away |
| 66 | `EnableInputSuppression` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 67 | `PerfCheckTimerImplementation` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 68 | `StandbyConnectivityGracePeriod` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 69 | `EnforceAusterityMode` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 70 | `AlwaysComputeQosHints` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 71 | `HeteroMultiCoreClassesEnabled` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 72 | `HeteroMultiClassParkingEnabled` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 73 | `DisableIdleStatesAtBoot` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 74 | `PerfBoostAtGuaranteed` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 75 | `MSDisabled` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 76 | `FxAccountingTelemetryDisabled` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 77 | `Win32kCalloutWatchdogTimeoutSeconds` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 78 | `EnableMinimalHiberFile` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 79 | `HiberbootEnabled` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 80 | `MaximumFrequencyOverride` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 81 | `PoFxSystemIrpWaitForReportDevicePowered` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 82 | `AllowSystemRequiredPowerRequests` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 83 | `CoalescingFlushInterval` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 84 | `CoalescingTimerInterval` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 85 | `HeteroHgsEePerfHintsIndependentEnabled` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 86 | `HeteroHgsPlusDisabled` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 87 | `IpiLastClockOwnerDisable` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 88 | `PowerWatchdogRequestQueueTimeoutMsec` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 89 | `PowerWatchdogPoCalloutTimeoutMsec` | `blocked_by_gate` | rollback-not-tested | do-not-surface-unless-blocker-is-researched-away |
| 90 | `PowerWatchdogPowerOnGdiTimeoutMsec` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 91 | `PowerWatchdogDwmSyncFlushTimeoutMsec` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 92 | `PowerWatchdogDrvSetMonitorTimeoutMsec` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 93 | `Policy` | `blocked_by_gate` | key-missing-in-target-vm, rollback-not-tested | do-not-surface-unless-blocker-is-researched-away |
| 94 | `EnableDsNetRefresh` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 95 | `EnabledActions` | `not_app_surface_ready` | insufficient-positive-bounded-evidence-for-app-card | keep-as-research-observation-and-do-not-surface-as-app-card |
| 96 | `PowerThrottlingOff` | `blocked_by_gate` | key-missing-in-target-vm, rollback-not-tested | do-not-surface-unless-blocker-is-researched-away |
