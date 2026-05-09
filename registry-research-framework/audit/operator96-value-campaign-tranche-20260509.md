# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-09T22:09:04Z`
- Status: **ok**
- Planned experiments: `100`
- Completed in this run: `100`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 1 | `operator96-001-enablelocallogonsid-0` | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM\EnableLocalLogonSid` | `0` | `absent` | `vm-observed` |
| 1 | `operator96-001-enablelocallogonsid-1` | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM\EnableLocalLogonSid` | `1` | `absent` | `vm-observed` |
| 2 | `operator96-002-enablevirtualization-0` | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM\EnableVirtualization` | `0` | `1` | `vm-observed` |
| 3 | `operator96-003-additionalcriticalworkerthreads-5` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\AdditionalCriticalWorkerThreads` | `5` | `0` | `vm-observed` |
| 3 | `operator96-003-additionalcriticalworkerthreads-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\AdditionalCriticalWorkerThreads` | `1` | `0` | `vm-observed` |
| 4 | `operator96-004-additionaldelayedworkerthreads-5` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\AdditionalDelayedWorkerThreads` | `5` | `0` | `vm-observed` |
| 4 | `operator96-004-additionaldelayedworkerthreads-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\AdditionalDelayedWorkerThreads` | `1` | `0` | `vm-observed` |
| 5 | `operator96-005-uuidsequencenumber-3322358` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\UuidSequenceNumber` | `3322358` | `2636877` | `vm-observed` |
| 5 | `operator96-005-uuidsequencenumber-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\UuidSequenceNumber` | `0` | `2636877` | `vm-observed` |
| 6 | `operator96-006-tickcountrolloverdelay-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\TickcountRolloverDelay` | `0` | `absent` | `vm-observed` |
| 6 | `operator96-006-tickcountrolloverdelay-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\TickcountRolloverDelay` | `1` | `absent` | `vm-observed` |
| 7 | `operator96-007-kernelworkertestflags-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\KernelWorkerTestFlags` | `0` | `absent` | `vm-observed` |
| 7 | `operator96-007-kernelworkertestflags-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\KernelWorkerTestFlags` | `1` | `absent` | `vm-observed` |
| 8 | `operator96-008-maximumkernelworkerthreads-25000` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\MaximumKernelWorkerThreads` | `25000` | `absent` | `vm-observed` |
| 8 | `operator96-008-maximumkernelworkerthreads-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\MaximumKernelWorkerThreads` | `0` | `absent` | `vm-observed` |
| 9 | `operator96-009-forceenablemutantautoboost-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\ForceEnableMutantAutoboost` | `1` | `absent` | `vm-observed` |
| 9 | `operator96-009-forceenablemutantautoboost-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Executive\ForceEnableMutantAutoboost` | `0` | `absent` | `vm-observed` |
| 10 | `operator96-010-allowremotedasd-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\AllowRemoteDASD` | `1` | `0` | `vm-observed` |
| 11 | `operator96-011-disablediskcounters-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\DisableDiskCounters` | `1` | `absent` | `vm-observed` |
| 11 | `operator96-011-disablediskcounters-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\DisableDiskCounters` | `0` | `absent` | `vm-observed` |
| 12 | `operator96-012-ioallowloadcrashdumpdriver-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\IoAllowLoadCrashDumpDriver` | `0` | `absent` | `vm-observed` |
| 12 | `operator96-012-ioallowloadcrashdumpdriver-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\IoAllowLoadCrashDumpDriver` | `1` | `absent` | `vm-observed` |
| 13 | `operator96-013-ioenablesessionzeroaccesscheck-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\IoEnableSessionZeroAccessCheck` | `1` | `absent` | `vm-observed` |
| 13 | `operator96-013-ioenablesessionzeroaccesscheck-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\IoEnableSessionZeroAccessCheck` | `0` | `absent` | `vm-observed` |
| 14 | `operator96-014-globaltimerresolutionrequests-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\GlobalTimerResolutionRequests` | `1` | `absent` | `vm-observed` |
| 14 | `operator96-014-globaltimerresolutionrequests-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\GlobalTimerResolutionRequests` | `0` | `absent` | `vm-observed` |
| 15 | `operator96-015-forceparkingrequested-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceParkingRequested` | `0` | `absent` | `vm-observed` |
| 15 | `operator96-015-forceparkingrequested-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceParkingRequested` | `1` | `absent` | `vm-observed` |
| 16 | `operator96-016-enableweruserreporting-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnableWerUserReporting` | `0` | `absent` | `vm-observed` |
| 16 | `operator96-016-enableweruserreporting-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnableWerUserReporting` | `1` | `absent` | `vm-observed` |
| 17 | `operator96-017-hyperstartdisabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\HyperStartDisabled` | `1` | `absent` | `vm-observed` |
| 17 | `operator96-017-hyperstartdisabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\HyperStartDisabled` | `0` | `absent` | `vm-observed` |
| 18 | `operator96-018-disablelightweightsuspend-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\DisableLightWeightSuspend` | `0` | `absent` | `vm-observed` |
| 18 | `operator96-018-disablelightweightsuspend-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\DisableLightWeightSuspend` | `1` | `absent` | `vm-observed` |
| 19 | `operator96-019-timercheckflags-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\TimerCheckFlags` | `0` | `absent` | `vm-observed` |
| 19 | `operator96-019-timercheckflags-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\TimerCheckFlags` | `1` | `absent` | `vm-observed` |
| 20 | `operator96-020-forceidlegraceperiod-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceIdleGracePeriod` | `0` | `absent` | `vm-observed` |
| 20 | `operator96-020-forceidlegraceperiod-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceIdleGracePeriod` | `1` | `absent` | `vm-observed` |
| 21 | `operator96-021-disableexceptionchainvalidation-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\DisableExceptionChainValidation` | `1` | `absent` | `vm-observed` |
| 21 | `operator96-021-disableexceptionchainvalidation-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\DisableExceptionChainValidation` | `0` | `absent` | `vm-observed` |
| 22 | `operator96-022-maxdynamictickduration-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\MaxDynamicTickDuration` | `1` | `absent` | `vm-observed` |
| 22 | `operator96-022-maxdynamictickduration-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\MaxDynamicTickDuration` | `0` | `absent` | `vm-observed` |
| 23 | `operator96-023-enabletickaccumulationfromaccountingperiods-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnableTickAccumulationFromAccountingPeriods` | `1` | `absent` | `vm-observed` |
| 23 | `operator96-023-enabletickaccumulationfromaccountingperiods-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnableTickAccumulationFromAccountingPeriods` | `0` | `absent` | `vm-observed` |
| 24 | `operator96-024-enablepercpuclocktickscheduling-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnablePerCpuClockTickScheduling` | `1` | `absent` | `vm-observed` |
| 24 | `operator96-024-enablepercpuclocktickscheduling-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\EnablePerCpuClockTickScheduling` | `0` | `absent` | `vm-observed` |
| 25 | `operator96-025-serializetimerexpiration-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\SerializeTimerExpiration` | `0` | `absent` | `vm-observed` |
| 25 | `operator96-025-serializetimerexpiration-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\SerializeTimerExpiration` | `1` | `absent` | `vm-observed` |
| 26 | `operator96-026-xstatecontextlookasideperprocmaxdepth-1024` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\XStateContextLookasidePerProcMaxDepth` | `1024` | `absent` | `vm-observed` |
| 26 | `operator96-026-xstatecontextlookasideperprocmaxdepth-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\XStateContextLookasidePerProcMaxDepth` | `0` | `absent` | `vm-observed` |
| 27 | `operator96-027-longdpcqueuethreshold-2` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\LongDpcQueueThreshold` | `2` | `absent` | `vm-observed` |
| 27 | `operator96-027-longdpcqueuethreshold-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\LongDpcQueueThreshold` | `0` | `absent` | `vm-observed` |
| 28 | `operator96-028-longdpcruntimethreshold-50` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\LongDpcRuntimeThreshold` | `50` | `absent` | `vm-observed` |
| 28 | `operator96-028-longdpcruntimethreshold-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\LongDpcRuntimeThreshold` | `0` | `absent` | `vm-observed` |
| 29 | `operator96-029-forcebugcheckfordpcwatchdog-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceBugcheckForDpcWatchdog` | `0` | `absent` | `vm-observed` |
| 29 | `operator96-029-forcebugcheckfordpcwatchdog-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceBugcheckForDpcWatchdog` | `1` | `absent` | `vm-observed` |
| 30 | `operator96-030-forceforegroundboostdecay-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceForegroundBoostDecay` | `0` | `absent` | `vm-observed` |
| 30 | `operator96-030-forceforegroundboostdecay-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\ForceForegroundBoostDecay` | `1` | `absent` | `vm-observed` |
| 31 | `operator96-031-rebalanceminpriority-16` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\RebalanceMinPriority` | `16` | `absent` | `vm-observed` |
| 31 | `operator96-031-rebalanceminpriority-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\RebalanceMinPriority` | `0` | `absent` | `vm-observed` |
| 32 | `operator96-032-interruptsteeringflags-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\InterruptSteeringFlags` | `1` | `absent` | `vm-observed` |
| 32 | `operator96-032-interruptsteeringflags-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\InterruptSteeringFlags` | `0` | `absent` | `vm-observed` |
| 33 | `operator96-033-alwaystrackioboosting-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\AlwaysTrackIoBoosting` | `0` | `absent` | `vm-observed` |
| 33 | `operator96-033-alwaystrackioboosting-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\AlwaysTrackIoBoosting` | `1` | `absent` | `vm-observed` |
| 34 | `operator96-034-disablecontrolflowguardexportsuppression-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\DisableControlFlowGuardExportSuppression` | `1` | `absent` | `vm-observed` |
| 34 | `operator96-034-disablecontrolflowguardexportsuppression-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\DisableControlFlowGuardExportSuppression` | `0` | `absent` | `vm-observed` |
| 35 | `operator96-035-maximumcooperativeidlesearchwidth-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\MaximumCooperativeIdleSearchWidth` | `0` | `absent` | `vm-observed` |
| 35 | `operator96-035-maximumcooperativeidlesearchwidth-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\MaximumCooperativeIdleSearchWidth` | `1` | `absent` | `vm-observed` |
| 36 | `operator96-036-hiberbootenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\HiberbootEnabled` | `0` | `1` | `vm-observed` |
| 37 | `operator96-037-powersettingprofile-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\PowerSettingProfile` | `1` | `0` | `vm-observed` |
| 38 | `operator96-038-watchdogresumetimeout-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\WatchdogResumeTimeout` | `0` | `120` | `vm-observed` |
| 38 | `operator96-038-watchdogresumetimeout-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\WatchdogResumeTimeout` | `1` | `120` | `vm-observed` |
| 39 | `operator96-039-watchdogsleeptimeout-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\WatchdogSleepTimeout` | `0` | `300` | `vm-observed` |
| 39 | `operator96-039-watchdogsleeptimeout-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\WatchdogSleepTimeout` | `1` | `300` | `vm-observed` |
| 40 | `operator96-040-skiptickoverride-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\SkipTickOverride` | `0` | `absent` | `vm-observed` |
| 40 | `operator96-040-skiptickoverride-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\SkipTickOverride` | `1` | `absent` | `vm-observed` |
| 41 | `operator96-041-win32calloutwatchdogbugcheckenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\Win32CalloutWatchdogBugcheckEnabled` | `0` | `absent` | `vm-observed` |
| 41 | `operator96-041-win32calloutwatchdogbugcheckenabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\Win32CalloutWatchdogBugcheckEnabled` | `1` | `absent` | `vm-observed` |
| 42 | `operator96-042-idlescaninterval-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\IdleScanInterval` | `0` | `absent` | `vm-observed` |
| 42 | `operator96-042-idlescaninterval-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\IdleScanInterval` | `1` | `absent` | `vm-observed` |
| 43 | `operator96-043-sleepstudydisabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\SleepStudyDisabled` | `1` | `absent` | `vm-observed` |
| 43 | `operator96-043-sleepstudydisabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power\SleepStudyDisabled` | `0` | `absent` | `vm-observed` |
| 44 | `operator96-044-class1initialunparkcount-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\Class1InitialUnparkCount` | `0` | `64` | `vm-observed` |
| 44 | `operator96-044-class1initialunparkcount-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\Class1InitialUnparkCount` | `1` | `64` | `vm-observed` |
| 45 | `operator96-045-customizeduringsetup-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\CustomizeDuringSetup` | `0` | `1` | `vm-observed` |
| 46 | `operator96-046-energyestimationenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnergyEstimationEnabled` | `0` | `1` | `vm-observed` |
| 47 | `operator96-047-hiberfilesizepercent-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HiberFileSizePercent` | `1` | `0` | `vm-observed` |
| 48 | `operator96-048-mfbufferingthreshold-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\MfBufferingThreshold` | `1` | `0` | `vm-observed` |
| 49 | `operator96-049-perfcalculateactualutilization-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfCalculateActualUtilization` | `0` | `1` | `vm-observed` |
| 50 | `operator96-050-sourcesettingsversion-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\SourceSettingsVersion` | `0` | `4` | `vm-observed` |
| 50 | `operator96-050-sourcesettingsversion-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\SourceSettingsVersion` | `1` | `4` | `vm-observed` |
| 51 | `operator96-051-timerrebasethresholdondripsexit-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\TimerRebaseThresholdOnDripsExit` | `0` | `60` | `vm-observed` |
| 51 | `operator96-051-timerrebasethresholdondripsexit-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\TimerRebaseThresholdOnDripsExit` | `1` | `60` | `vm-observed` |
| 52 | `operator96-052-hibernateenableddefault-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HibernateEnabledDefault` | `0` | `1` | `vm-observed` |
| 53 | `operator96-053-eventprocessorenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EventProcessorEnabled` | `0` | `1` | `vm-observed` |
| 54 | `operator96-054-lidreliabilitystate-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\LidReliabilityState` | `0` | `1` | `vm-observed` |
| 55 | `operator96-055-hibernateenabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HibernateEnabled` | `1` | `0` | `vm-observed` |
| 56 | `operator96-056-disableinboxpepgeneratedconstraints-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableInboxPepGeneratedConstraints` | `1` | `absent` | `vm-observed` |
| 56 | `operator96-056-disableinboxpepgeneratedconstraints-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableInboxPepGeneratedConstraints` | `0` | `absent` | `vm-observed` |
| 57 | `operator96-057-disabledisplayburstonpowersourcechange-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableDisplayBurstOnPowerSourceChange` | `1` | `absent` | `vm-observed` |

## Results

| Experiment | Status | Hard smoke | Interactive | Post-reboot CPU single Δ% | Post-reboot CPU multi Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---:|---:|---:|---|
| `operator96-001-enablelocallogonsid-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `2.77` | `25.97` | `-17.58` | `registry-research-framework/audit/registry-value-experiments/operator96-001-enablelocallogonsid-0.json` |
| `operator96-001-enablelocallogonsid-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `-11.97` | `-18.74` | `46.31` | `registry-research-framework/audit/registry-value-experiments/operator96-001-enablelocallogonsid-1.json` |
| `operator96-002-enablevirtualization-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `0.72` | `16.85` | `121.02` | `registry-research-framework/audit/registry-value-experiments/operator96-002-enablevirtualization-0.json` |
| `operator96-003-additionalcriticalworkerthreads-5` | `skipped-existing-ok` | `True` | `ok`/`0` | `3.5` | `6.69` | `2.57` | `registry-research-framework/audit/registry-value-experiments/operator96-003-additionalcriticalworkerthreads-5.json` |
| `operator96-003-additionalcriticalworkerthreads-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `6.56` | `0.3` | `-40.0` | `registry-research-framework/audit/registry-value-experiments/operator96-003-additionalcriticalworkerthreads-1.json` |
| `operator96-004-additionaldelayedworkerthreads-5` | `skipped-existing-ok` | `True` | `ok`/`0` | `-5.43` | `-6.47` | `-18.87` | `registry-research-framework/audit/registry-value-experiments/operator96-004-additionaldelayedworkerthreads-5.json` |
| `operator96-004-additionaldelayedworkerthreads-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `-3.85` | `2.56` | `7.38` | `registry-research-framework/audit/registry-value-experiments/operator96-004-additionaldelayedworkerthreads-1.json` |
| `operator96-005-uuidsequencenumber-3322358` | `skipped-existing-ok` | `True` | `ok`/`0` | `9.63` | `3.47` | `-76.71` | `registry-research-framework/audit/registry-value-experiments/operator96-005-uuidsequencenumber-3322358.json` |
| `operator96-005-uuidsequencenumber-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `3.08` | `-9.6` | `12.84` | `registry-research-framework/audit/registry-value-experiments/operator96-005-uuidsequencenumber-0.json` |
| `operator96-006-tickcountrolloverdelay-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `7.52` | `1.84` | `5.47` | `registry-research-framework/audit/registry-value-experiments/operator96-006-tickcountrolloverdelay-0.json` |
| `operator96-006-tickcountrolloverdelay-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `-14.33` | `-17.49` | `-75.12` | `registry-research-framework/audit/registry-value-experiments/operator96-006-tickcountrolloverdelay-1.json` |
| `operator96-007-kernelworkertestflags-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `0.5` | `13.21` | `12.16` | `registry-research-framework/audit/registry-value-experiments/operator96-007-kernelworkertestflags-0.json` |
| `operator96-007-kernelworkertestflags-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `3.48` | `11.32` | `197.74` | `registry-research-framework/audit/registry-value-experiments/operator96-007-kernelworkertestflags-1.json` |
| `operator96-008-maximumkernelworkerthreads-25000` | `skipped-existing-ok` | `True` | `ok`/`0` | `1.85` | `43.07` | `400.5` | `registry-research-framework/audit/registry-value-experiments/operator96-008-maximumkernelworkerthreads-25000.json` |
| `operator96-008-maximumkernelworkerthreads-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `-4.75` | `-5.22` | `15.06` | `registry-research-framework/audit/registry-value-experiments/operator96-008-maximumkernelworkerthreads-0.json` |
| `operator96-009-forceenablemutantautoboost-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `0.95` | `2.95` | `-14.09` | `registry-research-framework/audit/registry-value-experiments/operator96-009-forceenablemutantautoboost-1.json` |
| `operator96-009-forceenablemutantautoboost-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `5.01` | `9.41` | `-40.86` | `registry-research-framework/audit/registry-value-experiments/operator96-009-forceenablemutantautoboost-0.json` |
| `operator96-010-allowremotedasd-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `-28.26` | `-54.84` | `-31.79` | `registry-research-framework/audit/registry-value-experiments/operator96-010-allowremotedasd-1.json` |
| `operator96-011-disablediskcounters-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `0.97` | `-0.12` | `-40.04` | `registry-research-framework/audit/registry-value-experiments/operator96-011-disablediskcounters-1.json` |
| `operator96-011-disablediskcounters-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `-1.83` | `-5.88` | `-23.18` | `registry-research-framework/audit/registry-value-experiments/operator96-011-disablediskcounters-0.json` |
| `operator96-012-ioallowloadcrashdumpdriver-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `-14.21` | `-18.96` | `6.16` | `registry-research-framework/audit/registry-value-experiments/operator96-012-ioallowloadcrashdumpdriver-0.json` |
| `operator96-012-ioallowloadcrashdumpdriver-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `-0.33` | `-1.03` | `-32.57` | `registry-research-framework/audit/registry-value-experiments/operator96-012-ioallowloadcrashdumpdriver-1.json` |
| `operator96-013-ioenablesessionzeroaccesscheck-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `-2.63` | `2.54` | `82.82` | `registry-research-framework/audit/registry-value-experiments/operator96-013-ioenablesessionzeroaccesscheck-1.json` |
| `operator96-013-ioenablesessionzeroaccesscheck-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `3.66` | `1.14` | `-2.67` | `registry-research-framework/audit/registry-value-experiments/operator96-013-ioenablesessionzeroaccesscheck-0.json` |
| `operator96-014-globaltimerresolutionrequests-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `1.83` | `0.5` | `4.89` | `registry-research-framework/audit/registry-value-experiments/operator96-014-globaltimerresolutionrequests-1.json` |
| `operator96-014-globaltimerresolutionrequests-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `3.17` | `26.82` | `4.04` | `registry-research-framework/audit/registry-value-experiments/operator96-014-globaltimerresolutionrequests-0.json` |
| `operator96-015-forceparkingrequested-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `0.25` | `2.15` | `-3.26` | `registry-research-framework/audit/registry-value-experiments/operator96-015-forceparkingrequested-0.json` |
| `operator96-015-forceparkingrequested-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `0.25` | `0.55` | `81.79` | `registry-research-framework/audit/registry-value-experiments/operator96-015-forceparkingrequested-1.json` |
| `operator96-016-enableweruserreporting-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `1.4` | `-0.92` | `-2.5` | `registry-research-framework/audit/registry-value-experiments/operator96-016-enableweruserreporting-0.json` |
| `operator96-016-enableweruserreporting-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `0.74` | `1.2` | `418.78` | `registry-research-framework/audit/registry-value-experiments/operator96-016-enableweruserreporting-1.json` |
| `operator96-017-hyperstartdisabled-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `2.78` | `1.44` | `-37.56` | `registry-research-framework/audit/registry-value-experiments/operator96-017-hyperstartdisabled-1.json` |
| `operator96-017-hyperstartdisabled-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `1.15` | `0.51` | `-64.85` | `registry-research-framework/audit/registry-value-experiments/operator96-017-hyperstartdisabled-0.json` |
| `operator96-018-disablelightweightsuspend-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `-1.88` | `-5.12` | `81.82` | `registry-research-framework/audit/registry-value-experiments/operator96-018-disablelightweightsuspend-0.json` |
| `operator96-018-disablelightweightsuspend-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `0.46` | `1.11` | `1.75` | `registry-research-framework/audit/registry-value-experiments/operator96-018-disablelightweightsuspend-1.json` |
| `operator96-019-timercheckflags-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `5.29` | `2.29` | `-54.73` | `registry-research-framework/audit/registry-value-experiments/operator96-019-timercheckflags-0.json` |
| `operator96-019-timercheckflags-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `0.94` | `-0.05` | `3.28` | `registry-research-framework/audit/registry-value-experiments/operator96-019-timercheckflags-1.json` |
| `operator96-020-forceidlegraceperiod-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `2.34` | `-1.34` | `6.31` | `registry-research-framework/audit/registry-value-experiments/operator96-020-forceidlegraceperiod-0.json` |
| `operator96-020-forceidlegraceperiod-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `2.1` | `1.2` | `-11.71` | `registry-research-framework/audit/registry-value-experiments/operator96-020-forceidlegraceperiod-1.json` |
| `operator96-021-disableexceptionchainvalidation-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `-0.68` | `0.28` | `2.85` | `registry-research-framework/audit/registry-value-experiments/operator96-021-disableexceptionchainvalidation-1.json` |
| `operator96-021-disableexceptionchainvalidation-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `1.47` | `-0.41` | `-0.0` | `registry-research-framework/audit/registry-value-experiments/operator96-021-disableexceptionchainvalidation-0.json` |
| `operator96-022-maxdynamictickduration-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `15.01` | `-31.64` | `331.06` | `registry-research-framework/audit/registry-value-experiments/operator96-022-maxdynamictickduration-1.json` |
| `operator96-022-maxdynamictickduration-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `-2.64` | `0.54` | `-68.25` | `registry-research-framework/audit/registry-value-experiments/operator96-022-maxdynamictickduration-0.json` |
| `operator96-023-enabletickaccumulationfromaccountingperiods-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `1.09` | `-3.26` | `1.5` | `registry-research-framework/audit/registry-value-experiments/operator96-023-enabletickaccumulationfromaccountingperiods-1.json` |
| `operator96-023-enabletickaccumulationfromaccountingperiods-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `7.73` | `7.09` | `-6.19` | `registry-research-framework/audit/registry-value-experiments/operator96-023-enabletickaccumulationfromaccountingperiods-0.json` |
| `operator96-024-enablepercpuclocktickscheduling-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `8.56` | `13.74` | `-38.37` | `registry-research-framework/audit/registry-value-experiments/operator96-024-enablepercpuclocktickscheduling-1.json` |
| `operator96-024-enablepercpuclocktickscheduling-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `25.14` | `41.69` | `-52.06` | `registry-research-framework/audit/registry-value-experiments/operator96-024-enablepercpuclocktickscheduling-0.json` |
| `operator96-025-serializetimerexpiration-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `-0.36` | `-1.63` | `36.99` | `registry-research-framework/audit/registry-value-experiments/operator96-025-serializetimerexpiration-0.json` |
| `operator96-025-serializetimerexpiration-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `0.5` | `25.84` | `3.31` | `registry-research-framework/audit/registry-value-experiments/operator96-025-serializetimerexpiration-1.json` |
| `operator96-026-xstatecontextlookasideperprocmaxdepth-1024` | `skipped-existing-ok` | `True` | `ok`/`0` | `0.04` | `-0.92` | `-20.96` | `registry-research-framework/audit/registry-value-experiments/operator96-026-xstatecontextlookasideperprocmaxdepth-1024.json` |
| `operator96-026-xstatecontextlookasideperprocmaxdepth-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `19.36` | `12.77` | `46.96` | `registry-research-framework/audit/registry-value-experiments/operator96-026-xstatecontextlookasideperprocmaxdepth-0.json` |
| `operator96-027-longdpcqueuethreshold-2` | `skipped-existing-ok` | `True` | `ok`/`0` | `40.23` | `43.04` | `20.1` | `registry-research-framework/audit/registry-value-experiments/operator96-027-longdpcqueuethreshold-2.json` |
| `operator96-027-longdpcqueuethreshold-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `18.55` | `15.19` | `-1.12` | `registry-research-framework/audit/registry-value-experiments/operator96-027-longdpcqueuethreshold-0.json` |
| `operator96-028-longdpcruntimethreshold-50` | `skipped-existing-ok` | `True` | `ok`/`0` | `14.83` | `-6.32` | `-58.11` | `registry-research-framework/audit/registry-value-experiments/operator96-028-longdpcruntimethreshold-50.json` |
| `operator96-028-longdpcruntimethreshold-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `4.27` | `-11.19` | `-13.22` | `registry-research-framework/audit/registry-value-experiments/operator96-028-longdpcruntimethreshold-0.json` |
| `operator96-029-forcebugcheckfordpcwatchdog-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `0.84` | `-16.1` | `-13.14` | `registry-research-framework/audit/registry-value-experiments/operator96-029-forcebugcheckfordpcwatchdog-0.json` |
| `operator96-029-forcebugcheckfordpcwatchdog-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `0.09` | `5.56` | `-80.55` | `registry-research-framework/audit/registry-value-experiments/operator96-029-forcebugcheckfordpcwatchdog-1.json` |
| `operator96-030-forceforegroundboostdecay-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `8.41` | `-21.07` | `3.67` | `registry-research-framework/audit/registry-value-experiments/operator96-030-forceforegroundboostdecay-0.json` |
| `operator96-030-forceforegroundboostdecay-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `18.1` | `-2.95` | `-69.61` | `registry-research-framework/audit/registry-value-experiments/operator96-030-forceforegroundboostdecay-1.json` |
| `operator96-031-rebalanceminpriority-16` | `skipped-existing-ok` | `True` | `ok`/`0` | `1.03` | `-42.02` | `-8.48` | `registry-research-framework/audit/registry-value-experiments/operator96-031-rebalanceminpriority-16.json` |
| `operator96-031-rebalanceminpriority-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `1.13` | `1.3` | `1.01` | `registry-research-framework/audit/registry-value-experiments/operator96-031-rebalanceminpriority-0.json` |
| `operator96-032-interruptsteeringflags-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `34.58` | `74.0` | `-6.48` | `registry-research-framework/audit/registry-value-experiments/operator96-032-interruptsteeringflags-1.json` |
| `operator96-032-interruptsteeringflags-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `7.35` | `-3.28` | `286.55` | `registry-research-framework/audit/registry-value-experiments/operator96-032-interruptsteeringflags-0.json` |
| `operator96-033-alwaystrackioboosting-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `9.39` | `-40.92` | `349.21` | `registry-research-framework/audit/registry-value-experiments/operator96-033-alwaystrackioboosting-0.json` |
| `operator96-033-alwaystrackioboosting-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `0.53` | `-12.52` | `12.83` | `registry-research-framework/audit/registry-value-experiments/operator96-033-alwaystrackioboosting-1.json` |
| `operator96-034-disablecontrolflowguardexportsuppression-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `26.65` | `36.9` | `0.23` | `registry-research-framework/audit/registry-value-experiments/operator96-034-disablecontrolflowguardexportsuppression-1.json` |
| `operator96-034-disablecontrolflowguardexportsuppression-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `34.17` | `11.05` | `-55.31` | `registry-research-framework/audit/registry-value-experiments/operator96-034-disablecontrolflowguardexportsuppression-0.json` |
| `operator96-035-maximumcooperativeidlesearchwidth-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `24.33` | `12.33` | `-14.49` | `registry-research-framework/audit/registry-value-experiments/operator96-035-maximumcooperativeidlesearchwidth-0.json` |
| `operator96-035-maximumcooperativeidlesearchwidth-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `23.5` | `7.21` | `-40.06` | `registry-research-framework/audit/registry-value-experiments/operator96-035-maximumcooperativeidlesearchwidth-1.json` |
| `operator96-036-hiberbootenabled-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `6.49` | `3.6` | `-73.85` | `registry-research-framework/audit/registry-value-experiments/operator96-036-hiberbootenabled-0.json` |
| `operator96-037-powersettingprofile-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `15.32` | `36.47` | `-62.08` | `registry-research-framework/audit/registry-value-experiments/operator96-037-powersettingprofile-1.json` |
| `operator96-038-watchdogresumetimeout-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `17.52` | `5.06` | `-40.05` | `registry-research-framework/audit/registry-value-experiments/operator96-038-watchdogresumetimeout-0.json` |
| `operator96-038-watchdogresumetimeout-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `2.89` | `27.22` | `6.68` | `registry-research-framework/audit/registry-value-experiments/operator96-038-watchdogresumetimeout-1.json` |
| `operator96-039-watchdogsleeptimeout-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `34.63` | `2.16` | `42.25` | `registry-research-framework/audit/registry-value-experiments/operator96-039-watchdogsleeptimeout-0.json` |
| `operator96-039-watchdogsleeptimeout-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `4.24` | `8.76` | `-15.51` | `registry-research-framework/audit/registry-value-experiments/operator96-039-watchdogsleeptimeout-1.json` |
| `operator96-040-skiptickoverride-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `17.87` | `4.05` | `-35.88` | `registry-research-framework/audit/registry-value-experiments/operator96-040-skiptickoverride-0.json` |
| `operator96-040-skiptickoverride-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `36.85` | `49.61` | `-75.22` | `registry-research-framework/audit/registry-value-experiments/operator96-040-skiptickoverride-1.json` |
| `operator96-041-win32calloutwatchdogbugcheckenabled-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `1.67` | `0.17` | `-2.69` | `registry-research-framework/audit/registry-value-experiments/operator96-041-win32calloutwatchdogbugcheckenabled-0.json` |
| `operator96-041-win32calloutwatchdogbugcheckenabled-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `-3.24` | `-7.71` | `17.89` | `registry-research-framework/audit/registry-value-experiments/operator96-041-win32calloutwatchdogbugcheckenabled-1.json` |
| `operator96-042-idlescaninterval-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `4.97` | `1.68` | `-81.83` | `registry-research-framework/audit/registry-value-experiments/operator96-042-idlescaninterval-0.json` |
| `operator96-042-idlescaninterval-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `0.79` | `44.72` | `10.27` | `registry-research-framework/audit/registry-value-experiments/operator96-042-idlescaninterval-1.json` |
| `operator96-043-sleepstudydisabled-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `8.31` | `12.32` | `-1.84` | `registry-research-framework/audit/registry-value-experiments/operator96-043-sleepstudydisabled-1.json` |
| `operator96-043-sleepstudydisabled-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `0.37` | `-33.22` | `2.17` | `registry-research-framework/audit/registry-value-experiments/operator96-043-sleepstudydisabled-0.json` |
| `operator96-044-class1initialunparkcount-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `2.23` | `2.05` | `-1.12` | `registry-research-framework/audit/registry-value-experiments/operator96-044-class1initialunparkcount-0.json` |
| `operator96-044-class1initialunparkcount-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `0.55` | `-6.23` | `337.13` | `registry-research-framework/audit/registry-value-experiments/operator96-044-class1initialunparkcount-1.json` |
| `operator96-045-customizeduringsetup-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `21.04` | `7.04` | `1.08` | `registry-research-framework/audit/registry-value-experiments/operator96-045-customizeduringsetup-0.json` |
| `operator96-046-energyestimationenabled-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `2.59` | `2.94` | `-3.84` | `registry-research-framework/audit/registry-value-experiments/operator96-046-energyestimationenabled-0.json` |
| `operator96-047-hiberfilesizepercent-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `4.96` | `6.21` | `-26.59` | `registry-research-framework/audit/registry-value-experiments/operator96-047-hiberfilesizepercent-1.json` |
| `operator96-048-mfbufferingthreshold-1` | `skipped-existing-ok` | `True` | `ok`/`0` | `1.79` | `7.58` | `7.78` | `registry-research-framework/audit/registry-value-experiments/operator96-048-mfbufferingthreshold-1.json` |
| `operator96-049-perfcalculateactualutilization-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `3.81` | `52.9` | `10.02` | `registry-research-framework/audit/registry-value-experiments/operator96-049-perfcalculateactualutilization-0.json` |
| `operator96-050-sourcesettingsversion-0` | `skipped-existing-ok` | `True` | `ok`/`0` | `-0.8` | `2.12` | `236.91` | `registry-research-framework/audit/registry-value-experiments/operator96-050-sourcesettingsversion-0.json` |
| `operator96-050-sourcesettingsversion-1` | `ok` | `True` | `ok`/`0` | `20.89` | `0.25` | `-15.9` | `registry-research-framework/audit/registry-value-experiments/operator96-050-sourcesettingsversion-1.json` |
| `operator96-051-timerrebasethresholdondripsexit-0` | `ok` | `True` | `ok`/`0` | `2.07` | `15.76` | `-60.73` | `registry-research-framework/audit/registry-value-experiments/operator96-051-timerrebasethresholdondripsexit-0.json` |
| `operator96-051-timerrebasethresholdondripsexit-1` | `ok` | `True` | `ok`/`0` | `0.16` | `10.75` | `-33.1` | `registry-research-framework/audit/registry-value-experiments/operator96-051-timerrebasethresholdondripsexit-1.json` |
| `operator96-052-hibernateenableddefault-0` | `ok` | `True` | `ok`/`0` | `0.84` | `2.8` | `-1.31` | `registry-research-framework/audit/registry-value-experiments/operator96-052-hibernateenableddefault-0.json` |
| `operator96-053-eventprocessorenabled-0` | `ok` | `True` | `ok`/`0` | `8.11` | `74.18` | `-61.15` | `registry-research-framework/audit/registry-value-experiments/operator96-053-eventprocessorenabled-0.json` |
| `operator96-054-lidreliabilitystate-0` | `ok` | `True` | `ok`/`0` | `13.89` | `4.05` | `-11.86` | `registry-research-framework/audit/registry-value-experiments/operator96-054-lidreliabilitystate-0.json` |
| `operator96-055-hibernateenabled-1` | `ok` | `True` | `ok`/`0` | `7.13` | `-1.5` | `55.63` | `registry-research-framework/audit/registry-value-experiments/operator96-055-hibernateenabled-1.json` |
| `operator96-056-disableinboxpepgeneratedconstraints-1` | `ok` | `True` | `ok`/`0` | `3.82` | `-6.21` | `43.1` | `registry-research-framework/audit/registry-value-experiments/operator96-056-disableinboxpepgeneratedconstraints-1.json` |
| `operator96-056-disableinboxpepgeneratedconstraints-0` | `ok` | `True` | `ok`/`0` | `18.07` | `-0.44` | `-6.42` | `registry-research-framework/audit/registry-value-experiments/operator96-056-disableinboxpepgeneratedconstraints-0.json` |
| `operator96-057-disabledisplayburstonpowersourcechange-1` | `ok` | `True` | `ok`/`0` | `1.52` | `-2.43` | `27.23` | `registry-research-framework/audit/registry-value-experiments/operator96-057-disabledisplayburstonpowersourcechange-1.json` |
