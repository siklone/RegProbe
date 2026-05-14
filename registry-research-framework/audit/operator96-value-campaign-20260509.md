# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-09T09:26:03Z`
- Status: **planned**
- Planned experiments: `179`
- Completed in this run: `0`

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
| 57 | `operator96-057-disabledisplayburstonpowersourcechange-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableDisplayBurstOnPowerSourceChange` | `0` | `absent` | `vm-observed` |
| 58 | `operator96-058-idleprocessorsrequireqosmanagement-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IdleProcessorsRequireQosManagement` | `0` | `absent` | `vm-observed` |
| 58 | `operator96-058-idleprocessorsrequireqosmanagement-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IdleProcessorsRequireQosManagement` | `1` | `absent` | `vm-observed` |
| 59 | `operator96-059-ttmenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\TtmEnabled` | `0` | `absent` | `vm-observed` |
| 59 | `operator96-059-ttmenabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\TtmEnabled` | `1` | `absent` | `vm-observed` |
| 60 | `operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AllowAudioToEnableExecutionRequiredPowerRequests` | `0` | `absent` | `vm-observed` |
| 60 | `operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AllowAudioToEnableExecutionRequiredPowerRequests` | `1` | `absent` | `vm-observed` |
| 61 | `operator96-061-deepiocoalescingenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DeepIoCoalescingEnabled` | `0` | `absent` | `vm-observed` |
| 61 | `operator96-061-deepiocoalescingenabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DeepIoCoalescingEnabled` | `1` | `absent` | `vm-observed` |
| 62 | `operator96-062-ignorecscompliancecheck-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IgnoreCsComplianceCheck` | `1` | `absent` | `vm-observed` |
| 62 | `operator96-062-ignorecscompliancecheck-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IgnoreCsComplianceCheck` | `0` | `absent` | `vm-observed` |
| 63 | `operator96-063-dripsswhwdivergenceenablelivedump-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DripsSwHwDivergenceEnableLiveDump` | `0` | `absent` | `vm-observed` |
| 63 | `operator96-063-dripsswhwdivergenceenablelivedump-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DripsSwHwDivergenceEnableLiveDump` | `1` | `absent` | `vm-observed` |
| 64 | `operator96-064-disablevsynclatencyupdate-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableVsyncLatencyUpdate` | `1` | `absent` | `vm-observed` |
| 64 | `operator96-064-disablevsynclatencyupdate-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableVsyncLatencyUpdate` | `0` | `absent` | `vm-observed` |
| 65 | `operator96-065-sleepstudyaccountingenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\SleepstudyAccountingEnabled` | `0` | `absent` | `vm-observed` |
| 65 | `operator96-065-sleepstudyaccountingenabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\SleepstudyAccountingEnabled` | `1` | `absent` | `vm-observed` |
| 66 | `operator96-066-enableinputsuppression-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnableInputSuppression` | `1` | `absent` | `vm-observed` |
| 66 | `operator96-066-enableinputsuppression-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnableInputSuppression` | `0` | `absent` | `vm-observed` |
| 67 | `operator96-067-perfchecktimerimplementation-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfCheckTimerImplementation` | `0` | `absent` | `vm-observed` |
| 67 | `operator96-067-perfchecktimerimplementation-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfCheckTimerImplementation` | `1` | `absent` | `vm-observed` |
| 68 | `operator96-068-standbyconnectivitygraceperiod-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\StandbyConnectivityGracePeriod` | `0` | `absent` | `vm-observed` |
| 68 | `operator96-068-standbyconnectivitygraceperiod-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\StandbyConnectivityGracePeriod` | `1` | `absent` | `vm-observed` |
| 69 | `operator96-069-enforceausteritymode-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnforceAusterityMode` | `0` | `absent` | `vm-observed` |
| 69 | `operator96-069-enforceausteritymode-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnforceAusterityMode` | `1` | `absent` | `vm-observed` |
| 70 | `operator96-070-alwayscomputeqoshints-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AlwaysComputeQosHints` | `0` | `absent` | `vm-observed` |
| 70 | `operator96-070-alwayscomputeqoshints-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AlwaysComputeQosHints` | `1` | `absent` | `vm-observed` |
| 71 | `operator96-071-heteromulticoreclassesenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroMultiCoreClassesEnabled` | `0` | `absent` | `vm-observed` |
| 71 | `operator96-071-heteromulticoreclassesenabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroMultiCoreClassesEnabled` | `1` | `absent` | `vm-observed` |
| 72 | `operator96-072-heteromulticlassparkingenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroMultiClassParkingEnabled` | `0` | `absent` | `vm-observed` |
| 72 | `operator96-072-heteromulticlassparkingenabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroMultiClassParkingEnabled` | `1` | `absent` | `vm-observed` |
| 73 | `operator96-073-disableidlestatesatboot-2` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableIdleStatesAtBoot` | `2` | `absent` | `vm-observed` |
| 73 | `operator96-073-disableidlestatesatboot-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DisableIdleStatesAtBoot` | `0` | `absent` | `vm-observed` |
| 74 | `operator96-074-perfboostatguaranteed-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfBoostAtGuaranteed` | `1` | `absent` | `vm-observed` |
| 74 | `operator96-074-perfboostatguaranteed-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PerfBoostAtGuaranteed` | `0` | `absent` | `vm-observed` |
| 75 | `operator96-075-msdisabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\MSDisabled` | `1` | `absent` | `vm-observed` |
| 75 | `operator96-075-msdisabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\MSDisabled` | `0` | `absent` | `vm-observed` |
| 76 | `operator96-076-fxaccountingtelemetrydisabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\FxAccountingTelemetryDisabled` | `1` | `absent` | `vm-observed` |
| 76 | `operator96-076-fxaccountingtelemetrydisabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\FxAccountingTelemetryDisabled` | `0` | `absent` | `vm-observed` |
| 77 | `operator96-077-win32kcalloutwatchdogtimeoutseconds-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\Win32kCalloutWatchdogTimeoutSeconds` | `0` | `absent` | `vm-observed` |
| 77 | `operator96-077-win32kcalloutwatchdogtimeoutseconds-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\Win32kCalloutWatchdogTimeoutSeconds` | `1` | `absent` | `vm-observed` |
| 78 | `operator96-078-enableminimalhiberfile-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnableMinimalHiberFile` | `0` | `absent` | `vm-observed` |
| 78 | `operator96-078-enableminimalhiberfile-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\EnableMinimalHiberFile` | `1` | `absent` | `vm-observed` |
| 79 | `operator96-079-hiberbootenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HiberbootEnabled` | `0` | `absent` | `vm-observed` |
| 79 | `operator96-079-hiberbootenabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HiberbootEnabled` | `1` | `absent` | `vm-observed` |
| 80 | `operator96-080-maximumfrequencyoverride-100` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\MaximumFrequencyOverride` | `100` | `absent` | `vm-observed` |
| 80 | `operator96-080-maximumfrequencyoverride-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\MaximumFrequencyOverride` | `0` | `absent` | `vm-observed` |
| 81 | `operator96-081-pofxsystemirpwaitforreportdevicepowered-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PoFxSystemIrpWaitForReportDevicePowered` | `0` | `absent` | `vm-observed` |
| 81 | `operator96-081-pofxsystemirpwaitforreportdevicepowered-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PoFxSystemIrpWaitForReportDevicePowered` | `1` | `absent` | `vm-observed` |
| 82 | `operator96-082-allowsystemrequiredpowerrequests-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AllowSystemRequiredPowerRequests` | `0` | `absent` | `vm-observed` |
| 82 | `operator96-082-allowsystemrequiredpowerrequests-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AllowSystemRequiredPowerRequests` | `1` | `absent` | `vm-observed` |
| 83 | `operator96-083-coalescingflushinterval-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\CoalescingFlushInterval` | `0` | `absent` | `vm-observed` |
| 83 | `operator96-083-coalescingflushinterval-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\CoalescingFlushInterval` | `1` | `absent` | `vm-observed` |
| 84 | `operator96-084-coalescingtimerinterval-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\CoalescingTimerInterval` | `0` | `absent` | `vm-observed` |
| 84 | `operator96-084-coalescingtimerinterval-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\CoalescingTimerInterval` | `1` | `absent` | `vm-observed` |
| 85 | `operator96-085-heterohgseeperfhintsindependentenabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroHgsEePerfHintsIndependentEnabled` | `1` | `absent` | `vm-observed` |
| 85 | `operator96-085-heterohgseeperfhintsindependentenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroHgsEePerfHintsIndependentEnabled` | `0` | `absent` | `vm-observed` |
| 86 | `operator96-086-heterohgsplusdisabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroHgsPlusDisabled` | `1` | `absent` | `vm-observed` |
| 86 | `operator96-086-heterohgsplusdisabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\HeteroHgsPlusDisabled` | `0` | `absent` | `vm-observed` |
| 87 | `operator96-087-ipilastclockownerdisable-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IpiLastClockOwnerDisable` | `1` | `absent` | `vm-observed` |
| 87 | `operator96-087-ipilastclockownerdisable-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IpiLastClockOwnerDisable` | `0` | `absent` | `vm-observed` |
| 88 | `operator96-088-powerwatchdogrequestqueuetimeoutmsec-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogRequestQueueTimeoutMsec` | `0` | `absent` | `vm-observed` |
| 88 | `operator96-088-powerwatchdogrequestqueuetimeoutmsec-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogRequestQueueTimeoutMsec` | `1` | `absent` | `vm-observed` |
| 89 | `operator96-089-powerwatchdogpocallouttimeoutmsec-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogPoCalloutTimeoutMsec` | `0` | `absent` | `vm-observed` |
| 89 | `operator96-089-powerwatchdogpocallouttimeoutmsec-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogPoCalloutTimeoutMsec` | `1` | `absent` | `vm-observed` |
| 90 | `operator96-090-powerwatchdogpowerongditimeoutmsec-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogPowerOnGdiTimeoutMsec` | `0` | `absent` | `vm-observed` |
| 90 | `operator96-090-powerwatchdogpowerongditimeoutmsec-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogPowerOnGdiTimeoutMsec` | `1` | `absent` | `vm-observed` |
| 91 | `operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogDwmSyncFlushTimeoutMsec` | `0` | `absent` | `vm-observed` |
| 91 | `operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogDwmSyncFlushTimeoutMsec` | `1` | `absent` | `vm-observed` |
| 92 | `operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogDrvSetMonitorTimeoutMsec` | `0` | `absent` | `vm-observed` |
| 92 | `operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerWatchdogDrvSetMonitorTimeoutMsec` | `1` | `absent` | `vm-observed` |
| 93 | `operator96-093-policy-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\ForceHibernateDisabled\Policy` | `1` | `absent` | `no-authoritative-evidence-for-25h2` |
| 93 | `operator96-093-policy-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\ForceHibernateDisabled\Policy` | `0` | `absent` | `no-authoritative-evidence-for-25h2` |
| 94 | `operator96-094-enabledsnetrefresh-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\ModernSleep\EnableDsNetRefresh` | `0` | `absent` | `vm-observed` |
| 94 | `operator96-094-enabledsnetrefresh-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\ModernSleep\EnableDsNetRefresh` | `1` | `absent` | `vm-observed` |
| 95 | `operator96-095-enabledactions-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\ModernSleep\EnabledActions` | `0` | `absent` | `vm-observed` |
| 95 | `operator96-095-enabledactions-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\ModernSleep\EnabledActions` | `1` | `absent` | `vm-observed` |
| 96 | `operator96-096-powerthrottlingoff-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling\PowerThrottlingOff` | `1` | `absent` | `source-backed-policy-default-absent` |
| 96 | `operator96-096-powerthrottlingoff-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling\PowerThrottlingOff` | `0` | `absent` | `source-backed-policy-default-absent` |
