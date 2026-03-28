# power.session-watchdog-timeouts DcomLaunch attribution and post-boot trigger - 2026-03-28

## Summary

- The boot-trace `svchost.exe (PID 980)` lead is now attributed to the `DcomLaunch` service host group.
- The same group contains the `Power` service on the current `Win25H2Clean` baseline.
- A targeted post-boot Procmon probe then ran against that `DcomLaunch`/`Power` lane using `tasklist /svc`, `powercfg /q`, `powercfg /a`, and `sc queryex Power`.
- The probe stayed shell-safe, produced a real `PML`, a real `CSV`, and normalized tasklist/service outputs.
- Even with that tighter trigger, there were still no matching reads for `WatchdogResumeTimeout`, `WatchdogSleepTimeout`, or the adjacent Session Manager `Power` values.

## Source artifacts

- DcomLaunch attribution summary: `evidence/files/vm-tooling-staging/watchdog-dcomlaunch-attribution-20260328/summary.json`
- DcomLaunch services: `evidence/files/vm-tooling-staging/watchdog-dcomlaunch-attribution-20260328/dcomlaunch-services.json`
- ETL process-start lines: `evidence/files/vm-tooling-staging/watchdog-dcomlaunch-attribution-20260328/svchost-pid980-etl-lines.txt`
- Targeted probe summary: `evidence/files/vm-tooling-staging/watchdog-power-trigger-20260328-141804/summary.json`
- Targeted probe text export: `evidence/files/vm-tooling-staging/watchdog-power-trigger-20260328-141804/watchdog-power-trigger.txt`
- Tasklist snapshot: `evidence/files/vm-tooling-staging/watchdog-power-trigger-20260328-141804/watchdog-power-trigger-tasklist.txt`
- Power service snapshot: `evidence/files/vm-tooling-staging/watchdog-power-trigger-20260328-141804/watchdog-power-trigger-sc-power.txt`
- Sleep capability snapshot: `evidence/files/vm-tooling-staging/watchdog-power-trigger-20260328-141804/watchdog-power-trigger-powercfg-a.txt`
- Raw placeholder: `evidence/files/vm-tooling-staging/watchdog-power-trigger-20260328-141804/watchdog-power-trigger.pml.md`

## Result

- The ETL `svchost.exe (PID 980)` path was narrowed to `C:\Windows\system32\svchost.exe -k DcomLaunch -p`.
- The boot-era DcomLaunch grouping contained:
  - `BrokerInfrastructure`
  - `DcomLaunch`
  - `PlugPlay`
  - `Power`
  - `SystemEventsBroker`
- On the fresh post-boot baseline, `tasklist /svc` showed `svchost.exe (PID 960)` hosting the same `DcomLaunch` group, including `Power`.
- `sc queryex Power` confirmed that the `Power` service was running inside that shared process.
- The targeted Procmon capture finished with:
  - `PML_EXISTS=True`
  - `CSV_EXISTS=True`
  - `MATCH_COUNT=0`
- No filtered hits were produced for:
  - `WatchdogResumeTimeout`
  - `WatchdogSleepTimeout`
  - `PowerSettingProfile`
  - `SystemPowerPolicy`
  - `ShutdownOccurred`

## Why this matters

This closes the generic `svchost.exe` gap in the watchdog lane. The host-side ETL review no longer points to an anonymous service host; it points to the `DcomLaunch` group and specifically leaves the `Power` service in scope.

The tighter post-boot Procmon trigger is just as important because it stayed clean while still returning no matching registry traffic. That means the next missing proof is no longer "find the right service host." It is now narrower: either the decisive reads happen only during boot or S1 transition timing, or the watchdog pair is consumed through a code path that does not surface as an exact post-boot Procmon read under the current VMware baseline.
