# Operator Reg Add VM Baseline

- Generated UTC: `2026-05-09T08:19:21Z`
- Status: **ok**
- Total entries: `96`
- Key present/missing: `94` / `2`
- Value present/missing: `21` / `73`
- Error count: `0`
- Repo exact target matches: `40`

## Records

| # | Target | Requested | VM status | Current value | Kind | Sibling count | Repo hits | Risk flags |
|---:|---|---:|---|---|---|---:|---:|---|
| 1 | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM\EnableLocalLogonSid` | `0` | `value-missing` | `` | `` | 23 | 0 |  |
| 2 | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM\EnableVirtualization` | `0` | `value-present` | `1` | `DWord` | 23 | 3 |  |
| 3 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\AdditionalCriticalWorkerThreads` | `5` | `value-present` | `0` | `DWord` | 3 | 2 | executive-worker-thread low-level profiling-required |
| 4 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\AdditionalDelayedWorkerThreads` | `5` | `value-present` | `0` | `DWord` | 3 | 2 | executive-worker-thread low-level profiling-required |
| 5 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\UuidSequenceNumber` | `3322358` | `value-present` | `2636877` | `DWord` | 3 | 1 |  |
| 6 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\TickcountRolloverDelay` | `0` | `value-missing` | `` | `` | 3 | 0 | kernel-timing-sensitive |
| 7 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\KernelWorkerTestFlags` | `0` | `value-missing` | `` | `` | 3 | 0 |  |
| 8 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\MaximumKernelWorkerThreads` | `25000` | `value-missing` | `` | `` | 3 | 0 | executive-worker-thread low-level profiling-required |
| 9 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\ForceEnableMutantAutoboost` | `1` | `value-missing` | `` | `` | 3 | 0 |  |
| 10 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\AllowRemoteDASD` | `0` | `value-present` | `0` | `DWord` | 1 | 1 | deprecated-path-collision removable-storage-policy-is-active-surface |
| 11 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\DisableDiskCounters` | `1` | `value-missing` | `` | `` | 1 | 0 |  |
| 12 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\IoAllowLoadCrashDumpDriver` | `0` | `value-missing` | `` | `` | 1 | 0 |  |
| 13 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\IoEnableSessionZeroAccessCheck` | `1` | `value-missing` | `` | `` | 1 | 0 |  |
| 14 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\GlobalTimerResolutionRequests` | `1` | `value-missing` | `` | `` | 4 | 1 | kernel-timing-sensitive |
| 15 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceParkingRequested` | `0` | `value-missing` | `` | `` | 4 | 0 |  |
| 16 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnableWerUserReporting` | `0` | `value-missing` | `` | `` | 4 | 0 |  |
| 17 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\HyperStartDisabled` | `1` | `value-missing` | `` | `` | 4 | 0 |  |
| 18 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\DisableLightWeightSuspend` | `0` | `value-missing` | `` | `` | 4 | 0 |  |
| 19 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\TimerCheckFlags` | `0` | `value-missing` | `` | `` | 4 | 1 | kernel-timing-sensitive |
| 20 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceIdleGracePeriod` | `0` | `value-missing` | `` | `` | 4 | 0 |  |
| 21 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\DisableExceptionChainValidation` | `1` | `value-missing` | `` | `` | 4 | 2 | security-hold mitigation-bypass never-default-apply |
| 22 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\MaxDynamicTickDuration` | `1` | `value-missing` | `` | `` | 4 | 0 | kernel-timing-sensitive |
| 23 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnableTickAccumulationFromAccountingPeriods` | `1` | `value-missing` | `` | `` | 4 | 0 | kernel-timing-sensitive |
| 24 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnablePerCpuClockTickScheduling` | `1` | `value-missing` | `` | `` | 4 | 0 | kernel-timing-sensitive |
| 25 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\SerializeTimerExpiration` | `0` | `value-missing` | `` | `` | 4 | 2 | kernel-timing-sensitive |
| 26 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\XStateContextLookasidePerProcMaxDepth` | `1024` | `value-missing` | `` | `` | 4 | 0 |  |
| 27 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\LongDpcQueueThreshold` | `2` | `value-missing` | `` | `` | 4 | 1 | kernel-timing-sensitive |
| 28 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\LongDpcRuntimeThreshold` | `50` | `value-missing` | `` | `` | 4 | 1 | kernel-timing-sensitive |
| 29 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceBugcheckForDpcWatchdog` | `0` | `value-missing` | `` | `` | 4 | 1 | watchdog/bugcheck-sensitive, kernel-timing-sensitive |
| 30 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceForegroundBoostDecay` | `0` | `value-missing` | `` | `` | 4 | 0 |  |
| 31 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\RebalanceMinPriority` | `16` | `value-missing` | `` | `` | 4 | 0 |  |
| 32 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\InterruptSteeringFlags` | `1` | `value-missing` | `` | `` | 4 | 0 | kernel-timing-sensitive |
| 33 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\AlwaysTrackIoBoosting` | `0` | `value-missing` | `` | `` | 4 | 0 |  |
| 34 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\DisableControlFlowGuardExportSuppression` | `1` | `value-missing` | `` | `` | 4 | 0 |  |
| 35 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\MaximumCooperativeIdleSearchWidth` | `0` | `value-missing` | `` | `` | 4 | 0 |  |
| 36 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\HiberbootEnabled` | `0` | `value-present` | `1` | `DWord` | 14 | 2 |  |
| 37 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\PowerSettingProfile` | `0` | `value-present` | `0` | `DWord` | 14 | 0 |  |
| 38 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\WatchdogResumeTimeout` | `0` | `value-present` | `120` | `DWord` | 14 | 2 | watchdog/bugcheck-sensitive |
| 39 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\WatchdogSleepTimeout` | `0` | `value-present` | `300` | `DWord` | 14 | 2 | watchdog/bugcheck-sensitive |
| 40 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\SkipTickOverride` | `0` | `value-missing` | `` | `` | 14 | 0 | kernel-timing-sensitive |
| 41 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\Win32CalloutWatchdogBugcheckEnabled` | `0` | `value-missing` | `` | `` | 14 | 1 | watchdog/bugcheck-sensitive |
| 42 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\IdleScanInterval` | `0` | `value-missing` | `` | `` | 14 | 0 |  |
| 43 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\SleepStudyDisabled` | `1` | `value-missing` | `` | `` | 14 | 0 |  |
| 44 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\Class1InitialUnparkCount` | `64` | `value-present` | `64` | `DWord` | 12 | 2 |  |
| 45 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\CustomizeDuringSetup` | `1` | `value-present` | `1` | `DWord` | 12 | 0 |  |
| 46 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnergyEstimationEnabled` | `0` | `value-present` | `1` | `DWord` | 12 | 2 |  |
| 47 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HiberFileSizePercent` | `0` | `value-present` | `0` | `DWord` | 12 | 2 |  |
| 48 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\MfBufferingThreshold` | `0` | `value-present` | `0` | `DWord` | 12 | 2 |  |
| 49 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfCalculateActualUtilization` | `0` | `value-present` | `1` | `DWord` | 12 | 2 |  |
| 50 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\SourceSettingsVersion` | `4` | `value-present` | `4` | `DWord` | 12 | 0 |  |
| 51 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\TimerRebaseThresholdOnDripsExit` | `60` | `value-present` | `60` | `DWord` | 12 | 2 | kernel-timing-sensitive |
| 52 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HibernateEnabledDefault` | `0` | `value-present` | `1` | `DWord` | 12 | 1 |  |
| 53 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EventProcessorEnabled` | `0` | `value-present` | `1` | `DWord` | 12 | 2 |  |
| 54 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\LidReliabilityState` | `1` | `value-present` | `1` | `DWord` | 12 | 2 |  |
| 55 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HibernateEnabled` | `0` | `value-present` | `0` | `DWord` | 12 | 2 |  |
| 56 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableInboxPepGeneratedConstraints` | `1` | `value-missing` | `` | `` | 12 | 0 |  |
| 57 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableDisplayBurstOnPowerSourceChange` | `1` | `value-missing` | `` | `` | 12 | 0 |  |
| 58 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IdleProcessorsRequireQosManagement` | `0` | `value-missing` | `` | `` | 12 | 0 |  |
| 59 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\TtmEnabled` | `0` | `value-missing` | `` | `` | 12 | 1 |  |
| 60 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AllowAudioToEnableExecutionRequiredPowerRequests` | `0` | `value-missing` | `` | `` | 12 | 2 |  |
| 61 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DeepIoCoalescingEnabled` | `0` | `value-missing` | `` | `` | 12 | 2 |  |
| 62 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IgnoreCsComplianceCheck` | `1` | `value-missing` | `` | `` | 12 | 0 |  |
| 63 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DripsSwHwDivergenceEnableLiveDump` | `0` | `value-missing` | `` | `` | 12 | 0 |  |
| 64 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableVsyncLatencyUpdate` | `1` | `value-missing` | `` | `` | 12 | 0 |  |
| 65 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\SleepstudyAccountingEnabled` | `0` | `value-missing` | `` | `` | 12 | 0 |  |
| 66 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnableInputSuppression` | `1` | `value-missing` | `` | `` | 12 | 0 |  |
| 67 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfCheckTimerImplementation` | `0` | `value-missing` | `` | `` | 12 | 0 | kernel-timing-sensitive |
| 68 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\StandbyConnectivityGracePeriod` | `0` | `value-missing` | `` | `` | 12 | 0 |  |
| 69 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnforceAusterityMode` | `0` | `value-missing` | `` | `` | 12 | 0 |  |
| 70 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AlwaysComputeQosHints` | `0` | `value-missing` | `` | `` | 12 | 0 |  |
| 71 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroMultiCoreClassesEnabled` | `0` | `value-missing` | `` | `` | 12 | 0 |  |
| 72 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroMultiClassParkingEnabled` | `0` | `value-missing` | `` | `` | 12 | 0 |  |
| 73 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableIdleStatesAtBoot` | `2` | `value-missing` | `` | `` | 12 | 2 |  |
| 74 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfBoostAtGuaranteed` | `1` | `value-missing` | `` | `` | 12 | 0 |  |
| 75 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\MSDisabled` | `1` | `value-missing` | `` | `` | 12 | 1 |  |
| 76 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\FxAccountingTelemetryDisabled` | `1` | `value-missing` | `` | `` | 12 | 0 |  |
| 77 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\Win32kCalloutWatchdogTimeoutSeconds` | `0` | `value-missing` | `` | `` | 12 | 1 | watchdog/bugcheck-sensitive |
| 78 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnableMinimalHiberFile` | `0` | `value-missing` | `` | `` | 12 | 0 |  |
| 79 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HiberbootEnabled` | `0` | `value-missing` | `` | `` | 12 | 0 |  |
| 80 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\MaximumFrequencyOverride` | `100` | `value-missing` | `` | `` | 12 | 0 |  |
| 81 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PoFxSystemIrpWaitForReportDevicePowered` | `0` | `value-missing` | `` | `` | 12 | 0 |  |
| 82 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AllowSystemRequiredPowerRequests` | `0` | `value-missing` | `` | `` | 12 | 2 |  |
| 83 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\CoalescingFlushInterval` | `0` | `value-missing` | `` | `` | 12 | 0 |  |
| 84 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\CoalescingTimerInterval` | `0` | `value-missing` | `` | `` | 12 | 2 | kernel-timing-sensitive |
| 85 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroHgsEePerfHintsIndependentEnabled` | `1` | `value-missing` | `` | `` | 12 | 0 |  |
| 86 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroHgsPlusDisabled` | `1` | `value-missing` | `` | `` | 12 | 0 |  |
| 87 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IpiLastClockOwnerDisable` | `1` | `value-missing` | `` | `` | 12 | 0 |  |
| 88 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogRequestQueueTimeoutMsec` | `0` | `value-missing` | `` | `` | 12 | 1 | watchdog/bugcheck-sensitive |
| 89 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogPoCalloutTimeoutMsec` | `0` | `value-missing` | `` | `` | 12 | 1 | watchdog/bugcheck-sensitive |
| 90 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogPowerOnGdiTimeoutMsec` | `0` | `value-missing` | `` | `` | 12 | 1 | watchdog/bugcheck-sensitive |
| 91 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogDwmSyncFlushTimeoutMsec` | `0` | `value-missing` | `` | `` | 12 | 1 | watchdog/bugcheck-sensitive |
| 92 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogDrvSetMonitorTimeoutMsec` | `0` | `value-missing` | `` | `` | 12 | 1 | watchdog/bugcheck-sensitive |
| 93 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\ForceHibernateDisabled\Policy` | `1` | `key-missing` | `` | `` |  | 0 |  |
| 94 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\ModernSleep\EnableDsNetRefresh` | `0` | `value-missing` | `` | `` | 0 | 0 |  |
| 95 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\ModernSleep\EnabledActions` | `0` | `value-missing` | `` | `` | 0 | 0 |  |
| 96 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling\PowerThrottlingOff` | `1` | `key-missing` | `` | `` |  | 2 |  |
