# Operator96 App Surface Review

- Generated UTC: `2026-05-13T22:40:23Z`
- Matrix: `registry-research-framework/audit/operator96-enriched-value-matrix-20260510.json`
- Records: `96`
- Ready for bounded app card: `0`
- Needs low-noise rerun: `79`
- Blocked by gate: `17`
- Blocked by safety: `0`

## Policy

- Only ready_for_bounded_app_card may enter the app surface without another VM campaign.
- Low-confidence, noisy, or host-noise-unknown experiments are observations only.
- needs_low_noise_rerun records require repeated low-noise VM runs before card copy or performance claims.

## Buckets

- `blocked_by_gate`: `17`
- `needs_low_noise_rerun`: `79`

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
| 11 | `DisableDiskCounters` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 12 | `IoAllowLoadCrashDumpDriver` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 13 | `IoEnableSessionZeroAccessCheck` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 14 | `GlobalTimerResolutionRequests` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 15 | `ForceParkingRequested` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 16 | `EnableWerUserReporting` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 17 | `HyperStartDisabled` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 18 | `DisableLightWeightSuspend` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 19 | `TimerCheckFlags` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 20 | `ForceIdleGracePeriod` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 21 | `DisableExceptionChainValidation` | `blocked_by_gate` | rollback-not-tested, security-mitigation-override | do-not-surface-unless-blocker-is-researched-away |
| 22 | `MaxDynamicTickDuration` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 23 | `EnableTickAccumulationFromAccountingPeriods` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 24 | `EnablePerCpuClockTickScheduling` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 25 | `SerializeTimerExpiration` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 26 | `XStateContextLookasidePerProcMaxDepth` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 27 | `LongDpcQueueThreshold` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 28 | `LongDpcRuntimeThreshold` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 29 | `ForceBugcheckForDpcWatchdog` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 30 | `ForceForegroundBoostDecay` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 31 | `RebalanceMinPriority` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 32 | `InterruptSteeringFlags` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 33 | `AlwaysTrackIoBoosting` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 34 | `DisableControlFlowGuardExportSuppression` | `blocked_by_gate` | rollback-not-tested, security-mitigation-override | do-not-surface-unless-blocker-is-researched-away |
| 35 | `MaximumCooperativeIdleSearchWidth` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 36 | `HiberbootEnabled` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 37 | `PowerSettingProfile` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 38 | `WatchdogResumeTimeout` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 39 | `WatchdogSleepTimeout` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 40 | `SkipTickOverride` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 41 | `Win32CalloutWatchdogBugcheckEnabled` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 42 | `IdleScanInterval` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 43 | `SleepStudyDisabled` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 44 | `Class1InitialUnparkCount` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 45 | `CustomizeDuringSetup` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 46 | `EnergyEstimationEnabled` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 47 | `HiberFileSizePercent` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 48 | `MfBufferingThreshold` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 49 | `PerfCalculateActualUtilization` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 50 | `SourceSettingsVersion` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 51 | `TimerRebaseThresholdOnDripsExit` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 52 | `HibernateEnabledDefault` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 53 | `EventProcessorEnabled` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 54 | `LidReliabilityState` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 55 | `HibernateEnabled` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 56 | `DisableInboxPepGeneratedConstraints` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 57 | `DisableDisplayBurstOnPowerSourceChange` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 58 | `IdleProcessorsRequireQosManagement` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 59 | `TtmEnabled` | `blocked_by_gate` | rollback-not-tested | do-not-surface-unless-blocker-is-researched-away |
| 60 | `AllowAudioToEnableExecutionRequiredPowerRequests` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 61 | `DeepIoCoalescingEnabled` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 62 | `IgnoreCsComplianceCheck` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 63 | `DripsSwHwDivergenceEnableLiveDump` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 64 | `DisableVsyncLatencyUpdate` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 65 | `SleepstudyAccountingEnabled` | `blocked_by_gate` | rollback-not-tested | do-not-surface-unless-blocker-is-researched-away |
| 66 | `EnableInputSuppression` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 67 | `PerfCheckTimerImplementation` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 68 | `StandbyConnectivityGracePeriod` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 69 | `EnforceAusterityMode` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 70 | `AlwaysComputeQosHints` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 71 | `HeteroMultiCoreClassesEnabled` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 72 | `HeteroMultiClassParkingEnabled` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 73 | `DisableIdleStatesAtBoot` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 74 | `PerfBoostAtGuaranteed` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 75 | `MSDisabled` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 76 | `FxAccountingTelemetryDisabled` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 77 | `Win32kCalloutWatchdogTimeoutSeconds` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 78 | `EnableMinimalHiberFile` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 79 | `HiberbootEnabled` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 80 | `MaximumFrequencyOverride` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 81 | `PoFxSystemIrpWaitForReportDevicePowered` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 82 | `AllowSystemRequiredPowerRequests` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 83 | `CoalescingFlushInterval` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 84 | `CoalescingTimerInterval` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 85 | `HeteroHgsEePerfHintsIndependentEnabled` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 86 | `HeteroHgsPlusDisabled` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 87 | `IpiLastClockOwnerDisable` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 88 | `PowerWatchdogRequestQueueTimeoutMsec` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 89 | `PowerWatchdogPoCalloutTimeoutMsec` | `blocked_by_gate` | rollback-not-tested | do-not-surface-unless-blocker-is-researched-away |
| 90 | `PowerWatchdogPowerOnGdiTimeoutMsec` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 91 | `PowerWatchdogDwmSyncFlushTimeoutMsec` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 92 | `PowerWatchdogDrvSetMonitorTimeoutMsec` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 93 | `Policy` | `blocked_by_gate` | key-missing-in-target-vm, rollback-not-tested | do-not-surface-unless-blocker-is-researched-away |
| 94 | `EnableDsNetRefresh` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 95 | `EnabledActions` | `needs_low_noise_rerun` | low-noise-repeat-required-before-app-card | rerun-low-noise-before-any-app-card-or-performance-claim |
| 96 | `PowerThrottlingOff` | `blocked_by_gate` | key-missing-in-target-vm, rollback-not-tested | do-not-surface-unless-blocker-is-researched-away |
