# Custom Registry Value App Surface Review

- Generated UTC: `2026-05-14T23:42:13Z`
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
- Clean custom registry value experiment records that are not ready_for_bounded_app_card are Contributor Lab research observations, not end-user optimization cards. operator96 is the legacy artifact ID for the current seed batch.
- Low-confidence, harmful, noisy, or host-noise-unknown experiments are observations only.
- needs_low_noise_rerun means host noise was not clean; not_app_surface_ready means the run was clean enough to store but not positive/bounded enough to ship.
- Aggregate non_ok or noisy_result_count greater than zero blocks all custom value app surfacing until a clean rerun exists.

## Buckets

- `blocked_by_gate`: `17`
- `not_app_surface_ready`: `79`

## Records

| # | Value | Destination | Bucket | Missing for app card | Action |
|---:|---|---|---|---|---|
| 1 | `EnableLocalLogonSid` | `contributor-lab-research-only` | `blocked_by_gate` | rollback_tested, clean_low_noise_vm_proofs, positive_bounded_evidence | do-not-surface-unless-blocker-is-researched-away |
| 2 | `EnableVirtualization` | `contributor-lab-research-only` | `blocked_by_gate` | rollback_tested, clean_low_noise_vm_proofs, positive_bounded_evidence | do-not-surface-unless-blocker-is-researched-away |
| 3 | `AdditionalCriticalWorkerThreads` | `contributor-lab-research-only` | `blocked_by_gate` | rollback_tested, clean_low_noise_vm_proofs, positive_bounded_evidence | do-not-surface-unless-blocker-is-researched-away |
| 4 | `AdditionalDelayedWorkerThreads` | `contributor-lab-research-only` | `blocked_by_gate` | rollback_tested, clean_low_noise_vm_proofs, positive_bounded_evidence | do-not-surface-unless-blocker-is-researched-away |
| 5 | `UuidSequenceNumber` | `contributor-lab-research-only` | `blocked_by_gate` | rollback_tested, clean_low_noise_vm_proofs, positive_bounded_evidence | do-not-surface-unless-blocker-is-researched-away |
| 6 | `TickcountRolloverDelay` | `contributor-lab-research-only` | `blocked_by_gate` | rollback_tested, clean_low_noise_vm_proofs, positive_bounded_evidence | do-not-surface-unless-blocker-is-researched-away |
| 7 | `KernelWorkerTestFlags` | `contributor-lab-research-only` | `blocked_by_gate` | rollback_tested, clean_low_noise_vm_proofs, positive_bounded_evidence | do-not-surface-unless-blocker-is-researched-away |
| 8 | `MaximumKernelWorkerThreads` | `contributor-lab-research-only` | `blocked_by_gate` | rollback_tested, clean_low_noise_vm_proofs, positive_bounded_evidence | do-not-surface-unless-blocker-is-researched-away |
| 9 | `ForceEnableMutantAutoboost` | `contributor-lab-research-only` | `blocked_by_gate` | rollback_tested, clean_low_noise_vm_proofs, positive_bounded_evidence | do-not-surface-unless-blocker-is-researched-away |
| 10 | `AllowRemoteDASD` | `contributor-lab-research-only` | `blocked_by_gate` | rollback_tested, clean_low_noise_vm_proofs, positive_bounded_evidence | do-not-surface-unless-blocker-is-researched-away |
| 11 | `DisableDiskCounters` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 12 | `IoAllowLoadCrashDumpDriver` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 13 | `IoEnableSessionZeroAccessCheck` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 14 | `GlobalTimerResolutionRequests` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 15 | `ForceParkingRequested` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 16 | `EnableWerUserReporting` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 17 | `HyperStartDisabled` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 18 | `DisableLightWeightSuspend` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 19 | `TimerCheckFlags` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 20 | `ForceIdleGracePeriod` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 21 | `DisableExceptionChainValidation` | `contributor-lab-research-only` | `blocked_by_gate` | rollback_tested, clean_low_noise_vm_proofs, positive_bounded_evidence | do-not-surface-unless-blocker-is-researched-away |
| 22 | `MaxDynamicTickDuration` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 23 | `EnableTickAccumulationFromAccountingPeriods` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 24 | `EnablePerCpuClockTickScheduling` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 25 | `SerializeTimerExpiration` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 26 | `XStateContextLookasidePerProcMaxDepth` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 27 | `LongDpcQueueThreshold` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 28 | `LongDpcRuntimeThreshold` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 29 | `ForceBugcheckForDpcWatchdog` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 30 | `ForceForegroundBoostDecay` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 31 | `RebalanceMinPriority` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 32 | `InterruptSteeringFlags` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 33 | `AlwaysTrackIoBoosting` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 34 | `DisableControlFlowGuardExportSuppression` | `contributor-lab-research-only` | `blocked_by_gate` | rollback_tested, clean_low_noise_vm_proofs, positive_bounded_evidence | do-not-surface-unless-blocker-is-researched-away |
| 35 | `MaximumCooperativeIdleSearchWidth` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 36 | `HiberbootEnabled` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 37 | `PowerSettingProfile` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 38 | `WatchdogResumeTimeout` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 39 | `WatchdogSleepTimeout` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 40 | `SkipTickOverride` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 41 | `Win32CalloutWatchdogBugcheckEnabled` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 42 | `IdleScanInterval` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 43 | `SleepStudyDisabled` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 44 | `Class1InitialUnparkCount` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 45 | `CustomizeDuringSetup` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 46 | `EnergyEstimationEnabled` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 47 | `HiberFileSizePercent` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 48 | `MfBufferingThreshold` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 49 | `PerfCalculateActualUtilization` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 50 | `SourceSettingsVersion` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 51 | `TimerRebaseThresholdOnDripsExit` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 52 | `HibernateEnabledDefault` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 53 | `EventProcessorEnabled` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 54 | `LidReliabilityState` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 55 | `HibernateEnabled` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 56 | `DisableInboxPepGeneratedConstraints` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 57 | `DisableDisplayBurstOnPowerSourceChange` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 58 | `IdleProcessorsRequireQosManagement` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 59 | `TtmEnabled` | `contributor-lab-research-only` | `blocked_by_gate` | rollback_tested, clean_low_noise_vm_proofs, positive_bounded_evidence | do-not-surface-unless-blocker-is-researched-away |
| 60 | `AllowAudioToEnableExecutionRequiredPowerRequests` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 61 | `DeepIoCoalescingEnabled` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 62 | `IgnoreCsComplianceCheck` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 63 | `DripsSwHwDivergenceEnableLiveDump` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 64 | `DisableVsyncLatencyUpdate` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 65 | `SleepstudyAccountingEnabled` | `contributor-lab-research-only` | `blocked_by_gate` | rollback_tested, clean_low_noise_vm_proofs, positive_bounded_evidence | do-not-surface-unless-blocker-is-researched-away |
| 66 | `EnableInputSuppression` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 67 | `PerfCheckTimerImplementation` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 68 | `StandbyConnectivityGracePeriod` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 69 | `EnforceAusterityMode` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 70 | `AlwaysComputeQosHints` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 71 | `HeteroMultiCoreClassesEnabled` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 72 | `HeteroMultiClassParkingEnabled` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 73 | `DisableIdleStatesAtBoot` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 74 | `PerfBoostAtGuaranteed` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 75 | `MSDisabled` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 76 | `FxAccountingTelemetryDisabled` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 77 | `Win32kCalloutWatchdogTimeoutSeconds` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 78 | `EnableMinimalHiberFile` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 79 | `HiberbootEnabled` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 80 | `MaximumFrequencyOverride` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 81 | `PoFxSystemIrpWaitForReportDevicePowered` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 82 | `AllowSystemRequiredPowerRequests` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 83 | `CoalescingFlushInterval` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 84 | `CoalescingTimerInterval` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 85 | `HeteroHgsEePerfHintsIndependentEnabled` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 86 | `HeteroHgsPlusDisabled` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 87 | `IpiLastClockOwnerDisable` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 88 | `PowerWatchdogRequestQueueTimeoutMsec` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 89 | `PowerWatchdogPoCalloutTimeoutMsec` | `contributor-lab-research-only` | `blocked_by_gate` | rollback_tested, clean_low_noise_vm_proofs, positive_bounded_evidence | do-not-surface-unless-blocker-is-researched-away |
| 90 | `PowerWatchdogPowerOnGdiTimeoutMsec` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 91 | `PowerWatchdogDwmSyncFlushTimeoutMsec` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 92 | `PowerWatchdogDrvSetMonitorTimeoutMsec` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 93 | `Policy` | `contributor-lab-research-only` | `blocked_by_gate` | rollback_tested, clean_low_noise_vm_proofs, positive_bounded_evidence | do-not-surface-unless-blocker-is-researched-away |
| 94 | `EnableDsNetRefresh` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 95 | `EnabledActions` | `contributor-lab-research-only` | `not_app_surface_ready` | positive_bounded_evidence | keep-as-research-observation-and-do-not-surface-as-app-card |
| 96 | `PowerThrottlingOff` | `contributor-lab-research-only` | `blocked_by_gate` | rollback_tested, clean_low_noise_vm_proofs, positive_bounded_evidence | do-not-surface-unless-blocker-is-researched-away |
