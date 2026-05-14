# Operator Reg Add Follow-up Report

- Generated UTC: `2026-05-09T08:56:28Z`
- Baseline: `registry-research-framework/audit/operator-regadd-vm-baseline-20260509T081911Z.json`
- Key-missing records: `2`
- Default rows: `94`

## Key-Missing Audit

| Target | VM 25H2 | Verdict | Default interpretation | Recommendation |
|---|---|---|---|---|
| `HKLM\SYSTEM\CurrentControlSet\Control\Power\ForceHibernateDisabled\Policy` | `key-missing` | `no-authoritative-evidence-for-25h2` | absent-on-clean-25h2 | do-not-promote-without-consumer-proof |
| `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling\PowerThrottlingOff` | `key-missing` | `source-backed-policy-default-absent` | not-configured/unset; ADMX enabled=1 disabled=0 | keep-as-policy-backed; distinguish clean-default-absent from configured state |

## Default Value Matrix

| # | Target | VM status | Default | Source quality | Test values |
|---:|---|---|---|---|---|
| 1 | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM\EnableLocalLogonSid` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 2 | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM\EnableVirtualization` | `value-present` | `1` | `vm-observed` | `[0, 1]` |
| 3 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\AdditionalCriticalWorkerThreads` | `value-present` | `0` | `vm-observed` | `[5, 0, 1]` |
| 4 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\AdditionalDelayedWorkerThreads` | `value-present` | `0` | `vm-observed` | `[5, 0, 1]` |
| 5 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\UuidSequenceNumber` | `value-present` | `2636877` | `vm-observed` | `[3322358, 0, 1]` |
| 6 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\TickcountRolloverDelay` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 7 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\KernelWorkerTestFlags` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 8 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\MaximumKernelWorkerThreads` | `value-missing` | `absent` | `vm-observed` | `[25000, 0, 1]` |
| 9 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\ForceEnableMutantAutoboost` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 10 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\AllowRemoteDASD` | `value-present` | `0` | `vm-observed` | `[0, 1]` |
| 11 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\DisableDiskCounters` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 12 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\IoAllowLoadCrashDumpDriver` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 13 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\IoEnableSessionZeroAccessCheck` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 14 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\GlobalTimerResolutionRequests` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 15 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceParkingRequested` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 16 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnableWerUserReporting` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 17 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\HyperStartDisabled` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 18 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\DisableLightWeightSuspend` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 19 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\TimerCheckFlags` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 20 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceIdleGracePeriod` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 21 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\DisableExceptionChainValidation` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 22 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\MaxDynamicTickDuration` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 23 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnableTickAccumulationFromAccountingPeriods` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 24 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnablePerCpuClockTickScheduling` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 25 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\SerializeTimerExpiration` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 26 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\XStateContextLookasidePerProcMaxDepth` | `value-missing` | `absent` | `vm-observed` | `[1024, 0, 1]` |
| 27 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\LongDpcQueueThreshold` | `value-missing` | `absent` | `vm-observed` | `[2, 0, 1]` |
| 28 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\LongDpcRuntimeThreshold` | `value-missing` | `absent` | `vm-observed` | `[50, 0, 1]` |
| 29 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceBugcheckForDpcWatchdog` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 30 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceForegroundBoostDecay` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 31 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\RebalanceMinPriority` | `value-missing` | `absent` | `vm-observed` | `[16, 0, 1]` |
| 32 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\InterruptSteeringFlags` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 33 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\AlwaysTrackIoBoosting` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 34 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\DisableControlFlowGuardExportSuppression` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 35 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\MaximumCooperativeIdleSearchWidth` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 36 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\HiberbootEnabled` | `value-present` | `1` | `vm-observed` | `[0, 1]` |
| 37 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\PowerSettingProfile` | `value-present` | `0` | `vm-observed` | `[0, 1]` |
| 38 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\WatchdogResumeTimeout` | `value-present` | `120` | `vm-observed` | `[0, 1]` |
| 39 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\WatchdogSleepTimeout` | `value-present` | `300` | `vm-observed` | `[0, 1]` |
| 40 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\SkipTickOverride` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 41 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\Win32CalloutWatchdogBugcheckEnabled` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 42 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\IdleScanInterval` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 43 | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\SleepStudyDisabled` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 44 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\Class1InitialUnparkCount` | `value-present` | `64` | `vm-observed` | `[64, 0, 1]` |
| 45 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\CustomizeDuringSetup` | `value-present` | `1` | `vm-observed` | `[1, 0]` |
| 46 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnergyEstimationEnabled` | `value-present` | `1` | `vm-observed` | `[0, 1]` |
| 47 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HiberFileSizePercent` | `value-present` | `0` | `vm-observed` | `[0, 1]` |
| 48 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\MfBufferingThreshold` | `value-present` | `0` | `vm-observed` | `[0, 1]` |
| 49 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfCalculateActualUtilization` | `value-present` | `1` | `vm-observed` | `[0, 1]` |
| 50 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\SourceSettingsVersion` | `value-present` | `4` | `vm-observed` | `[4, 0, 1]` |
| 51 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\TimerRebaseThresholdOnDripsExit` | `value-present` | `60` | `vm-observed` | `[60, 0, 1]` |
| 52 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HibernateEnabledDefault` | `value-present` | `1` | `vm-observed` | `[0, 1]` |
| 53 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EventProcessorEnabled` | `value-present` | `1` | `vm-observed` | `[0, 1]` |
| 54 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\LidReliabilityState` | `value-present` | `1` | `vm-observed` | `[1, 0]` |
| 55 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HibernateEnabled` | `value-present` | `0` | `vm-observed` | `[0, 1]` |
| 56 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableInboxPepGeneratedConstraints` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 57 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableDisplayBurstOnPowerSourceChange` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 58 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IdleProcessorsRequireQosManagement` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 59 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\TtmEnabled` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 60 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AllowAudioToEnableExecutionRequiredPowerRequests` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 61 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DeepIoCoalescingEnabled` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 62 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IgnoreCsComplianceCheck` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 63 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DripsSwHwDivergenceEnableLiveDump` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 64 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableVsyncLatencyUpdate` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 65 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\SleepstudyAccountingEnabled` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 66 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnableInputSuppression` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 67 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfCheckTimerImplementation` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 68 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\StandbyConnectivityGracePeriod` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 69 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnforceAusterityMode` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 70 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AlwaysComputeQosHints` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 71 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroMultiCoreClassesEnabled` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 72 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroMultiClassParkingEnabled` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 73 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableIdleStatesAtBoot` | `value-missing` | `absent` | `vm-observed` | `[2, 0, 1]` |
| 74 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfBoostAtGuaranteed` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 75 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\MSDisabled` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 76 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\FxAccountingTelemetryDisabled` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 77 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\Win32kCalloutWatchdogTimeoutSeconds` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 78 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnableMinimalHiberFile` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 79 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HiberbootEnabled` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 80 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\MaximumFrequencyOverride` | `value-missing` | `absent` | `vm-observed` | `[100, 0, 1]` |
| 81 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PoFxSystemIrpWaitForReportDevicePowered` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 82 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AllowSystemRequiredPowerRequests` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 83 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\CoalescingFlushInterval` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 84 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\CoalescingTimerInterval` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 85 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroHgsEePerfHintsIndependentEnabled` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 86 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroHgsPlusDisabled` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 87 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IpiLastClockOwnerDisable` | `value-missing` | `absent` | `vm-observed` | `[1, 0]` |
| 88 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogRequestQueueTimeoutMsec` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 89 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogPoCalloutTimeoutMsec` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 90 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogPowerOnGdiTimeoutMsec` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 91 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogDwmSyncFlushTimeoutMsec` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 92 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogDrvSetMonitorTimeoutMsec` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 94 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\ModernSleep\EnableDsNetRefresh` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |
| 95 | `HKLM\SYSTEM\CurrentControlSet\Control\Power\ModernSleep\EnabledActions` | `value-missing` | `absent` | `vm-observed` | `[0, 1]` |

## External References

- Microsoft Learn ADMX_Power Policy CSP: https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-power#powerthrottlingturnoff
- ReactOS hibernation capability diff reviewed as a non-exact source: https://reactos.org/archives/public/ros-diffs/2020-March/072845.html
