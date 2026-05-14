# Custom Registry Value Low-Noise Rerun Aggregate

- Generated UTC: `2026-05-14T23:42:42Z`
- Status: `ok`
- Source campaigns: `33`
- Plan entries: `157`
- Results: `157`
- Non-ok: `0`
- Hard smoke all: `False`
- Noisy results: `41`

## Counts

- Verdicts: `{'app_breakage': 1, 'cpu_gain': 2, 'harmful': 79, 'low_confidence': 34, 'noisy': 40, 'rollback_failure': 1}`
- Host noise: `{'noisy': 41, 'ok': 116}`
- Confidence: `{'high': 13, 'low': 137, 'medium': 7}`

## Source Campaigns

| Campaign | Status | Plan | Results | Non-ok |
|---|---|---:|---:|---:|
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260510.json` | `ok` | 8 | 8 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-02-20260510.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-03-20260510.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-04-20260510.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-05-20260510.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-06-20260510.json` | `ok` | 8 | 8 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-07-20260510.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-08-20260510.json` | `ok` | 7 | 7 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-09-20260510.json` | `ok` | 7 | 7 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-10-20260510.json` | `ok` | 7 | 7 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-11-20260510.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-12-20260510.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-13-20260510.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-14-20260510.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-15-20260510.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-16-20260510.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-17-20260510.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-02.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-03.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-04.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-05.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-06.json` | `ok` | 8 | 8 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-07.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-08.json` | `ok` | 7 | 7 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-09.json` | `ok` | 7 | 7 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-10.json` | `ok` | 7 | 7 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-11.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-12.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-13.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-14.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-15.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-16.json` | `ok` | 10 | 10 | 0 |
| `registry-research-framework/audit/operator96-low-noise-rerun-tranche-20260512-17.json` | `ok` | 8 | 8 | 0 |

## Noisy Results

| Experiment | Value | Verdict | Host noise | Artifact |
|---|---|---|---|---|
| `operator96-001-enablelocallogonsid-0` | `EnableLocalLogonSid` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-001-enablelocallogonsid-0.json` |
| `operator96-009-forceenablemutantautoboost-0` | `ForceEnableMutantAutoboost` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-009-forceenablemutantautoboost-0.json` |
| `operator96-010-allowremotedasd-1` | `AllowRemoteDASD` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-010-allowremotedasd-1.json` |
| `operator96-011-disablediskcounters-1` | `DisableDiskCounters` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-011-disablediskcounters-1.json` |
| `operator96-011-disablediskcounters-0` | `DisableDiskCounters` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-011-disablediskcounters-0.json` |
| `operator96-012-ioallowloadcrashdumpdriver-0` | `IoAllowLoadCrashDumpDriver` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-012-ioallowloadcrashdumpdriver-0.json` |
| `operator96-012-ioallowloadcrashdumpdriver-1` | `IoAllowLoadCrashDumpDriver` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-02/operator96-012-ioallowloadcrashdumpdriver-1.json` |
| `operator96-016-enableweruserreporting-0` | `EnableWerUserReporting` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-016-enableweruserreporting-0.json` |
| `operator96-016-enableweruserreporting-1` | `EnableWerUserReporting` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-03/operator96-016-enableweruserreporting-1.json` |
| `operator96-058-idleprocessorsrequireqosmanagement-0` | `IdleProcessorsRequireQosManagement` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-058-idleprocessorsrequireqosmanagement-0.json` |
| `operator96-058-idleprocessorsrequireqosmanagement-1` | `IdleProcessorsRequireQosManagement` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-058-idleprocessorsrequireqosmanagement-1.json` |
| `operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-0` | `AllowAudioToEnableExecutionRequiredPowerRequests` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-0.json` |
| `operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-1` | `AllowAudioToEnableExecutionRequiredPowerRequests` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-1.json` |
| `operator96-064-disablevsynclatencyupdate-1` | `DisableVsyncLatencyUpdate` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-064-disablevsynclatencyupdate-1.json` |
| `operator96-064-disablevsynclatencyupdate-0` | `DisableVsyncLatencyUpdate` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-064-disablevsynclatencyupdate-0.json` |
| `operator96-066-enableinputsuppression-1` | `EnableInputSuppression` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-066-enableinputsuppression-1.json` |
| `operator96-066-enableinputsuppression-0` | `EnableInputSuppression` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-066-enableinputsuppression-0.json` |
| `operator96-067-perfchecktimerimplementation-0` | `PerfCheckTimerImplementation` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-067-perfchecktimerimplementation-0.json` |
| `operator96-067-perfchecktimerimplementation-1` | `PerfCheckTimerImplementation` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-067-perfchecktimerimplementation-1.json` |
| `operator96-068-standbyconnectivitygraceperiod-0` | `StandbyConnectivityGracePeriod` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-068-standbyconnectivitygraceperiod-0.json` |
| `operator96-068-standbyconnectivitygraceperiod-1` | `StandbyConnectivityGracePeriod` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-068-standbyconnectivitygraceperiod-1.json` |
| `operator96-069-enforceausteritymode-0` | `EnforceAusterityMode` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-069-enforceausteritymode-0.json` |
| `operator96-069-enforceausteritymode-1` | `EnforceAusterityMode` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-12/operator96-069-enforceausteritymode-1.json` |
| `operator96-070-alwayscomputeqoshints-0` | `AlwaysComputeQosHints` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-070-alwayscomputeqoshints-0.json` |
| `operator96-070-alwayscomputeqoshints-1` | `AlwaysComputeQosHints` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-070-alwayscomputeqoshints-1.json` |
| `operator96-071-heteromulticoreclassesenabled-0` | `HeteroMultiCoreClassesEnabled` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-071-heteromulticoreclassesenabled-0.json` |
| `operator96-071-heteromulticoreclassesenabled-1` | `HeteroMultiCoreClassesEnabled` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-071-heteromulticoreclassesenabled-1.json` |
| `operator96-072-heteromulticlassparkingenabled-0` | `HeteroMultiClassParkingEnabled` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-072-heteromulticlassparkingenabled-0.json` |
| `operator96-072-heteromulticlassparkingenabled-1` | `HeteroMultiClassParkingEnabled` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-072-heteromulticlassparkingenabled-1.json` |
| `operator96-073-disableidlestatesatboot-2` | `DisableIdleStatesAtBoot` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-073-disableidlestatesatboot-2.json` |
| `operator96-073-disableidlestatesatboot-0` | `DisableIdleStatesAtBoot` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-13/operator96-073-disableidlestatesatboot-0.json` |
| `operator96-075-msdisabled-1` | `MSDisabled` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-075-msdisabled-1.json` |
| `operator96-075-msdisabled-0` | `MSDisabled` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-075-msdisabled-0.json` |
| `operator96-076-fxaccountingtelemetrydisabled-1` | `FxAccountingTelemetryDisabled` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-076-fxaccountingtelemetrydisabled-1.json` |
| `operator96-078-enableminimalhiberfile-1` | `EnableMinimalHiberFile` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-078-enableminimalhiberfile-1.json` |
| `operator96-079-hiberbootenabled-0` | `HiberbootEnabled` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-079-hiberbootenabled-0.json` |
| `operator96-079-hiberbootenabled-1` | `HiberbootEnabled` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-14/operator96-079-hiberbootenabled-1.json` |
| `operator96-080-maximumfrequencyoverride-100` | `MaximumFrequencyOverride` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-080-maximumfrequencyoverride-100.json` |
| `operator96-080-maximumfrequencyoverride-0` | `MaximumFrequencyOverride` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-15/operator96-080-maximumfrequencyoverride-0.json` |
| `operator96-089-powerwatchdogpocallouttimeoutmsec-1` | `PowerWatchdogPoCalloutTimeoutMsec` | `app_breakage` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-16/operator96-089-powerwatchdogpocallouttimeoutmsec-1.json` |
| `operator96-090-powerwatchdogpowerongditimeoutmsec-0` | `PowerWatchdogPowerOnGdiTimeoutMsec` | `noisy` | `noisy` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-17/operator96-090-powerwatchdogpowerongditimeoutmsec-0.json` |
