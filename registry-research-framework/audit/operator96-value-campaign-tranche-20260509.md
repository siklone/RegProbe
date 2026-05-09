# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-09T12:51:26Z`
- Status: **ok**
- Planned experiments: `40`
- Completed in this run: `40`

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
| `operator96-017-hyperstartdisabled-1` | `ok` | `True` | `ok`/`0` | `2.78` | `1.44` | `-37.56` | `registry-research-framework/audit/registry-value-experiments/operator96-017-hyperstartdisabled-1.json` |
| `operator96-017-hyperstartdisabled-0` | `ok` | `True` | `ok`/`0` | `1.15` | `0.51` | `-64.85` | `registry-research-framework/audit/registry-value-experiments/operator96-017-hyperstartdisabled-0.json` |
| `operator96-018-disablelightweightsuspend-0` | `ok` | `True` | `ok`/`0` | `-1.88` | `-5.12` | `81.82` | `registry-research-framework/audit/registry-value-experiments/operator96-018-disablelightweightsuspend-0.json` |
| `operator96-018-disablelightweightsuspend-1` | `ok` | `True` | `ok`/`0` | `0.46` | `1.11` | `1.75` | `registry-research-framework/audit/registry-value-experiments/operator96-018-disablelightweightsuspend-1.json` |
| `operator96-019-timercheckflags-0` | `ok` | `True` | `ok`/`0` | `5.29` | `2.29` | `-54.73` | `registry-research-framework/audit/registry-value-experiments/operator96-019-timercheckflags-0.json` |
| `operator96-019-timercheckflags-1` | `ok` | `True` | `ok`/`0` | `0.94` | `-0.05` | `3.28` | `registry-research-framework/audit/registry-value-experiments/operator96-019-timercheckflags-1.json` |
| `operator96-020-forceidlegraceperiod-0` | `ok` | `True` | `ok`/`0` | `2.34` | `-1.34` | `6.31` | `registry-research-framework/audit/registry-value-experiments/operator96-020-forceidlegraceperiod-0.json` |
| `operator96-020-forceidlegraceperiod-1` | `ok` | `True` | `ok`/`0` | `2.1` | `1.2` | `-11.71` | `registry-research-framework/audit/registry-value-experiments/operator96-020-forceidlegraceperiod-1.json` |
| `operator96-021-disableexceptionchainvalidation-1` | `ok` | `True` | `ok`/`0` | `-0.68` | `0.28` | `2.85` | `registry-research-framework/audit/registry-value-experiments/operator96-021-disableexceptionchainvalidation-1.json` |
| `operator96-021-disableexceptionchainvalidation-0` | `ok` | `True` | `ok`/`0` | `1.47` | `-0.41` | `-0.0` | `registry-research-framework/audit/registry-value-experiments/operator96-021-disableexceptionchainvalidation-0.json` |
