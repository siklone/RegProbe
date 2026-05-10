# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-10T13:36:32Z`
- Status: **ok**
- Planned experiments: `170`
- Completed in this run: `170`

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

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-001-enablelocallogonsid-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-25.972` | `-17.58` | `registry-research-framework/audit/registry-value-experiments/operator96-001-enablelocallogonsid-0.json` |
| `operator96-001-enablelocallogonsid-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-7.673` | `46.31` | `registry-research-framework/audit/registry-value-experiments/operator96-001-enablelocallogonsid-1.json` |
| `operator96-002-enablevirtualization-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-60.067` | `121.02` | `registry-research-framework/audit/registry-value-experiments/operator96-002-enablevirtualization-0.json` |
| `operator96-003-additionalcriticalworkerthreads-5` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-8.43` | `2.57` | `registry-research-framework/audit/registry-value-experiments/operator96-003-additionalcriticalworkerthreads-5.json` |
| `operator96-003-additionalcriticalworkerthreads-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-39.998` | `-40.0` | `registry-research-framework/audit/registry-value-experiments/operator96-003-additionalcriticalworkerthreads-1.json` |
| `operator96-004-additionaldelayedworkerthreads-5` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-18.867` | `-18.87` | `registry-research-framework/audit/registry-value-experiments/operator96-004-additionaldelayedworkerthreads-5.json` |
| `operator96-004-additionaldelayedworkerthreads-1` | `low_confidence` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `7.377` | `7.38` | `registry-research-framework/audit/registry-value-experiments/operator96-004-additionaldelayedworkerthreads-1.json` |
| `operator96-005-uuidsequencenumber-3322358` | `rollback_failure` | `high` | `unknown` | `ok` | `True` | `ok`/`0` | `None` | `-76.71` | `registry-research-framework/audit/registry-value-experiments/operator96-005-uuidsequencenumber-3322358.json` |
| `operator96-005-uuidsequencenumber-0` | `rollback_failure` | `high` | `unknown` | `ok` | `True` | `ok`/`0` | `None` | `12.84` | `registry-research-framework/audit/registry-value-experiments/operator96-005-uuidsequencenumber-0.json` |
| `operator96-006-tickcountrolloverdelay-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-8.618` | `5.47` | `registry-research-framework/audit/registry-value-experiments/operator96-006-tickcountrolloverdelay-0.json` |
| `operator96-006-tickcountrolloverdelay-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-75.12` | `-75.12` | `registry-research-framework/audit/registry-value-experiments/operator96-006-tickcountrolloverdelay-1.json` |
| `operator96-007-kernelworkertestflags-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-13.207` | `12.16` | `registry-research-framework/audit/registry-value-experiments/operator96-007-kernelworkertestflags-0.json` |
| `operator96-007-kernelworkertestflags-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-11.324` | `197.74` | `registry-research-framework/audit/registry-value-experiments/operator96-007-kernelworkertestflags-1.json` |
| `operator96-008-maximumkernelworkerthreads-25000` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-85.354` | `400.5` | `registry-research-framework/audit/registry-value-experiments/operator96-008-maximumkernelworkerthreads-25000.json` |
| `operator96-008-maximumkernelworkerthreads-0` | `low_confidence` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `15.065` | `15.06` | `registry-research-framework/audit/registry-value-experiments/operator96-008-maximumkernelworkerthreads-0.json` |
| `operator96-009-forceenablemutantautoboost-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-20.565` | `-14.09` | `registry-research-framework/audit/registry-value-experiments/operator96-009-forceenablemutantautoboost-1.json` |
| `operator96-009-forceenablemutantautoboost-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-40.855` | `-40.86` | `registry-research-framework/audit/registry-value-experiments/operator96-009-forceenablemutantautoboost-0.json` |
| `operator96-010-allowremotedasd-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-31.785` | `-31.79` | `registry-research-framework/audit/registry-value-experiments/operator96-010-allowremotedasd-1.json` |
| `operator96-011-disablediskcounters-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-40.044` | `-40.04` | `registry-research-framework/audit/registry-value-experiments/operator96-011-disablediskcounters-1.json` |
| `operator96-011-disablediskcounters-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-23.177` | `-23.18` | `registry-research-framework/audit/registry-value-experiments/operator96-011-disablediskcounters-0.json` |
| `operator96-012-ioallowloadcrashdumpdriver-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-78.499` | `6.16` | `registry-research-framework/audit/registry-value-experiments/operator96-012-ioallowloadcrashdumpdriver-0.json` |
| `operator96-012-ioallowloadcrashdumpdriver-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-32.566` | `-32.57` | `registry-research-framework/audit/registry-value-experiments/operator96-012-ioallowloadcrashdumpdriver-1.json` |
| `operator96-013-ioenablesessionzeroaccesscheck-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-13.009` | `82.82` | `registry-research-framework/audit/registry-value-experiments/operator96-013-ioenablesessionzeroaccesscheck-1.json` |
| `operator96-013-ioenablesessionzeroaccesscheck-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-26.366` | `-2.67` | `registry-research-framework/audit/registry-value-experiments/operator96-013-ioenablesessionzeroaccesscheck-0.json` |
| `operator96-014-globaltimerresolutionrequests-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-81.278` | `4.89` | `registry-research-framework/audit/registry-value-experiments/operator96-014-globaltimerresolutionrequests-1.json` |
| `operator96-014-globaltimerresolutionrequests-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-26.823` | `4.04` | `registry-research-framework/audit/registry-value-experiments/operator96-014-globaltimerresolutionrequests-0.json` |
| `operator96-015-forceparkingrequested-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-11.636` | `-3.26` | `registry-research-framework/audit/registry-value-experiments/operator96-015-forceparkingrequested-0.json` |
| `operator96-015-forceparkingrequested-1` | `low_confidence` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `83.648` | `81.79` | `registry-research-framework/audit/registry-value-experiments/operator96-015-forceparkingrequested-1.json` |
| `operator96-016-enableweruserreporting-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-11.318` | `-2.5` | `registry-research-framework/audit/registry-value-experiments/operator96-016-enableweruserreporting-0.json` |
| `operator96-016-enableweruserreporting-1` | `low_confidence` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `464.837` | `418.78` | `registry-research-framework/audit/registry-value-experiments/operator96-016-enableweruserreporting-1.json` |
| `operator96-017-hyperstartdisabled-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-37.558` | `-37.56` | `registry-research-framework/audit/registry-value-experiments/operator96-017-hyperstartdisabled-1.json` |
| `operator96-017-hyperstartdisabled-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-64.845` | `-64.85` | `registry-research-framework/audit/registry-value-experiments/operator96-017-hyperstartdisabled-0.json` |
| `operator96-018-disablelightweightsuspend-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-13.173` | `81.82` | `registry-research-framework/audit/registry-value-experiments/operator96-018-disablelightweightsuspend-0.json` |
| `operator96-018-disablelightweightsuspend-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-9.348` | `1.75` | `registry-research-framework/audit/registry-value-experiments/operator96-018-disablelightweightsuspend-1.json` |
| `operator96-019-timercheckflags-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-54.73` | `-54.73` | `registry-research-framework/audit/registry-value-experiments/operator96-019-timercheckflags-0.json` |
| `operator96-019-timercheckflags-1` | `low_confidence` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `3.282` | `3.28` | `registry-research-framework/audit/registry-value-experiments/operator96-019-timercheckflags-1.json` |
| `operator96-020-forceidlegraceperiod-0` | `low_confidence` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `6.307` | `6.31` | `registry-research-framework/audit/registry-value-experiments/operator96-020-forceidlegraceperiod-0.json` |
| `operator96-020-forceidlegraceperiod-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-62.399` | `-11.71` | `registry-research-framework/audit/registry-value-experiments/operator96-020-forceidlegraceperiod-1.json` |
| `operator96-021-disableexceptionchainvalidation-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-35.947` | `2.85` | `registry-research-framework/audit/registry-value-experiments/operator96-021-disableexceptionchainvalidation-1.json` |
| `operator96-021-disableexceptionchainvalidation-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-40.556` | `-0.0` | `registry-research-framework/audit/registry-value-experiments/operator96-021-disableexceptionchainvalidation-0.json` |
| `operator96-022-maxdynamictickduration-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-15.008` | `331.06` | `registry-research-framework/audit/registry-value-experiments/operator96-022-maxdynamictickduration-1.json` |
| `operator96-022-maxdynamictickduration-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-68.253` | `-68.25` | `registry-research-framework/audit/registry-value-experiments/operator96-022-maxdynamictickduration-0.json` |
| `operator96-023-enabletickaccumulationfromaccountingperiods-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-17.527` | `1.5` | `registry-research-framework/audit/registry-value-experiments/operator96-023-enabletickaccumulationfromaccountingperiods-1.json` |
| `operator96-023-enabletickaccumulationfromaccountingperiods-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-7.735` | `-6.19` | `registry-research-framework/audit/registry-value-experiments/operator96-023-enabletickaccumulationfromaccountingperiods-0.json` |
| `operator96-024-enablepercpuclocktickscheduling-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-38.372` | `-38.37` | `registry-research-framework/audit/registry-value-experiments/operator96-024-enablepercpuclocktickscheduling-1.json` |
| `operator96-024-enablepercpuclocktickscheduling-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-52.063` | `-52.06` | `registry-research-framework/audit/registry-value-experiments/operator96-024-enablepercpuclocktickscheduling-0.json` |
| `operator96-025-serializetimerexpiration-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-32.631` | `36.99` | `registry-research-framework/audit/registry-value-experiments/operator96-025-serializetimerexpiration-0.json` |
| `operator96-025-serializetimerexpiration-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-25.842` | `3.31` | `registry-research-framework/audit/registry-value-experiments/operator96-025-serializetimerexpiration-1.json` |
| `operator96-026-xstatecontextlookasideperprocmaxdepth-1024` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-20.96` | `-20.96` | `registry-research-framework/audit/registry-value-experiments/operator96-026-xstatecontextlookasideperprocmaxdepth-1024.json` |
| `operator96-026-xstatecontextlookasideperprocmaxdepth-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-24.697` | `46.96` | `registry-research-framework/audit/registry-value-experiments/operator96-026-xstatecontextlookasideperprocmaxdepth-0.json` |
| `operator96-027-longdpcqueuethreshold-2` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-43.044` | `20.1` | `registry-research-framework/audit/registry-value-experiments/operator96-027-longdpcqueuethreshold-2.json` |
| `operator96-027-longdpcqueuethreshold-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-18.551` | `-1.12` | `registry-research-framework/audit/registry-value-experiments/operator96-027-longdpcqueuethreshold-0.json` |
| `operator96-028-longdpcruntimethreshold-50` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-58.11` | `-58.11` | `registry-research-framework/audit/registry-value-experiments/operator96-028-longdpcruntimethreshold-50.json` |
| `operator96-028-longdpcruntimethreshold-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-13.221` | `-13.22` | `registry-research-framework/audit/registry-value-experiments/operator96-028-longdpcruntimethreshold-0.json` |
| `operator96-029-forcebugcheckfordpcwatchdog-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-13.141` | `-13.14` | `registry-research-framework/audit/registry-value-experiments/operator96-029-forcebugcheckfordpcwatchdog-0.json` |
| `operator96-029-forcebugcheckfordpcwatchdog-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-80.551` | `-80.55` | `registry-research-framework/audit/registry-value-experiments/operator96-029-forcebugcheckfordpcwatchdog-1.json` |
| `operator96-030-forceforegroundboostdecay-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-55.177` | `3.67` | `registry-research-framework/audit/registry-value-experiments/operator96-030-forceforegroundboostdecay-0.json` |
| `operator96-030-forceforegroundboostdecay-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-69.611` | `-69.61` | `registry-research-framework/audit/registry-value-experiments/operator96-030-forceforegroundboostdecay-1.json` |
| `operator96-031-rebalanceminpriority-16` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-8.484` | `-8.48` | `registry-research-framework/audit/registry-value-experiments/operator96-031-rebalanceminpriority-16.json` |
| `operator96-031-rebalanceminpriority-0` | `low_confidence` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-5.978` | `1.01` | `registry-research-framework/audit/registry-value-experiments/operator96-031-rebalanceminpriority-0.json` |
| `operator96-032-interruptsteeringflags-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-73.998` | `-6.48` | `registry-research-framework/audit/registry-value-experiments/operator96-032-interruptsteeringflags-1.json` |
| `operator96-032-interruptsteeringflags-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-7.355` | `286.55` | `registry-research-framework/audit/registry-value-experiments/operator96-032-interruptsteeringflags-0.json` |
| `operator96-033-alwaystrackioboosting-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-9.393` | `349.21` | `registry-research-framework/audit/registry-value-experiments/operator96-033-alwaystrackioboosting-0.json` |
| `operator96-033-alwaystrackioboosting-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-41.286` | `12.83` | `registry-research-framework/audit/registry-value-experiments/operator96-033-alwaystrackioboosting-1.json` |
| `operator96-034-disablecontrolflowguardexportsuppression-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-78.753` | `0.23` | `registry-research-framework/audit/registry-value-experiments/operator96-034-disablecontrolflowguardexportsuppression-1.json` |
| `operator96-034-disablecontrolflowguardexportsuppression-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-78.253` | `-55.31` | `registry-research-framework/audit/registry-value-experiments/operator96-034-disablecontrolflowguardexportsuppression-0.json` |
| `operator96-035-maximumcooperativeidlesearchwidth-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-24.327` | `-14.49` | `registry-research-framework/audit/registry-value-experiments/operator96-035-maximumcooperativeidlesearchwidth-0.json` |
| `operator96-035-maximumcooperativeidlesearchwidth-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-40.058` | `-40.06` | `registry-research-framework/audit/registry-value-experiments/operator96-035-maximumcooperativeidlesearchwidth-1.json` |
| `operator96-036-hiberbootenabled-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-73.847` | `-73.85` | `registry-research-framework/audit/registry-value-experiments/operator96-036-hiberbootenabled-0.json` |
| `operator96-037-powersettingprofile-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-62.079` | `-62.08` | `registry-research-framework/audit/registry-value-experiments/operator96-037-powersettingprofile-1.json` |
| `operator96-038-watchdogresumetimeout-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-41.304` | `-40.05` | `registry-research-framework/audit/registry-value-experiments/operator96-038-watchdogresumetimeout-0.json` |
| `operator96-038-watchdogresumetimeout-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-30.316` | `6.68` | `registry-research-framework/audit/registry-value-experiments/operator96-038-watchdogresumetimeout-1.json` |
| `operator96-039-watchdogsleeptimeout-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-60.937` | `42.25` | `registry-research-framework/audit/registry-value-experiments/operator96-039-watchdogsleeptimeout-0.json` |
| `operator96-039-watchdogsleeptimeout-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-15.51` | `-15.51` | `registry-research-framework/audit/registry-value-experiments/operator96-039-watchdogsleeptimeout-1.json` |
| `operator96-040-skiptickoverride-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-35.882` | `-35.88` | `registry-research-framework/audit/registry-value-experiments/operator96-040-skiptickoverride-0.json` |
| `operator96-040-skiptickoverride-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-75.216` | `-75.22` | `registry-research-framework/audit/registry-value-experiments/operator96-040-skiptickoverride-1.json` |
| `operator96-041-win32calloutwatchdogbugcheckenabled-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-12.758` | `-2.69` | `registry-research-framework/audit/registry-value-experiments/operator96-041-win32calloutwatchdogbugcheckenabled-0.json` |
| `operator96-041-win32calloutwatchdogbugcheckenabled-1` | `low_confidence` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `17.894` | `17.89` | `registry-research-framework/audit/registry-value-experiments/operator96-041-win32calloutwatchdogbugcheckenabled-1.json` |
| `operator96-042-idlescaninterval-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-81.833` | `-81.83` | `registry-research-framework/audit/registry-value-experiments/operator96-042-idlescaninterval-0.json` |
| `operator96-042-idlescaninterval-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-44.722` | `10.27` | `registry-research-framework/audit/registry-value-experiments/operator96-042-idlescaninterval-1.json` |
| `operator96-043-sleepstudydisabled-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-12.316` | `-1.84` | `registry-research-framework/audit/registry-value-experiments/operator96-043-sleepstudydisabled-1.json` |
| `operator96-043-sleepstudydisabled-0` | `low_confidence` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `33.216` | `2.17` | `registry-research-framework/audit/registry-value-experiments/operator96-043-sleepstudydisabled-0.json` |
| `operator96-044-class1initialunparkcount-0` | `low_confidence` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `8.623` | `-1.12` | `registry-research-framework/audit/registry-value-experiments/operator96-044-class1initialunparkcount-0.json` |
| `operator96-044-class1initialunparkcount-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-7.572` | `337.13` | `registry-research-framework/audit/registry-value-experiments/operator96-044-class1initialunparkcount-1.json` |
| `operator96-045-customizeduringsetup-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-21.04` | `1.08` | `registry-research-framework/audit/registry-value-experiments/operator96-045-customizeduringsetup-0.json` |
| `operator96-046-energyestimationenabled-0` | `low_confidence` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `8.075` | `-3.84` | `registry-research-framework/audit/registry-value-experiments/operator96-046-energyestimationenabled-0.json` |
| `operator96-047-hiberfilesizepercent-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-32.292` | `-26.59` | `registry-research-framework/audit/registry-value-experiments/operator96-047-hiberfilesizepercent-1.json` |
| `operator96-048-mfbufferingthreshold-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-7.575` | `7.78` | `registry-research-framework/audit/registry-value-experiments/operator96-048-mfbufferingthreshold-1.json` |
| `operator96-049-perfcalculateactualutilization-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-52.9` | `10.02` | `registry-research-framework/audit/registry-value-experiments/operator96-049-perfcalculateactualutilization-0.json` |
| `operator96-050-sourcesettingsversion-0` | `low_confidence` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `338.632` | `236.91` | `registry-research-framework/audit/registry-value-experiments/operator96-050-sourcesettingsversion-0.json` |
| `operator96-050-sourcesettingsversion-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-26.8` | `-15.9` | `registry-research-framework/audit/registry-value-experiments/operator96-050-sourcesettingsversion-1.json` |
| `operator96-051-timerrebasethresholdondripsexit-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-60.729` | `-60.73` | `registry-research-framework/audit/registry-value-experiments/operator96-051-timerrebasethresholdondripsexit-0.json` |
| `operator96-051-timerrebasethresholdondripsexit-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-49.568` | `-33.1` | `registry-research-framework/audit/registry-value-experiments/operator96-051-timerrebasethresholdondripsexit-1.json` |
| `operator96-052-hibernateenableddefault-0` | `low_confidence` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-4.912` | `-1.31` | `registry-research-framework/audit/registry-value-experiments/operator96-052-hibernateenableddefault-0.json` |
| `operator96-053-eventprocessorenabled-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-74.181` | `-61.15` | `registry-research-framework/audit/registry-value-experiments/operator96-053-eventprocessorenabled-0.json` |
| `operator96-054-lidreliabilitystate-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-13.885` | `-11.86` | `registry-research-framework/audit/registry-value-experiments/operator96-054-lidreliabilitystate-0.json` |
| `operator96-055-hibernateenabled-1` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-7.133` | `55.63` | `registry-research-framework/audit/registry-value-experiments/operator96-055-hibernateenabled-1.json` |
| `operator96-056-disableinboxpepgeneratedconstraints-1` | `low_confidence` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `75.751` | `43.1` | `registry-research-framework/audit/registry-value-experiments/operator96-056-disableinboxpepgeneratedconstraints-1.json` |
| `operator96-056-disableinboxpepgeneratedconstraints-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-18.075` | `-6.42` | `registry-research-framework/audit/registry-value-experiments/operator96-056-disableinboxpepgeneratedconstraints-0.json` |
| `operator96-057-disabledisplayburstonpowersourcechange-1` | `low_confidence` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `27.226` | `27.23` | `registry-research-framework/audit/registry-value-experiments/operator96-057-disabledisplayburstonpowersourcechange-1.json` |
| `operator96-057-disabledisplayburstonpowersourcechange-0` | `harmful` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `-61.689` | `243.32` | `registry-research-framework/audit/registry-value-experiments/operator96-057-disabledisplayburstonpowersourcechange-0.json` |
| `operator96-058-idleprocessorsrequireqosmanagement-0` | `low_confidence` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `40.837` | `34.41` | `registry-research-framework/audit/registry-value-experiments/operator96-058-idleprocessorsrequireqosmanagement-0.json` |
| `operator96-058-idleprocessorsrequireqosmanagement-1` | `low_confidence` | `low` | `unknown` | `ok` | `True` | `ok`/`0` | `66.745` | `8.28` | `registry-research-framework/audit/registry-value-experiments/operator96-058-idleprocessorsrequireqosmanagement-1.json` |
| `operator96-059-ttmenabled-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `86.33` | `1.0` | `registry-research-framework/audit/registry-value-experiments/operator96-059-ttmenabled-0.json` |
| `operator96-059-ttmenabled-1` | `boot_failure` | `high` | `unknown` | `ok` | `False` | `missing`/`None` | `None` | `None` | `registry-research-framework/audit/registry-value-experiments/operator96-059-ttmenabled-1.json` |
| `operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-31.054` | `-25.98` | `registry-research-framework/audit/registry-value-experiments/operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-0.json` |
| `operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-73.752` | `27.15` | `registry-research-framework/audit/registry-value-experiments/operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-1.json` |
| `operator96-061-deepiocoalescingenabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-19.12` | `20.52` | `registry-research-framework/audit/registry-value-experiments/operator96-061-deepiocoalescingenabled-0.json` |
| `operator96-061-deepiocoalescingenabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-41.937` | `-36.65` | `registry-research-framework/audit/registry-value-experiments/operator96-061-deepiocoalescingenabled-1.json` |
| `operator96-062-ignorecscompliancecheck-1` | `harmful` | `high` | `ok` | `ok` | `True` | `ok`/`0` | `-8.363` | `8.1` | `registry-research-framework/audit/registry-value-experiments/operator96-062-ignorecscompliancecheck-1.json` |
| `operator96-062-ignorecscompliancecheck-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-32.127` | `-12.92` | `registry-research-framework/audit/registry-value-experiments/operator96-062-ignorecscompliancecheck-0.json` |
| `operator96-063-dripsswhwdivergenceenablelivedump-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-17.886` | `-6.43` | `registry-research-framework/audit/registry-value-experiments/operator96-063-dripsswhwdivergenceenablelivedump-0.json` |
| `operator96-063-dripsswhwdivergenceenablelivedump-1` | `harmful` | `high` | `ok` | `ok` | `True` | `ok`/`0` | `-7.178` | `1.1` | `registry-research-framework/audit/registry-value-experiments/operator96-063-dripsswhwdivergenceenablelivedump-1.json` |
| `operator96-064-disablevsynclatencyupdate-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-45.152` | `1.57` | `registry-research-framework/audit/registry-value-experiments/operator96-064-disablevsynclatencyupdate-1.json` |
| `operator96-064-disablevsynclatencyupdate-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `68.723` | `9.55` | `registry-research-framework/audit/registry-value-experiments/operator96-064-disablevsynclatencyupdate-0.json` |
| `operator96-065-sleepstudyaccountingenabled-0` | `app_breakage` | `medium` | `ok` | `ok` | `True` | `ok`/`1` | `None` | `-6.8` | `registry-research-framework/audit/registry-value-experiments/operator96-065-sleepstudyaccountingenabled-0.json` |
| `operator96-065-sleepstudyaccountingenabled-1` | `harmful` | `high` | `ok` | `ok` | `True` | `ok`/`0` | `-7.421` | `4.32` | `registry-research-framework/audit/registry-value-experiments/operator96-065-sleepstudyaccountingenabled-1.json` |
| `operator96-066-enableinputsuppression-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `67.583` | `8.43` | `registry-research-framework/audit/registry-value-experiments/operator96-066-enableinputsuppression-1.json` |
| `operator96-066-enableinputsuppression-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `82.951` | `2.47` | `registry-research-framework/audit/registry-value-experiments/operator96-066-enableinputsuppression-0.json` |
| `operator96-067-perfchecktimerimplementation-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-10.395` | `8.04` | `registry-research-framework/audit/registry-value-experiments/operator96-067-perfchecktimerimplementation-0.json` |
| `operator96-067-perfchecktimerimplementation-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `141.934` | `26.64` | `registry-research-framework/audit/registry-value-experiments/operator96-067-perfchecktimerimplementation-1.json` |
| `operator96-068-standbyconnectivitygraceperiod-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-20.922` | `-3.71` | `registry-research-framework/audit/registry-value-experiments/operator96-068-standbyconnectivitygraceperiod-0.json` |
| `operator96-068-standbyconnectivitygraceperiod-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-9.525` | `0.93` | `registry-research-framework/audit/registry-value-experiments/operator96-068-standbyconnectivitygraceperiod-1.json` |
| `operator96-069-enforceausteritymode-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-16.572` | `-3.56` | `registry-research-framework/audit/registry-value-experiments/operator96-069-enforceausteritymode-0.json` |
| `operator96-069-enforceausteritymode-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-22.153` | `6.57` | `registry-research-framework/audit/registry-value-experiments/operator96-069-enforceausteritymode-1.json` |
| `operator96-070-alwayscomputeqoshints-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-15.507` | `-4.41` | `registry-research-framework/audit/registry-value-experiments/operator96-070-alwayscomputeqoshints-0.json` |
| `operator96-070-alwayscomputeqoshints-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-31.211` | `112.61` | `registry-research-framework/audit/registry-value-experiments/operator96-070-alwayscomputeqoshints-1.json` |
| `operator96-071-heteromulticoreclassesenabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-15.753` | `-0.74` | `registry-research-framework/audit/registry-value-experiments/operator96-071-heteromulticoreclassesenabled-0.json` |
| `operator96-071-heteromulticoreclassesenabled-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `75.425` | `11.72` | `registry-research-framework/audit/registry-value-experiments/operator96-071-heteromulticoreclassesenabled-1.json` |
| `operator96-072-heteromulticlassparkingenabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-26.067` | `-15.04` | `registry-research-framework/audit/registry-value-experiments/operator96-072-heteromulticlassparkingenabled-0.json` |
| `operator96-072-heteromulticlassparkingenabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-56.508` | `0.49` | `registry-research-framework/audit/registry-value-experiments/operator96-072-heteromulticlassparkingenabled-1.json` |
| `operator96-073-disableidlestatesatboot-2` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `36.84` | `registry-research-framework/audit/registry-value-experiments/operator96-073-disableidlestatesatboot-2.json` |
| `operator96-073-disableidlestatesatboot-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-4.87` | `registry-research-framework/audit/registry-value-experiments/operator96-073-disableidlestatesatboot-0.json` |
| `operator96-074-perfboostatguaranteed-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `2.33` | `registry-research-framework/audit/registry-value-experiments/operator96-074-perfboostatguaranteed-1.json` |
| `operator96-074-perfboostatguaranteed-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-10.147` | `-5.0` | `registry-research-framework/audit/registry-value-experiments/operator96-074-perfboostatguaranteed-0.json` |
| `operator96-075-msdisabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-18.195` | `17.12` | `registry-research-framework/audit/registry-value-experiments/operator96-075-msdisabled-1.json` |
| `operator96-075-msdisabled-0` | `harmful` | `high` | `ok` | `ok` | `True` | `ok`/`0` | `-7.083` | `-3.56` | `registry-research-framework/audit/registry-value-experiments/operator96-075-msdisabled-0.json` |
| `operator96-076-fxaccountingtelemetrydisabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-84.513` | `-14.37` | `registry-research-framework/audit/registry-value-experiments/operator96-076-fxaccountingtelemetrydisabled-1.json` |
| `operator96-076-fxaccountingtelemetrydisabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-13.856` | `-11.85` | `registry-research-framework/audit/registry-value-experiments/operator96-076-fxaccountingtelemetrydisabled-0.json` |
| `operator96-077-win32kcalloutwatchdogtimeoutseconds-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-16.757` | `-0.43` | `registry-research-framework/audit/registry-value-experiments/operator96-077-win32kcalloutwatchdogtimeoutseconds-0.json` |
| `operator96-077-win32kcalloutwatchdogtimeoutseconds-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-7.9` | `registry-research-framework/audit/registry-value-experiments/operator96-077-win32kcalloutwatchdogtimeoutseconds-1.json` |
| `operator96-078-enableminimalhiberfile-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `timeout`/`None` | `None` | `-47.26` | `registry-research-framework/audit/registry-value-experiments/operator96-078-enableminimalhiberfile-0.json` |
| `operator96-078-enableminimalhiberfile-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `32.28` | `registry-research-framework/audit/registry-value-experiments/operator96-078-enableminimalhiberfile-1.json` |
| `operator96-079-hiberbootenabled-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-29.33` | `registry-research-framework/audit/registry-value-experiments/operator96-079-hiberbootenabled-0.json` |
| `operator96-079-hiberbootenabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-21.152` | `-0.68` | `registry-research-framework/audit/registry-value-experiments/operator96-079-hiberbootenabled-1.json` |
| `operator96-080-maximumfrequencyoverride-100` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-70.587` | `-3.59` | `registry-research-framework/audit/registry-value-experiments/operator96-080-maximumfrequencyoverride-100.json` |
| `operator96-080-maximumfrequencyoverride-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-23.36` | `1.85` | `registry-research-framework/audit/registry-value-experiments/operator96-080-maximumfrequencyoverride-0.json` |
| `operator96-081-pofxsystemirpwaitforreportdevicepowered-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-3.5` | `registry-research-framework/audit/registry-value-experiments/operator96-081-pofxsystemirpwaitforreportdevicepowered-0.json` |
| `operator96-081-pofxsystemirpwaitforreportdevicepowered-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `12.76` | `registry-research-framework/audit/registry-value-experiments/operator96-081-pofxsystemirpwaitforreportdevicepowered-1.json` |
| `operator96-082-allowsystemrequiredpowerrequests-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-7.501` | `3.28` | `registry-research-framework/audit/registry-value-experiments/operator96-082-allowsystemrequiredpowerrequests-0.json` |
| `operator96-082-allowsystemrequiredpowerrequests-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `68.858` | `9.11` | `registry-research-framework/audit/registry-value-experiments/operator96-082-allowsystemrequiredpowerrequests-1.json` |
| `operator96-083-coalescingflushinterval-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-18.189` | `47.35` | `registry-research-framework/audit/registry-value-experiments/operator96-083-coalescingflushinterval-0.json` |
| `operator96-083-coalescingflushinterval-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-36.938` | `-31.39` | `registry-research-framework/audit/registry-value-experiments/operator96-083-coalescingflushinterval-1.json` |
| `operator96-084-coalescingtimerinterval-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-12.507` | `-7.59` | `registry-research-framework/audit/registry-value-experiments/operator96-084-coalescingtimerinterval-0.json` |
| `operator96-084-coalescingtimerinterval-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-24.047` | `-6.68` | `registry-research-framework/audit/registry-value-experiments/operator96-084-coalescingtimerinterval-1.json` |
| `operator96-085-heterohgseeperfhintsindependentenabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-25.761` | `-14.86` | `registry-research-framework/audit/registry-value-experiments/operator96-085-heterohgseeperfhintsindependentenabled-1.json` |
| `operator96-085-heterohgseeperfhintsindependentenabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-29.629` | `-4.35` | `registry-research-framework/audit/registry-value-experiments/operator96-085-heterohgseeperfhintsindependentenabled-0.json` |
| `operator96-086-heterohgsplusdisabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-29.761` | `-5.68` | `registry-research-framework/audit/registry-value-experiments/operator96-086-heterohgsplusdisabled-1.json` |
| `operator96-086-heterohgsplusdisabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-27.272` | `-3.5` | `registry-research-framework/audit/registry-value-experiments/operator96-086-heterohgsplusdisabled-0.json` |
| `operator96-087-ipilastclockownerdisable-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-9.916` | `-9.92` | `registry-research-framework/audit/registry-value-experiments/operator96-087-ipilastclockownerdisable-1.json` |
| `operator96-087-ipilastclockownerdisable-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `18.61` | `registry-research-framework/audit/registry-value-experiments/operator96-087-ipilastclockownerdisable-0.json` |
| `operator96-088-powerwatchdogrequestqueuetimeoutmsec-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `14.71` | `registry-research-framework/audit/registry-value-experiments/operator96-088-powerwatchdogrequestqueuetimeoutmsec-0.json` |
| `operator96-088-powerwatchdogrequestqueuetimeoutmsec-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `timeout`/`None` | `None` | `-1.2` | `registry-research-framework/audit/registry-value-experiments/operator96-088-powerwatchdogrequestqueuetimeoutmsec-1.json` |
| `operator96-089-powerwatchdogpocallouttimeoutmsec-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `timeout`/`None` | `None` | `7.67` | `registry-research-framework/audit/registry-value-experiments/operator96-089-powerwatchdogpocallouttimeoutmsec-0.json` |
| `operator96-089-powerwatchdogpocallouttimeoutmsec-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-17.74` | `registry-research-framework/audit/registry-value-experiments/operator96-089-powerwatchdogpocallouttimeoutmsec-1.json` |
| `operator96-090-powerwatchdogpowerongditimeoutmsec-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `timeout`/`None` | `None` | `-7.28` | `registry-research-framework/audit/registry-value-experiments/operator96-090-powerwatchdogpowerongditimeoutmsec-0.json` |
| `operator96-090-powerwatchdogpowerongditimeoutmsec-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `timeout`/`None` | `None` | `-29.84` | `registry-research-framework/audit/registry-value-experiments/operator96-090-powerwatchdogpowerongditimeoutmsec-1.json` |
| `operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `timeout`/`None` | `None` | `-22.83` | `registry-research-framework/audit/registry-value-experiments/operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-0.json` |
| `operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `timeout`/`None` | `None` | `-6.69` | `registry-research-framework/audit/registry-value-experiments/operator96-091-powerwatchdogdwmsyncflushtimeoutmsec-1.json` |
| `operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `timeout`/`None` | `None` | `-17.36` | `registry-research-framework/audit/registry-value-experiments/operator96-092-powerwatchdogdrvsetmonitortimeoutmsec-0.json` |
